import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { itemOperationsRecovery } from '../src/index.ts';
import { harness } from './harness.ts';
import { mailItem } from './server/items.ts';

const WHOLE = `<html><body><p>${'the rest of it '.repeat(40)}</p></body></html>`;

function truncatedMessage(server: ReturnType<typeof harness>['server'], clip = 32): void {
    server.mailbox.addItem({
        id: 'm1',
        folderId: 'inbox',
        data: mailItem({ subject: 'Long one', body: WHOLE, truncateTo: clip }),
        whole: mailItem({ subject: 'Long one', body: WHOLE }),
    });
}

describe('fetching a truncated body', () => {
    it('replaces the clipped body with the whole one', async () => {
        const { engine, server, store } = harness();
        truncatedMessage(server);
        await engine.synchronise();

        const before = await store.message('inbox', 'm1');
        assert.equal(before?.body?.truncated, true);
        assert.equal(before?.body?.fullSize, WHOLE.length);
        assert.ok((before?.body?.content.length ?? 0) < WHOLE.length);

        const outcome = await engine.fetchBody('inbox', 'm1');

        assert.equal(outcome.recovery.kind, 'ok');
        assert.equal(outcome.fetched, true);
        assert.equal(outcome.message?.body?.content, WHOLE);
        assert.equal(outcome.message?.body?.truncated, false);

        const after = await store.message('inbox', 'm1');
        assert.equal(after?.body?.content, WHOLE);
    });

    it('leaves everything else about the message alone', async () => {
        const { engine, server, store } = harness();
        truncatedMessage(server);
        await engine.synchronise();

        const before = await store.message('inbox', 'm1');
        await engine.fetchBody('inbox', 'm1');
        const after = await store.message('inbox', 'm1');

        assert.equal(after?.subject, before?.subject);
        assert.equal(after?.receivedAt, before?.receivedAt);
        assert.deepEqual(after?.from, before?.from);
        assert.equal(after?.read, before?.read);
    });

    it('is still findable by search afterwards, so the tokens were rederived', async () => {
        const { engine, server, store } = harness();
        truncatedMessage(server);
        await engine.synchronise();
        await engine.fetchBody('inbox', 'm1');

        const hits = await store.searchMessages({ text: 'Long', limit: 10 });
        assert.deepEqual(hits.map((message) => message.id), ['m1']);
    });

    it('does not go to the server when the body is already whole', async () => {
        const { engine, server } = harness();
        server.mailbox.addItem({ id: 'm1', folderId: 'inbox', data: mailItem({ body: 'short' }) });
        await engine.synchronise();

        const outcome = await engine.fetchBody('inbox', 'm1');

        assert.equal(outcome.recovery.kind, 'ok');
        assert.equal(outcome.fetched, false);
        assert.equal(
            server.requests.filter((request) => request.command === 'ItemOperations').length, 0);
    });

    it('asks for the item by collection and server id', async () => {
        const { engine, server } = harness();
        truncatedMessage(server);
        await engine.synchronise();
        await engine.fetchBody('inbox', 'm1');

        const asked = server.requests.filter((request) => request.command === 'ItemOperations');
        assert.equal(asked.length, 1);
        assert.equal(asked[0]?.collectionId, 'inbox');
    });

    it('reports a message that is not in the store', async () => {
        const { engine } = harness();
        await engine.synchronise();

        const outcome = await engine.fetchBody('inbox', 'nope');

        assert.equal(outcome.recovery.kind, 'fail');
        assert.equal(outcome.message, undefined);
    });

    it('keeps the truncated body when the server refuses', async () => {
        const { engine, server, store } = harness();
        truncatedMessage(server);
        await engine.synchronise();

        server.scriptStatus('ItemOperations', 16);
        const outcome = await engine.fetchBody('inbox', 'm1');

        assert.equal(outcome.recovery.kind, 'fail');
        assert.equal(outcome.fetched, false);
        assert.equal(outcome.message?.body?.truncated, true);
        assert.equal((await store.message('inbox', 'm1'))?.body?.truncated, true);
    });

    it('reprovisions and tries again on 142', async () => {
        const { engine, server, store } = harness();
        truncatedMessage(server);
        await engine.synchronise();

        server.scriptStatus('ItemOperations', 142);
        const outcome = await engine.fetchBody('inbox', 'm1');

        assert.equal(outcome.recovery.kind, 'ok');
        assert.equal(outcome.fetched, true);
        assert.equal((await store.message('inbox', 'm1'))?.body?.content, WHOLE);
    });
});

describe('itemOperations statuses', () => {
    it('treats success and partial success as ok', () => {
        assert.equal(itemOperationsRecovery(1).kind, 'ok');
        assert.equal(itemOperationsRecovery(17).kind, 'ok');
        assert.equal(itemOperationsRecovery(undefined).kind, 'ok');
    });

    it('retries the transient ones', () => {
        assert.equal(itemOperationsRecovery(3).kind, 'retry');
        assert.equal(itemOperationsRecovery(12).kind, 'retry');
    });

    it('defers to the common table above 100', () => {
        assert.equal(itemOperationsRecovery(142).kind, 'reprovision');
        assert.equal(itemOperationsRecovery(140).kind, 'wipe');
        assert.equal(itemOperationsRecovery(111).kind, 'retry');
    });

    it('gives up on the item specific refusals', () => {
        for (const status of [2, 8, 9, 10, 11, 14, 15, 16, 18])
            assert.equal(itemOperationsRecovery(status).kind, 'fail', `status ${status}`);
    });
});
