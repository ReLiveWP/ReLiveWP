import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { SyncEngine, type SyncProgress } from '../src/index.ts';
import { droppingCommits, harness } from './harness.ts';
import {
    AS_DEPLOYED,
    CONFORMANT,
    contactsFolder,
    draftsFolder,
    notesFolder,
} from './server/server.ts';
import { contactItem, mailItem, messages } from './server/items.ts';

async function idsIn(store: Awaited<ReturnType<typeof harness>>['store'], folderId: string) {
    const page = await store.listMessages({ folderId, limit: 500 });
    return page.items.map((message) => message.id);
}

describe('a cold start', () => {
    it('provisions, learns the hierarchy, primes and drains', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 5);

        const report = await engine.synchronise();

        assert.equal(report.recovery.kind, 'ok');
        assert.equal(report.provision?.provisioned, true);
        assert.equal(report.hierarchy?.added, 1);
        assert.equal(report.folders.length, 1);
        assert.equal(report.folders[0]?.delivered, 5);
        assert.deepEqual(
            (await idsIn(store, 'inbox')).sort(),
            ['m1', 'm2', 'm3', 'm4', 'm5']);
    });

    it('keeps the policy document, because it decides the body preference', async () => {
        const { engine, store } = harness();
        await engine.synchronise();

        const account = await store.account();
        assert.equal(account.policy?.allowHtmlEmail, true);
        assert.equal(account.policy?.maxEmailHtmlBodyTruncationSize, 4096);
        assert.equal((await store.cursor('inbox'))?.options.truncationSize, 4096);
        assert.equal((await store.cursor('inbox'))?.options.bodyType, 'html');
    });

    it('projects a message into something a list can render', async () => {
        const { engine, store, server } = harness();
        messages(server.mailbox, 'inbox', 1);
        await engine.synchronise();

        const message = await store.message('inbox', 'm1');
        assert.equal(message?.subject, 'Message 1');
        assert.deepEqual(message?.from, { name: 'Sender 1', email: 'sender1@example.com' });
        assert.deepEqual(message?.to, [{ name: 'Me', email: 'me@example.com' }]);
        assert.equal(message?.read, false);
        assert.equal(message?.body?.type, 'html');
        assert.equal(message?.preview, 'Body of message 1');
        assert.equal(message?.receivedAt, Date.UTC(2026, 0, 1));
    });
});

describe('a second run', () => {
    it('costs one Sync per folder and re-downloads nothing', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 3);
        await engine.synchronise();

        const before = server.requests.length;
        const report = await engine.synchronise();

        const syncs = server.requests.slice(before).filter((r) => r.command === 'Sync');
        assert.equal(syncs.length, 1);
        assert.equal(report.folders[0]?.delivered, 0);
        assert.equal(report.folders[0]?.unchanged, true);
        assert.equal((await idsIn(store, 'inbox')).length, 3);
    });

    it('treats a body-less response as nothing changed rather than an error', async () => {
        const { engine, server } = harness();
        messages(server.mailbox, 'inbox', 1);
        await engine.synchronise();

        const keyBefore = (await engine['store'].cursor('inbox'))?.syncKey;
        const report = await engine.synchronise();
        const keyAfter = (await engine['store'].cursor('inbox'))?.syncKey;

        assert.equal(report.folders[0]?.recovery.kind, 'ok');
        assert.equal(report.folders[0]?.unchanged, true);
        assert.equal(keyAfter, keyBefore);
        assert.ok(server.requests.some((r) => r.command === 'Sync' && r.empty === false));
    });

    it('picks up an add, a change and a delete', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 3);
        await engine.synchronise();

        server.mailbox.changeItem('inbox', 'm1', mailItem({ subject: 'Message 1 revised', read: true }));
        server.mailbox.deleteItem('inbox', 'm2');
        server.mailbox.addItem({
            id: 'm9',
            folderId: 'inbox',
            data: mailItem({ subject: 'Later', receivedAt: new Date(Date.UTC(2026, 1, 1)) }),
        });

        await engine.synchronise();

        assert.deepEqual((await idsIn(store, 'inbox')).sort(), ['m1', 'm3', 'm9']);
        assert.equal((await store.message('inbox', 'm1'))?.read, true);
        assert.equal((await store.message('inbox', 'm1'))?.subject, 'Message 1 revised');
    });
});

