import 'fake-indexeddb/auto';

import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { openStore } from '../src/idb.ts';
import { CONTACTS, CONTACT_PHOTOS, META, upgrade } from '../src/idb/schema.ts';
import { contact, cursorInit } from './fixtures.ts';

// the suite only ever opens a fresh database, which runs every step and so never proves that an
// existing one migrates rather than being rebuilt
function openAt(
    name: string,
    version: number,
    onUpgrade: (db: IDBDatabase, from: number, to: number) => void): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        const opening = indexedDB.open(name, version);
        opening.onupgradeneeded = (event) =>
            onUpgrade(opening.result, event.oldVersion, event.newVersion ?? version);
        opening.onsuccess = () => resolve(opening.result);
        opening.onerror = () => reject(opening.error);
    });
}

let next = 0;

describe('the version 1 to 2 migration', () => {
    it('adds the photo store to a database that predates it, keeping what was there', async () => {
        const name = `upgrade-${next++}`;

        // build a genuine v1: the same upgrade function, stopped at the version before photos
        const v1 = await openAt(name, 1, (db, from, to) => upgrade(db, from, to));
        assert.equal(v1.objectStoreNames.contains(CONTACT_PHOTOS), false);
        assert.equal(v1.objectStoreNames.contains(CONTACTS), true);

        const tx = v1.transaction(META, 'readwrite');
        tx.objectStore(META).put({ key: 'canary', value: 'survives' });
        await new Promise((done) => { tx.oncomplete = done; });
        v1.close();

        const store = await openStore({
            userId: 'u', deviceId: 'D1', deviceType: 'Browser', name,
        });

        await store.ensureCursor(cursorInit('contacts', { class: 'Contact' }));
        await store.applyChanges({
            class: 'Contact', folderId: 'contacts', fromSyncKey: '0', toSyncKey: '1',
            syncedAt: 1, moreAvailable: false, remove: [], upsert: [contact('c1')],
            photos: [{ id: 'c1', bytes: new Uint8Array([0xff, 0xd8, 0xff, 0xe0]) }],
        });

        assert.equal((await store.contactPhoto('contacts', 'c1'))?.size, 4);
        await store.close();

        const reopened = await openAt(name, 2, () => {
            assert.fail('reopening at the current version must not upgrade again');
        });
        const read = reopened.transaction(META, 'readonly').objectStore(META).get('canary');
        await new Promise((done) => { read.onsuccess = done; });

        assert.equal(read.result?.value, 'survives');
        reopened.close();
    });

    it('runs every step in order on a database that did not exist', async () => {
        const name = `fresh-${next++}`;
        const db = await openAt(name, 2, (target, from, to) => {
            assert.equal(from, 0);
            upgrade(target, from, to);
        });

        assert.equal(db.objectStoreNames.contains(CONTACTS), true);
        assert.equal(db.objectStoreNames.contains(CONTACT_PHOTOS), true);
        db.close();
    });
});
