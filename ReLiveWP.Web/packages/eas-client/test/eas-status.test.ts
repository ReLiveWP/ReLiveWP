import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { describe, it, test } from 'node:test';

import { EAS_STATUS_TEXT } from '../src/generated/status-text.g.ts';
import { describeStatus, statusText } from '../src/status-text.ts';
import { EAS_STATUS, type EasStatusTableName } from '../src/generated/status.g.ts';
import { SUCCESS, isDocumentedStatus, statusCitation } from '../src/status.ts';

const names = Object.keys(EAS_STATUS) as EasStatusTableName[];
const statusValues = (name: EasStatusTableName) => EAS_STATUS[name].values;

describe('the generated tables match what was maintained by hand', () => {
    it('Sync', () => {
        assert.deepEqual(statusValues('Sync'), [1, 3, 4, 5, 6, 7, 8, 9, 12, 13, 14, 15, 16]);
    });

    it('FolderSync', () => {
        assert.deepEqual(statusValues('FolderSync'), [1, 6, 9, 10, 11, 12]);
    });
});

describe('lookups', () => {
    it('finds a command status with its scope', () => {
        assert.equal(statusText('Sync', 3)?.meaning, 'Invalid synchronization key.');
        assert.equal(statusText('Sync', 3)?.scope, 'Global');
    });

    it('falls back to the common codes, which any command may return', () => {
        assert.equal(isDocumentedStatus('Sync', 177), true);
        assert.equal(statusValues('Sync').includes(177), false);
        assert.equal(statusText('Sync', 177)?.name, 'MaximumDevicesReached');
    });

    it('reports an undocumented value as such', () => {
        assert.equal(isDocumentedStatus('Sync', 9999), false);
        assert.equal(describeStatus('Sync', 9999), 'Status 9999 (undocumented)');
        assert.equal(describeStatus('Sync', undefined), 'no Status');
    });

    it('cites the section the table came from', () => {
        assert.equal(statusCitation('FolderSync'), 'MS-ASCMD 2.2.3.177.5');
        assert.equal(statusCitation('Provision'), 'MS-ASPROV 2.2.2.54.2');
    });
});

describe('Status 1 does not mean success everywhere', () => {
    it('MoveItems succeeds with 3', () => {
        assert.match(statusText('MoveItems', 1)!.meaning, /^Invalid source collection ID/);
        assert.match(statusText('MoveItems', 3)!.meaning, /[Ss]uccess/);
    });

    it('Ping returns 1 for an expired heartbeat and 2 for changes', () => {
        assert.match(statusText('Ping', 1)!.meaning, /heartbeat interval expired/);
        assert.match(statusText('Ping', 2)!.meaning, /[Cc]hanges occurred/);
    });

    it('is otherwise success', () => {
        for (const name of names) {
            if (name === 'Common' || name === 'MoveItems' || name === 'Ping') continue;
            assert.ok(statusValues(name).includes(SUCCESS), `${name} documents no Status 1`);
            assert.match(statusText(name, SUCCESS)!.meaning, /success/i, `${name} Status 1`);
        }
    });
});

describe('table shape', () => {
    it('has no duplicate values and stays sorted', () => {
        for (const name of names) {
            const seen = statusValues(name);
            assert.deepEqual(seen, [...new Set(seen)], `${name} repeats a status value`);
            assert.deepEqual(seen, [...seen].sort((a, b) => a - b), `${name} is not sorted`);
        }
    });

    it('gives every value a meaning, and every meaning a value', () => {
        for (const name of names) {
            const text = EAS_STATUS_TEXT[name];
            assert.deepEqual(
                Object.keys(text).map(Number).sort((a, b) => a - b),
                [...statusValues(name)],
                `${name} disagrees between its values and its meanings`);

            for (const [value, entry] of Object.entries(text))
                assert.ok(entry.meaning.length > 0, `${name} ${value} has no meaning`);
        }
    });

    it('leaves no markdown behind', () => {
        for (const name of names)
            for (const entry of Object.values(EAS_STATUS_TEXT[name]))
                assert.doesNotMatch(entry.meaning, /\*\*|\]\(#|\\/);
    });
});