describe('paging', () => {
    it('converges on a window far smaller than the collection', async () => {
        const { engine, server, store } = harness({ engine: { windowSize: 2 } });
        messages(server.mailbox, 'inbox', 9);

        const report = await engine.synchronise();

        assert.equal(report.folders[0]?.converged, true);
        assert.equal(report.folders[0]?.delivered, 9);
        assert.ok(report.folders[0]!.rounds <= 8, `took ${report.folders[0]!.rounds} rounds`);
        assert.equal((await idsIn(store, 'inbox')).length, 9);
    });

    it('gives up rather than looping when the server never advances', async () => {
        const { engine, server } = harness({
            quirks: { ...AS_DEPLOYED, neverAdvances: true },
            engine: { windowSize: 2, maxRounds: 30 },
        });
        messages(server.mailbox, 'inbox', 9);

        const report = await engine.synchronise();
        const outcome = report.folders[0]!;

        assert.equal(outcome.converged, false);
        assert.equal(outcome.recovery.kind, 'fail');
        assert.ok(outcome.rounds < 30, `ran ${outcome.rounds} rounds before giving up`);
    });
});

describe('recovery', () => {
    it('re-primes one folder when the server rejects its sync key', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 3);
        await engine.synchronise();

        server.scriptStatus('Sync', 3);
        const outcome = await engine.syncFolder('inbox');

        assert.equal(outcome.recovery.kind, 'ok');
        assert.equal((await idsIn(store, 'inbox')).length, 3);
        assert.equal((await store.cursor('inbox'))?.primed, true);
    });

    it('runs a FolderSync when the hierarchy is reported stale, then finishes the Sync', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 2);
        await engine.synchronise();

        const before = server.requests.filter((r) => r.command === 'FolderSync').length;
        server.scriptStatus('Sync', 12);
        const outcome = await engine.syncFolder('inbox');

        const after = server.requests.filter((r) => r.command === 'FolderSync').length;
        assert.equal(outcome.recovery.kind, 'ok');
        assert.equal(after, before + 1);
        assert.equal((await idsIn(store, 'inbox')).length, 2);
    });

    it('re-provisions when the server says the policy key is stale', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 1);
        await engine.synchronise();

        server.scriptStatus('Sync', 144);
        const outcome = await engine.syncFolder('inbox');

        assert.equal(outcome.recovery.kind, 'ok');
        assert.equal((await store.account()).policyKey, server.policyKey);
    });

    it('survives a lost commit by resending the key the server last acknowledged', async () => {
        const { engine, server, store, session } = harness();
        messages(server.mailbox, 'inbox', 4);
        await engine.synchronise();

        server.mailbox.addItem({
            id: 'm5',
            folderId: 'inbox',
            data: mailItem({ subject: 'Fifth', receivedAt: new Date(Date.UTC(2026, 0, 5)) }),
        });

        const keyBefore = (await store.cursor('inbox'))?.syncKey;
        const lossy = new SyncEngine(session, droppingCommits(store, 1), { now: () => 1 });
        await lossy.syncFolder('inbox').catch(() => undefined);

        assert.equal((await store.cursor('inbox'))?.syncKey, keyBefore);

        await engine.synchronise();
        assert.deepEqual((await idsIn(store, 'inbox')).sort(), ['m1', 'm2', 'm3', 'm4', 'm5']);
    });

    it('rejects a delta built against a key that has already moved', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 2);
        await engine.synchronise();

        const result = await store.applyChanges({
            class: 'Email',
            folderId: 'inbox',
            fromSyncKey: '1',
            toSyncKey: '99',
            syncedAt: 0,
            moreAvailable: false,
            upsert: [],
            remove: ['m1'],
        });

        assert.equal(result.applied, false);
        assert.equal((await idsIn(store, 'inbox')).length, 2);
    });

    it('backs off through a throttled server and still finishes', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 2);
        server.throttleNext(2, 1);

        const report = await engine.synchronise();

        assert.equal(report.recovery.kind, 'ok');
        assert.equal((await idsIn(store, 'inbox')).length, 2);
    });

    it('destroys the mailbox when the server asks for a remote wipe', async () => {
        const { engine, server, store } = harness();
        messages(server.mailbox, 'inbox', 3);
        await engine.synchronise();
        assert.equal((await idsIn(store, 'inbox')).length, 3);

        server.requestRemoteWipe();
        server.scriptStatus('Sync', 140);
        const outcome = await engine.syncFolder('inbox');

        assert.equal(outcome.recovery.kind, 'wipe');
        assert.deepEqual(await store.folders(), []);
        assert.deepEqual(await store.cursors(), []);
        assert.equal((await store.account()).policyKey, null);
        assert.equal(await store.message('inbox', 'm1'), undefined);
    });
});

