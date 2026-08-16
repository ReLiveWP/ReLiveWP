import assert from 'node:assert/strict';
import { test } from 'node:test';

import { bytes, elements, flag, int, path, pick, pickAll, text, tri } from '../src/nodes/read.ts';
import { el, opaque } from '../src/nodes/node.ts';
import { AirSync, AirSyncBase, ComposeMail, Contacts } from '../src/generated/tags.g.ts';

const collection = AirSync.Collection(
    AirSync.SyncKey('3'),
    AirSync.CollectionId('inbox'),
    AirSync.Status(1),
    AirSync.MoreAvailable(),
    AirSync.Commands(
        AirSync.Add(AirSync.ServerId('a')),
        AirSync.Add(AirSync.ServerId('b')),
        AirSync.Delete(AirSync.ServerId('c'))));

const root = AirSync.Sync(AirSync.Collections(collection));

test('pick finds a child and pickAll finds every match', () => {
    assert.equal(text(pick(collection, AirSync.SyncKey), AirSync.SyncKey), undefined);
    assert.equal(text(collection, AirSync.SyncKey), '3');
    assert.equal(pickAll(pick(collection, AirSync.Commands), AirSync.Add).length, 2);
});

test('path walks a chain', () => {
    assert.equal(text(path(root, AirSync.Collections, AirSync.Collection), AirSync.CollectionId), 'inbox');
});

test('every reader is total on a missing node', () => {
    assert.equal(pick(undefined, AirSync.SyncKey), undefined);
    assert.deepEqual(pickAll(undefined, AirSync.Add), []);
    assert.equal(path(undefined, AirSync.Collections), undefined);
    assert.equal(text(undefined, AirSync.SyncKey), undefined);
    assert.equal(int(undefined, AirSync.Status), undefined);
    assert.equal(bytes(undefined, ComposeMail.Mime), undefined);
    assert.equal(flag(undefined, AirSync.MoreAvailable), false);
    assert.equal(tri(undefined, AirSync.GetChanges), undefined);
    assert.deepEqual(elements(undefined), []);
});

test('path returns undefined rather than throwing on a broken chain', () => {
    assert.equal(path(root, AirSync.Commands, AirSync.Add), undefined);
});

test('int parses integers and rejects everything else', () => {
    assert.equal(int(collection, AirSync.Status), 1);
    assert.equal(int(AirSync.Collection(AirSync.Status('two')), AirSync.Status), undefined);
    assert.equal(int(AirSync.Collection(AirSync.Status('1.5')), AirSync.Status), undefined);
    assert.equal(int(collection, AirSync.WindowSize), undefined);
});

test('flag reports presence of an empty tag', () => {
    assert.equal(flag(collection, AirSync.MoreAvailable), true);
    assert.equal(flag(collection, AirSync.Partial), false);
});

test('tri separates absent from empty from an explicit value', () => {
    assert.equal(tri(AirSync.Collection(), AirSync.GetChanges), undefined);
    assert.equal(tri(AirSync.Collection(AirSync.GetChanges()), AirSync.GetChanges), true);
    assert.equal(tri(AirSync.Collection(AirSync.GetChanges(true)), AirSync.GetChanges), true);
    assert.equal(tri(AirSync.Collection(AirSync.GetChanges(false)), AirSync.GetChanges), false);
});

test('bytes reads an opaque run', () => {
    const mime = new TextEncoder().encode('From: a@b\r\n');
    assert.deepEqual(bytes(AirSyncBase.Body(ComposeMail.Mime(mime)), ComposeMail.Mime), mime);
});

// a server can send any element as an opaque run, and reading only STR_I made those look like
// missing fields rather than a bug
test('text decodes an opaque run as UTF-8', () => {
    const name = el('Contacts', 'FirstName', opaque(new TextEncoder().encode('Síoda')));
    assert.equal(text(el('Contacts', 'ApplicationData', name), Contacts.FirstName), 'Síoda');
});

test('elements skips text and keeps wire order', () => {
    const commands = pick(collection, AirSync.Commands);
    assert.deepEqual(elements(commands).map((e) => e.name), ['Add', 'Add', 'Delete']);
    assert.deepEqual(elements(AirSync.SyncKey('3')), []);
});
