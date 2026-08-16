import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import type { Contact, ContactChangeSet, ContactPhotoChange, EasStore, MessageChangeSet }
    from '../src/index.ts';
import { contact, cursorInit, event, folder, message, task } from './fixtures.ts';

const JPEG = (marker: number) => new Uint8Array([0xff, 0xd8, 0xff, marker]);

function annotated(id: string, patch: Partial<Contact['annotation']> = {}): Contact {
    return contact(id, {
        annotation: {
            cid: null, objectId: null, wlid: null, imMri: null, type: null,
            userTileUrl: null, userTileHash: null, trustLevel: null, favouriteOrder: null,
            ...patch,
        },
    });
}

function contactChanges(patch: Partial<ContactChangeSet> = {}): ContactChangeSet {
    return {
        class: 'Contact',
        folderId: 'contacts',
        fromSyncKey: '0',
        toSyncKey: '1',
        syncedAt: 1000,
        moreAvailable: false,
        upsert: [],
        remove: [],
        photos: [],
        ...patch,
    };
}

async function withContacts(
    store: EasStore, upsert: Contact[], photos: ContactPhotoChange[] = []): Promise<void> {
    await store.ensureCursor(cursorInit('contacts', { class: 'Contact' }));
    await store.applyChanges(contactChanges({ upsert, photos }));
}

function changes(patch: Partial<MessageChangeSet> = {}): MessageChangeSet {
    return {
        class: 'Email',
        folderId: 'inbox',
        fromSyncKey: '0',
        toSyncKey: '1',
        syncedAt: 1000,
        moreAvailable: false,
        upsert: [],
        remove: [],
        ...patch,
    };
}

async function primed(store: EasStore): Promise<void> {
    await store.applyFolderChanges({
        fromSyncKey: '0', toSyncKey: 'f1', syncedAt: 1000, upsert: [folder('inbox')], remove: [],
    });
    await store.ensureCursor(cursorInit('inbox'));
}