describe('which folders get synchronised', () => {
    it('leaves Drafts alone, because the specification forbids syncing it', async () => {
        const { engine, server, store } = harness();
        server.mailbox.addFolder(draftsFolder());
        await engine.synchronise();

        const drafts = (await store.folders()).find((folder) => folder.id === 'drafts');
        assert.equal(drafts?.role, 'drafts');
        assert.equal(drafts?.class, null);
        assert.equal(await store.cursor('drafts'), undefined);
    });

    it('does not prime a class it has no model for yet', async () => {
        const { engine, server, store } = harness();
        server.mailbox.addFolder(notesFolder());
        const report = await engine.synchronise();

        assert.equal((await store.folders()).length, 2);
        assert.equal(await store.cursor('notes'), undefined);
        assert.deepEqual(report.folders.map((outcome) => outcome.folderId), ['inbox']);
    });
});

describe('progress', () => {
    function recorder() {
        const seen: string[] = [];
        return {
            seen,
            onProgress: (progress: SyncProgress) => {
                seen.push(progress.kind === 'folders' ? 'folders' : `items:${progress.folderId}`);
            },
        };
    }

    it('announces the hierarchy before any items land', async () => {
        const { seen, onProgress } = recorder();
        const { engine, server } = harness({ engine: { onProgress } });
        messages(server.mailbox, 'inbox', 3);

        await engine.synchronise();

        assert.equal(seen[0], 'folders');
        assert.ok(seen.includes('items:inbox'));
    });

    it('reports each round of a drain rather than only the end of it', async () => {
        const { seen, onProgress } = recorder();
        const { engine, server } = harness({ engine: { windowSize: 2, onProgress } });
        messages(server.mailbox, 'inbox', 9);

        const report = await engine.synchronise();
        const rounds = seen.filter((entry) => entry === 'items:inbox');

        assert.equal(report.folders[0]?.delivered, 9);
        assert.ok(rounds.length >= 4, `only ${rounds.length} reports for 9 items at a window of 2`);
    });

    it('reports a folder as soon as it drains, not once the run finishes', async () => {
        const { seen, onProgress } = recorder();
        const { engine, server } = harness({ engine: { onProgress } });
        server.mailbox.addFolder(contactsFolder());
        messages(server.mailbox, 'inbox', 2);
        server.mailbox.addItem({ id: 'c1', folderId: 'contacts', data: contactItem() });

        await engine.synchronise();

        assert.deepEqual(seen, ['folders', 'items:inbox', 'items:contacts']);
    });

    it('says nothing when a run delivers nothing', async () => {
        const { seen, onProgress } = recorder();
        const { engine, server } = harness({ engine: { onProgress } });
        messages(server.mailbox, 'inbox', 3);
        await engine.synchronise();

        seen.length = 0;
        await engine.synchronise();

        assert.deepEqual(seen, []);
    });
});

describe('against a conformant server', () => {
    it('reaches the same mailbox as it does against ours', async () => {
        const { engine, server, store } = harness({ quirks: CONFORMANT });
        messages(server.mailbox, 'inbox', 4);

        const report = await engine.synchronise();

        assert.equal(report.recovery.kind, 'ok');
        assert.equal((await idsIn(store, 'inbox')).length, 4);
    });
});
