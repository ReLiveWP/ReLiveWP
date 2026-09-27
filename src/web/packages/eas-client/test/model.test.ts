import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { describe, it, test } from 'node:test';

import { mergeEmail, readEmail, writeEmail, type EmailItem } from '../src/generated/model/email.g.ts';
import { readDate, writeDate } from '../src/nodes/dates.ts';
import { elements } from '../src/nodes/read.ts';
import { AirSync as A, Email as E, Email2 as E2 } from '../src/generated/tags.g.ts';

const received = new Date(Date.UTC(2026, 6, 16, 20, 0, 0));

const item = () => A.ApplicationData(
    E.Subject('hello'),
    E.From('a@b.test'),
    E.To('c@d.test'),
    E.DateReceived('2026-07-16T20:00:00.000Z'),
    E.Read(false),
    E.Importance(1),
    E.ThreadTopic('a thread'),
    E2.ConversationIndex('deadbeef'));

describe('dates', () => {
    it('writes the compact form for Calendar and Notes', () => {
        assert.equal(writeDate(received, 'compact'), '20260716T200000Z');
    });

    it('writes the extended form for Email, Contacts and Tasks', () => {
        assert.equal(writeDate(received, 'extended'), '2026-07-16T20:00:00.000Z');
    });

    it('reads either form back, whichever a server sends', () => {
        assert.deepEqual(readDate('20260716T200000Z'), received);
        assert.deepEqual(readDate('2026-07-16T20:00:00.000Z'), received);
        assert.deepEqual(readDate('2026-07-16T20:00:00Z'), received);
    });

    it('is undefined rather than Invalid Date on junk', () => {
        assert.equal(readDate('not a date'), undefined);
        assert.equal(readDate(''), undefined);
        assert.equal(readDate(undefined), undefined);
        assert.equal(writeDate(new Date(NaN), 'compact'), undefined);
    });
});

describe('readEmail', () => {
    it('reads scalars with their wire types', () => {
        const email = readEmail(item());

        assert.equal(email.subject, 'hello');
        assert.equal(email.importance, 1);
        assert.equal(email.read, false);
        assert.deepEqual(email.dateReceived, received);
        assert.equal(email.conversationIndex, 'deadbeef');
    });

    it('leaves absent properties undefined', () => {
        assert.equal(readEmail(A.ApplicationData()).subject, undefined);
    });

    it('round-trips through writeEmail', () => {
        const original: EmailItem = { subject: 'hi', importance: 2, read: true, dateReceived: received };
        assert.deepEqual(readEmail(A.ApplicationData(writeEmail(original))), {
            ...readEmail(A.ApplicationData()),
            ...original,
        });
    });
});

describe('mergeEmail preserves everything it was not asked to change', () => {
    it('keeps every other child, in order', () => {
        const before = item();
        const after = mergeEmail(before, { read: true });

        assert.deepEqual(elements(after).map((e) => e.name), elements(before).map((e) => e.name));

        for (const name of ['Subject', 'From', 'To', 'DateReceived', 'Importance', 'ThreadTopic']) {
            const was = elements(before).find((e) => e.name === name);
            const now = elements(after).find((e) => e.name === name);
            assert.deepEqual(now, was, `${name} was disturbed by a change to Read`);
        }
    });

    it('replaces the property in place rather than appending a second one', () => {
        const after = mergeEmail(item(), { read: true });
        const reads = elements(after).filter((e) => e.name === 'Read');

        assert.equal(reads.length, 1);
        assert.equal(readEmail(after).read, true);
    });

    it('appends a property the original did not carry', () => {
        const after = mergeEmail(item(), { categories: undefined, flag: undefined, subject: 'changed' });
        assert.equal(readEmail(after).subject, 'changed');
    });

    it('removes a property set to undefined', () => {
        const after = mergeEmail(item(), { threadTopic: undefined });

        assert.equal(elements(after).some((e) => e.name === 'ThreadTopic'), false);
        assert.equal(readEmail(after).subject, 'hello');
    });

    it('leaves an untouched tree completely alone', () => {
        const before = item();
        assert.deepEqual(mergeEmail(before, {}), before);
    });

    it('crosses namespaces without disturbing the other one', () => {
        const after = mergeEmail(item(), { conversationIndex: 'cafe' });

        assert.equal(readEmail(after).conversationIndex, 'cafe');
        assert.equal(readEmail(after).subject, 'hello');
    });

    it('writes a date in the format its class uses', () => {
        const after = mergeEmail(item(), { dateReceived: received });
        const written = elements(after).find((e) => e.name === 'DateReceived');

        assert.deepEqual(written?.children, [{ text: '2026-07-16T20:00:00.000Z' }]);
    });
});

