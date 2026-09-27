import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { COMMAND_CODES, commandForCode, COMMANDS } from '../src/transport/wire.ts';
import {
    buildBase64QueryString,
    buildPlainQueryString,
    LOCALE_EN_US,
    parseBase64QueryString,
} from '../src/transport/querystring.ts';

const SPEC_EXAMPLE = 'jAAJBAp2MTQwRGV2aWNlAApTbWFydFBob25l';
const DEVICE = 'CF3A8C57A751343AD2D14A1F789FEFAD';

describe('base64 query value', () => {
    it('lays the example out field for field', () => {
        const parsed = parseBase64QueryString(SPEC_EXAMPLE);

        assert.equal(parsed.protocolVersion, '14.0');
        assert.equal(parsed.command, 'Sync');
        assert.equal(parsed.locale, LOCALE_EN_US);
        assert.equal(parsed.deviceType, 'SmartPhone');
        assert.equal(parsed.policyKey, undefined);
    });

    it('sends a device id as the bytes behind its hex, the way a device does', () => {
        const built = buildBase64QueryString({
            command: 'Sync',
            protocolVersion: '14.0',
            deviceId: DEVICE,
            deviceType: 'SmartPhone',
        });

        const bytes = [...atob(built)].map((c) => c.charCodeAt(0));

        assert.equal(bytes[4], 16, 'device id field should be the 16 raw bytes, not 32 characters');
        assert.equal(parseBase64QueryString(built).deviceId, DEVICE);
    });

    it('encodes the locale little-endian', () => {
        // en-US is 0x0409, the example carries it as 09 04
        const bytes = [...atob(buildBase64QueryString({
            command: 'Sync',
            protocolVersion: '14.1',
            deviceId: DEVICE,
            deviceType: 'Browser',
        }))].map((c) => c.charCodeAt(0));

        assert.deepEqual(bytes.slice(2, 4), [0x09, 0x04]);
    });

    it('round-trips a policy key as four little-endian bytes', () => {
        const parsed = parseBase64QueryString(buildBase64QueryString({
            command: 'FolderSync',
            protocolVersion: '14.1',
            deviceId: DEVICE,
            deviceType: 'Browser',
            policyKey: 3942919513,
        }));

        assert.equal(parsed.policyKey, 3942919513);
        assert.equal(parsed.command, 'FolderSync');
    });

    it('round-trips every command parameter', () => {
        const parsed = parseBase64QueryString(buildBase64QueryString({
            command: 'ItemOperations',
            protocolVersion: '14.1',
            deviceId: DEVICE,
            deviceType: 'Browser',
            user: 'someone@relivewp.net',
            attachmentName: 'att-1',
            collectionId: '5',
            itemId: 'item-1',
            longId: 'long-1',
            occurrence: '2026-01-01',
            saveInSent: true,
            acceptMultiPart: true,
        }));

        assert.equal(parsed.user, 'someone@relivewp.net');
        assert.equal(parsed.attachmentName, 'att-1');
        assert.equal(parsed.collectionId, '5');
        assert.equal(parsed.itemId, 'item-1');
        assert.equal(parsed.longId, 'long-1');
        assert.equal(parsed.occurrence, '2026-01-01');
        assert.equal(parsed.saveInSent, true);
        assert.equal(parsed.acceptMultiPart, true);
    });

    it('carries a multi-byte device type as UTF-8, counting bytes not characters', () => {
        const parsed = parseBase64QueryString(buildBase64QueryString({
            command: 'Sync',
            protocolVersion: '14.1',
            deviceId: DEVICE,
            deviceType: 'Brøwser',
        }));

        assert.equal(parsed.deviceType, 'Brøwser');
    });

    it('omits the padding a query string cannot carry', () => {
        for (const deviceId of ['ab', 'abcd', 'abcdef', 'abcdef01', 'abcdef0123']) {
            const built = buildBase64QueryString({
                command: 'Sync', protocolVersion: '14.1', deviceId, deviceType: 'Browser',
            });
            assert.ok(!built.includes('='), `padding leaked for device id ${deviceId}`);
            assert.equal(parseBase64QueryString(built).deviceId, deviceId.toUpperCase());
        }
    });

    it('refuses versions the format cannot represent', () => {
        assert.throws(() => buildBase64QueryString({
            command: 'Sync', protocolVersion: '2.5', deviceId: DEVICE, deviceType: 'Browser',
        }), /plain text/);
        assert.throws(() => buildBase64QueryString({
            command: 'Sync', protocolVersion: '12.0', deviceId: DEVICE, deviceType: 'Browser',
        }), /plain text/);
    });

    it('refuses a device id it cannot send as bytes', () => {
        for (const deviceId of ['', 'v140Device', 'abc']) {
            assert.throws(() => buildBase64QueryString({
                command: 'Sync', protocolVersion: '14.1', deviceId, deviceType: 'Browser',
            }), /hex/, `accepted ${JSON.stringify(deviceId)}`);
        }
    });
});

describe('plain text query value', () => {
    it('orders the required parameters as the grammar does', () => {
        assert.equal(
            buildPlainQueryString({
                command: 'Sync',
                user: 'rmjones',
                deviceId: 'v140Device',
                deviceType: 'SmartPhone',
            }),
            'Cmd=Sync&User=rmjones&DeviceId=v140Device&DeviceType=SmartPhone');
    });

    it('sends SaveInSent as T', () => {
        const query = buildPlainQueryString({
            command: 'SendMail', deviceId: DEVICE, deviceType: 'Browser', saveInSent: true,
        });
        assert.match(query, /SaveInSent=T/);
    });
});

describe('command codes', () => {
    it('matches MS-ASHTTP 2.2.1.1.1.1.2', () => {
        assert.equal(COMMAND_CODES.Sync, 0);
        assert.equal(COMMAND_CODES.GetAttachment, 4);
        assert.equal(COMMAND_CODES.FolderSync, 9);
        assert.equal(COMMAND_CODES.Provision, 20);
        assert.equal(COMMAND_CODES.Find, 23);
    });

    it('leaves 5 to 8 unassigned', () => {
        for (const code of [5, 6, 7, 8]) assert.equal(commandForCode(code), undefined);
    });

    it('is one-to-one', () => {
        assert.equal(new Set(COMMANDS.map((c) => COMMAND_CODES[c])).size, COMMANDS.length);
    });
});
