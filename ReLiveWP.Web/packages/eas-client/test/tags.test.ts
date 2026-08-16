import assert from 'node:assert/strict';
import { test } from 'node:test';

import { AirSync, AirSyncBase, ComposeMail } from '../src/generated/tags.g.ts';
import { makeTags, opt } from '../src/nodes/tags.ts';
import { encode } from '../src/wbxml/encode.ts';
import { decode } from '../src/wbxml/decode.ts';

test('a tag carries its namespace and name', () => {
    assert.equal(AirSync.SyncKey.ns, 'AirSync');
    assert.equal(AirSync.SyncKey.name, 'SyncKey');
});

test('scalars coerce to their wire form', () => {
    assert.deepEqual(AirSync.WindowSize(50).children, [{ text: '50' }]);
    assert.deepEqual(AirSync.GetChanges(true).children, [{ text: '1' }]);
    assert.deepEqual(AirSync.SyncKey('7').children, [{ text: '7' }]);
});

test('false encodes as 0 rather than disappearing', () => {
    assert.deepEqual(AirSync.DeletesAsMoves(false).children, [{ text: '0' }]);
});

test('undefined and null children are dropped', () => {
    assert.deepEqual(AirSync.Options(undefined, null).children, []);
});

test('arrays are flattened', () => {
    const node = AirSync.Collections([AirSync.Collection(), AirSync.Collection()]);
    assert.equal(node.children.length, 2);
});

test('a Uint8Array child becomes opaque', () => {
    const mime = new TextEncoder().encode('From: a@b\r\n');
    assert.deepEqual(ComposeMail.Mime(mime).children, [{ opaque: mime }]);
});

test('opt emits only when the value is present, and keeps false', () => {
    assert.equal(opt(undefined, AirSync.WindowSize), undefined);
    assert.equal(opt(null, AirSync.WindowSize), undefined);
    assert.deepEqual(opt(0, AirSync.WindowSize)?.children, [{ text: '0' }]);
    assert.deepEqual(opt(false, AirSync.GetChanges)?.children, [{ text: '0' }]);
});

test('makeTags produces one tag per name and nothing else', () => {
    const set = makeTags('AirSync', ['Sync', 'Status'] as const);
    assert.deepEqual(Object.keys(set), ['Sync', 'Status']);
});

test('a built tree round-trips through the codec', () => {
    const built = AirSync.Sync(
        AirSync.Collections(
            AirSync.Collection(
                AirSync.SyncKey('3'),
                AirSync.CollectionId('inbox'),
                AirSync.GetChanges(true),
                AirSync.WindowSize(50),
                AirSync.Options(
                    AirSyncBase.BodyPreference(
                        AirSyncBase.Type(2),
                        AirSyncBase.TruncationSize(512))))));

    assert.deepEqual(decode(encode(built)).root, built);
});