export function runStoreSuite(name: string, open: () => Promise<EasStore>): void {
    describe(name, () => {
        it('starts with an unsynchronised account and no folders', async () => {
            const store = await open();
            const account = await store.account();

            assert.equal(account.folderSyncKey, '0');
            assert.equal(account.policyKey, null);
            assert.deepEqual(await store.folders(), []);
            assert.deepEqual(await store.cursors(), []);
            await store.close();
        });

        it('patches the account without dropping untouched fields', async () => {
            const store = await open();
            await store.saveAccount({ policyKey: 42 });
            const account = await store.saveAccount({ protocolVersion: '14.1' });

            assert.equal(account.policyKey, 42);
            assert.equal(account.protocolVersion, '14.1');
            assert.equal((await store.account()).deviceType, 'Browser');
            await store.close();
        });

        it('refuses a folder change whose key does not match, and writes nothing', async () => {
            const store = await open();
            const result = await store.applyFolderChanges({
                fromSyncKey: 'wrong', toSyncKey: 'f1', syncedAt: 1,
                upsert: [folder('inbox')], remove: [],
            });

            assert.deepEqual(result, { applied: false, reason: 'stale-cursor', found: '0' });
            assert.deepEqual(await store.folders(), []);
            await store.close();
        });

        it('removing a folder takes its cursor and its messages with it', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({ upsert: [message('a')] }));

            await store.applyFolderChanges({
                fromSyncKey: 'f1', toSyncKey: 'f2', syncedAt: 2, upsert: [], remove: ['inbox'],
            });

            assert.deepEqual(await store.folders(), []);
            assert.equal(await store.cursor('inbox'), undefined);
            assert.equal((await store.listMessages({ folderId: 'inbox', limit: 10 })).items.length, 0);
            await store.close();
        });

        it('creates a cursor once and returns the existing one afterwards', async () => {
            const store = await open();
            const first = await store.ensureCursor(cursorInit('inbox'));
            await store.applyChanges(changes());
            const second = await store.ensureCursor(cursorInit('inbox', { supportedHash: 'other' }));

            assert.equal(first.syncKey, '0');
            assert.equal(first.primed, false);
            assert.equal(second.syncKey, '1');
            assert.equal(second.supportedHash, null);
            await store.close();
        });

        it('replaces a cursor wholesale, because new options mean a new prime', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({ upsert: [message('a')] }));

            const replaced = await store.replaceCursor(cursorInit('inbox', {
                supportedHash: 'v2',
                options: {
                    windowSize: 50,
                    bodyType: 'text',
                    truncationSize: null,
                    filterType: 3,
                    conversationMode: false,
                    annotations: null,
                },
            }));

            assert.equal(replaced.syncKey, '0');
            assert.equal(replaced.primed, false);
            assert.equal(replaced.supportedHash, 'v2');
            assert.equal((await store.cursor('inbox'))?.options.filterType, 3);
            assert.equal(await store.message('inbox', 'a'), undefined);
            await store.close();
        });

        it('commits the new key and its messages together', async () => {
            const store = await open();
            await primed(store);
            const result = await store.applyChanges(changes({
                upsert: [message('a', { receivedAt: 5 }), message('b', { receivedAt: 6 })],
                moreAvailable: true,
            }));

            const cursor = await store.cursor('inbox');
            assert.deepEqual(result, { applied: true });
            assert.equal(cursor?.syncKey, '1');
            assert.equal(cursor?.primed, true);
            assert.equal(cursor?.moreAvailable, true);
            assert.equal(cursor?.lastSyncedAt, 1000);
            assert.equal((await store.listMessages({ folderId: 'inbox', limit: 10 })).items.length, 2);
            await store.close();
        });

        it('rejects a stale key and leaves the store untouched', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({ upsert: [message('a')] }));

            const result = await store.applyChanges(changes({
                fromSyncKey: '0', toSyncKey: '9', upsert: [message('b')],
            }));

            assert.deepEqual(result, { applied: false, reason: 'stale-cursor', found: '1' });
            assert.equal((await store.cursor('inbox'))?.syncKey, '1');
            assert.equal(await store.message('inbox', 'b'), undefined);
            await store.close();
        });

        it('reports a missing cursor rather than creating one', async () => {
            const store = await open();
            const result = await store.applyChanges(changes({ folderId: 'nowhere' }));

            assert.deepEqual(result, { applied: false, reason: 'stale-cursor', found: null });
            assert.deepEqual(await store.cursors(), []);
            await store.close();
        });

        it('rewinds a folder to unprimed and drops its messages', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({ upsert: [message('a')] }));
            await store.resetFolder('inbox');

            const cursor = await store.cursor('inbox');
            assert.equal(cursor?.syncKey, '0');
            assert.equal(cursor?.primed, false);
            assert.equal(cursor?.options.truncationSize, 4096);
            assert.equal((await store.listMessages({ folderId: 'inbox', limit: 10 })).items.length, 0);
            await store.close();
        });

        it('records a failure without moving the key', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes());
            await store.markFailed('inbox', 'status 5');

            const cursor = await store.cursor('inbox');
            assert.equal(cursor?.lastError, 'status 5');
            assert.equal(cursor?.syncKey, '1');
            await store.close();
        });

        it('lists newest first and breaks ties on id', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({
                upsert: [
                    message('a', { receivedAt: 10 }),
                    message('b', { receivedAt: 30 }),
                    message('c', { receivedAt: 20 }),
                    message('d', { receivedAt: 30 }),
                ],
            }));

            const page = await store.listMessages({ folderId: 'inbox', limit: 10 });
            assert.deepEqual(page.items.map((m) => m.id), ['d', 'b', 'c', 'a']);
            assert.equal(page.next, null);
            await store.close();
        });

        it('pages with the cursor from the previous page', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({
                upsert: [1, 2, 3, 4, 5].map((n) => message(`m${n}`, { receivedAt: n })),
            }));

            const first = await store.listMessages({ folderId: 'inbox', limit: 2 });
            assert.deepEqual(first.items.map((m) => m.id), ['m5', 'm4']);
            assert.deepEqual(first.next, { receivedAt: 4, id: 'm4' });

            const second = await store.listMessages({
                folderId: 'inbox', limit: 2, before: first.next ?? undefined,
            });
            assert.deepEqual(second.items.map((m) => m.id), ['m3', 'm2']);

            const third = await store.listMessages({
                folderId: 'inbox', limit: 2, before: second.next ?? undefined,
            });
            assert.deepEqual(third.items.map((m) => m.id), ['m1']);
            assert.equal(third.next, null);
            await store.close();
        });

        it('filters to unread on request', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({
                upsert: [
                    message('a', { receivedAt: 1, read: true }),
                    message('b', { receivedAt: 2 }),
                ],
            }));

            const page = await store.listMessages({ folderId: 'inbox', limit: 10, unreadOnly: true });
            assert.deepEqual(page.items.map((m) => m.id), ['b']);
            await store.close();
        });

        it('hands out copies, so a caller cannot mutate what is stored', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({ upsert: [message('a', { subject: 'first' })] }));

            const read = await store.message('inbox', 'a');
            assert.ok(read !== undefined);
            read.subject = 'tampered';

            assert.equal((await store.message('inbox', 'a'))?.subject, 'first');
            await store.close();
        });

        it('searches by prefix and requires every term to match', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({
                upsert: [
                    message('a', {
                        receivedAt: 1,
                        subject: 'Quarterly report',
                        from: { name: 'Ada Lovelace', email: 'ada@example.com' },
                    }),
                    message('b', { receivedAt: 2, subject: 'Quarterly lunch' }),
                ],
            }));

            const both = await store.searchMessages({ text: 'quarter', limit: 10 });
            assert.deepEqual(both.map((m) => m.id), ['b', 'a']);

            const narrowed = await store.searchMessages({ text: 'quarterly ada', limit: 10 });
            assert.deepEqual(narrowed.map((m) => m.id), ['a']);

            assert.deepEqual(await store.searchMessages({ text: 'nothinghere', limit: 10 }), []);
            assert.deepEqual(
                await store.searchMessages({ text: 'quarter', folderId: 'elsewhere', limit: 10 }), []);
            await store.close();
        });

        it('counts totals and unread per folder', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({
                upsert: [message('a', { read: true }), message('b'), message('c')],
            }));

            assert.deepEqual(await store.counts(), { inbox: { total: 3, unread: 2 } });
            await store.close();
        });

        it('counts a known but empty folder as zero rather than omitting it', async () => {
            const store = await open();
            await store.applyFolderChanges({
                fromSyncKey: '0',
                toSyncKey: 'f1',
                syncedAt: 1,
                upsert: [folder('inbox'), folder('archive', { role: 'user' })],
                remove: [],
            });

            assert.deepEqual(await store.counts(), {
                inbox: { total: 0, unread: 0 },
                archive: { total: 0, unread: 0 },
            });
            await store.close();
        });

        it('grants a free lease, holds it against a rival, and yields it once stale', async () => {
            const store = await open();
            assert.equal(await store.readLease(), undefined);

            assert.equal(await store.claimLease({ holderId: 'one', renewedAt: 1000 }, 15_000), true);
            assert.equal(await store.claimLease({ holderId: 'two', renewedAt: 5000 }, 15_000), false);
            assert.equal((await store.readLease())?.holderId, 'one');

            assert.equal(await store.claimLease({ holderId: 'two', renewedAt: 20_000 }, 15_000), true);
            assert.equal((await store.readLease())?.holderId, 'two');
            await store.close();
        });

        it('renews only for the holder, and releases only for the holder', async () => {
            const store = await open();
            await store.claimLease({ holderId: 'one', renewedAt: 1000 }, 15_000);

            assert.equal(await store.renewLease({ holderId: 'two', renewedAt: 2000 }), false);
            assert.equal(await store.renewLease({ holderId: 'one', renewedAt: 2000 }), true);
            assert.equal((await store.readLease())?.renewedAt, 2000);

            await store.releaseLease('two');
            assert.notEqual(await store.readLease(), undefined);

            await store.releaseLease('one');
            assert.equal(await store.readLease(), undefined);
            await store.close();
        });

        it('keeps each class in its own collection', async () => {
            const store = await open();
            await store.applyFolderChanges({
                fromSyncKey: '0',
                toSyncKey: 'f1',
                syncedAt: 1,
                upsert: [
                    folder('inbox'),
                    folder('contacts', { role: 'contacts', class: 'Contact', type: 9 }),
                    folder('calendar', { role: 'calendar', class: 'Calendar', type: 8 }),
                    folder('tasks', { role: 'tasks', class: 'Task', type: 7 }),
                ],
                remove: [],
            });

            await store.ensureCursor(cursorInit('inbox'));
            await store.ensureCursor(cursorInit('contacts', { class: 'Contact' }));
            await store.ensureCursor(cursorInit('calendar', { class: 'Calendar' }));
            await store.ensureCursor(cursorInit('tasks', { class: 'Task' }));

            await store.applyChanges(changes({ upsert: [message('m1')] }));
            await store.applyChanges({
                class: 'Contact', folderId: 'contacts', fromSyncKey: '0', toSyncKey: '1',
                syncedAt: 1, moreAvailable: false, remove: [], photos: [],
                upsert: [
                    contact('c1', { displayName: 'Ada Lovelace', sortName: 'lovelace ada',
                        emails: [{ label: 'email1', value: 'ada@example.com' }] }),
                    contact('c2', { displayName: 'Bob Barker', sortName: 'barker bob' }),
                ],
            });
            await store.applyChanges({
                class: 'Calendar', folderId: 'calendar', fromSyncKey: '0', toSyncKey: '1',
                syncedAt: 1, moreAvailable: false, remove: [],
                upsert: [
                    event('e1', { subject: 'Standup', startAt: 300, endAt: 400 }),
                    event('e2', { subject: 'Retro', startAt: 100, endAt: 200 }),
                ],
            });
            await store.applyChanges({
                class: 'Task', folderId: 'tasks', fromSyncKey: '0', toSyncKey: '1',
                syncedAt: 1, moreAvailable: false, remove: [],
                upsert: [
                    task('t1', { subject: 'Later', dueAt: 900 }),
                    task('t2', { subject: 'Sooner', dueAt: 100 }),
                    task('t3', { subject: 'Done', complete: true }),
                ],
            });

            assert.deepEqual(
                (await store.listContacts({ folderId: 'contacts', limit: 10 })).map((c) => c.id),
                ['c2', 'c1']);
            assert.deepEqual(
                (await store.listEvents({ folderId: 'calendar', limit: 10 })).map((e) => e.id),
                ['e2', 'e1']);
            assert.deepEqual(
                (await store.listTasks({ folderId: 'tasks', limit: 10 })).map((t) => t.id),
                ['t2', 't1']);

            assert.equal((await store.contact('contacts', 'c1'))?.displayName, 'Ada Lovelace');
            assert.equal((await store.event('calendar', 'e1'))?.subject, 'Standup');
            assert.equal((await store.task('tasks', 't1'))?.subject, 'Later');

            // same id in two classes must not collide
            assert.equal(await store.message('contacts', 'c1'), undefined);
            assert.equal(await store.contact('inbox', 'm1'), undefined);

            assert.deepEqual(await store.counts(), {
                inbox: { total: 1, unread: 1 },
                contacts: { total: 2, unread: 0 },
                calendar: { total: 2, unread: 0 },
                tasks: { total: 3, unread: 0 },
            });
            await store.close();
        });

        it('includes completed tasks only when asked', async () => {
            const store = await open();
            await store.ensureCursor(cursorInit('tasks', { class: 'Task' }));
            await store.applyChanges({
                class: 'Task', folderId: 'tasks', fromSyncKey: '0', toSyncKey: '1',
                syncedAt: 1, moreAvailable: false, remove: [],
                upsert: [task('t1'), task('t2', { complete: true })],
            });

            assert.equal((await store.listTasks({ folderId: 'tasks', limit: 10 })).length, 1);
            assert.equal(
                (await store.listTasks({ folderId: 'tasks', limit: 10, includeComplete: true })).length,
                2);
            await store.close();
        });

        it('returns events that overlap the window, not only those starting inside it', async () => {
            const store = await open();
            await store.ensureCursor(cursorInit('calendar', { class: 'Calendar' }));
            await store.applyChanges({
                class: 'Calendar', folderId: 'calendar', fromSyncKey: '0', toSyncKey: '1',
                syncedAt: 1, moreAvailable: false, remove: [],
                upsert: [
                    event('before', { startAt: 0, endAt: 50 }),
                    event('spanning', { startAt: 50, endAt: 250 }),
                    event('inside', { startAt: 120, endAt: 130 }),
                    event('after', { startAt: 400, endAt: 500 }),
                ],
            });

            const overlapping = await store.listEvents({
                folderId: 'calendar', limit: 10, from: 100, to: 300,
            });

            assert.deepEqual(overlapping.map((e) => e.id), ['spanning', 'inside']);
            await store.close();
        });

        it('searches contacts by name and by email', async () => {
            const store = await open();
            await store.ensureCursor(cursorInit('contacts', { class: 'Contact' }));
            await store.applyChanges({
                class: 'Contact', folderId: 'contacts', fromSyncKey: '0', toSyncKey: '1',
                syncedAt: 1, moreAvailable: false, remove: [], photos: [],
                upsert: [
                    contact('c1', {
                        displayName: 'Ada Lovelace', sortName: 'lovelace ada',
                        firstName: 'Ada', lastName: 'Lovelace',
                        emails: [{ label: 'email1', value: 'ada@example.com' }],
                    }),
                    contact('c2', { displayName: 'Bob Barker', sortName: 'barker bob' }),
                ],
            });

            assert.deepEqual(
                (await store.searchContacts({ text: 'lovel', limit: 10 })).map((c) => c.id), ['c1']);
            assert.deepEqual(
                (await store.searchContacts({ text: 'example', limit: 10 })).map((c) => c.id), ['c1']);
            assert.deepEqual(await store.searchContacts({ text: 'nobody', limit: 10 }), []);
            await store.close();
        });

        it('keeps the annotation set whole, so absent stays different from empty', async () => {
            const store = await open();
            await withContacts(store, [
                contact('plain'),
                annotated('marked', { type: 'Me', cid: '000f00c0de', favouriteOrder: 2 }),
            ]);

            assert.equal((await store.contact('contacts', 'plain'))?.annotation, null);

            const marked = await store.contact('contacts', 'marked');
            assert.equal(marked?.annotation?.type, 'Me');
            assert.equal(marked?.annotation?.cid, '000f00c0de');
            assert.equal(marked?.annotation?.favouriteOrder, 2);
            assert.equal(marked?.annotation?.wlid, null);
            await store.close();
        });

        it('orders favourites by FavoriteOrder and leaves everyone else out', async () => {
            const store = await open();
            await withContacts(store, [
                annotated('third', { favouriteOrder: 30 }),
                annotated('first', { favouriteOrder: 10 }),
                contact('nobody'),
                annotated('second', { favouriteOrder: 20 }),
                annotated('unranked', { type: 'Me' }),
            ]);

            assert.deepEqual(
                (await store.listFavourites({ folderId: 'contacts', limit: 10 })).map((c) => c.id),
                ['first', 'second', 'third']);
            assert.deepEqual(
                (await store.listFavourites({ folderId: 'contacts', limit: 2 })).map((c) => c.id),
                ['first', 'second']);
            await store.close();
        });

        it('finds the Me contact whatever case the server used, and nothing when unmarked', async () => {
            const store = await open();
            await withContacts(store, [contact('other'), annotated('self', { type: 'ME' })]);
            assert.equal((await store.meContact('contacts'))?.id, 'self');

            const bare = await open();
            await withContacts(bare, [contact('other')]);
            assert.equal(await bare.meContact('contacts'), undefined);

            await store.close();
            await bare.close();
        });

        it('stores a contact photo and reads it back as a blob', async () => {
            const store = await open();
            await withContacts(store, [contact('c1')], [{ id: 'c1', bytes: JPEG(0xe0) }]);

            const photo = await store.contactPhoto('contacts', 'c1');
            assert.equal(photo?.type, 'image/jpeg');
            assert.equal(photo?.size, 4);
            assert.equal(await store.contactPhoto('contacts', 'missing'), undefined);
            await store.close();
        });

        // an omitted Picture means unchanged, not cleared. only an empty one clears
        it('leaves a photo alone when a later change carries no entry for it', async () => {
            const store = await open();
            await withContacts(store, [contact('c1')], [{ id: 'c1', bytes: JPEG(0xe0) }]);

            await store.applyChanges(contactChanges({
                fromSyncKey: '1', toSyncKey: '2', upsert: [contact('c1', { displayName: 'Renamed' })],
            }));

            assert.equal((await store.contactPhoto('contacts', 'c1'))?.size, 4);
            await store.close();
        });

        it('clears a photo when the change carries an empty one', async () => {
            const store = await open();
            await withContacts(store, [contact('c1')], [{ id: 'c1', bytes: JPEG(0xe0) }]);

            await store.applyChanges(contactChanges({
                fromSyncKey: '1', toSyncKey: '2', photos: [{ id: 'c1', bytes: new Uint8Array() }],
            }));

            assert.equal(await store.contactPhoto('contacts', 'c1'), undefined);
            await store.close();
        });

        it('takes a photo with the contact it belongs to', async () => {
            const store = await open();
            await withContacts(store, [contact('c1')], [{ id: 'c1', bytes: JPEG(0xe0) }]);

            await store.applyChanges(contactChanges({
                fromSyncKey: '1', toSyncKey: '2', remove: ['c1'],
            }));

            assert.equal(await store.contactPhoto('contacts', 'c1'), undefined);
            await store.close();
        });

        it('reads photos in bulk, whole folder or a named subset', async () => {
            const store = await open();
            await withContacts(
                store,
                [contact('c1'), contact('c2'), contact('c3')],
                [{ id: 'c1', bytes: JPEG(0xe0) }, { id: 'c3', bytes: JPEG(0xe1) }]);

            const all = await store.contactPhotos('contacts');
            assert.deepEqual([...all.keys()].sort(), ['c1', 'c3']);

            const some = await store.contactPhotos('contacts', ['c1', 'c2']);
            assert.deepEqual([...some.keys()], ['c1']);

            assert.equal((await store.contactPhotos('nowhere')).size, 0);
            await store.close();
        });

        it('drops photos when the folder is rewound for a re-prime', async () => {
            const store = await open();
            await withContacts(store, [contact('c1')], [{ id: 'c1', bytes: JPEG(0xe0) }]);

            await store.resetFolder('contacts');

            assert.equal(await store.contactPhoto('contacts', 'c1'), undefined);
            assert.equal((await store.contactPhotos('contacts')).size, 0);
            await store.close();
        });

        it('wipes every trace of the mailbox and rewinds the account', async () => {
            const store = await open();
            await primed(store);
            await store.applyChanges(changes({ upsert: [message('a')] }));
            await store.saveAccount({ policyKey: 42, protocolVersion: '14.1' });
            await store.claimLease({ holderId: 'one', renewedAt: 1 }, 15_000);

            await store.wipe();

            const account = await store.account();
            assert.equal(account.policyKey, null);
            assert.equal(account.folderSyncKey, '0');
            assert.deepEqual(await store.folders(), []);
            assert.deepEqual(await store.cursors(), []);
            assert.deepEqual(await store.counts(), {});
            assert.equal(await store.readLease(), undefined);
            assert.equal(await store.message('inbox', 'a'), undefined);
            await store.close();
        });
    });
}
