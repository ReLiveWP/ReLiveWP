import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { Scheduler, type SchedulerOptions } from '../src/index.ts';
import { harness } from './harness.ts';
import { contactsFolder } from './server/server.ts';
import { messages } from './server/items.ts';

async function settle(turns = 60): Promise<void> {
    for (let i = 0; i < turns; i++) await new Promise<void>((resolve) => { setImmediate(resolve); });
}

interface Waiter {
    due: number;
    resolve: () => void;
}

function clock(start = 100_000) {
    let at = start;
    let waiters: Waiter[] = [];

    return {
        now: () => at,
        sleep: (ms: number, signal: AbortSignal) => new Promise<void>((resolve) => {
            if (signal.aborted) return resolve();

            const waiter: Waiter = { due: at + ms, resolve };
            waiters.push(waiter);

            signal.addEventListener('abort', () => {
                waiters = waiters.filter((held) => held !== waiter);
                resolve();
            }, { once: true });
        }),
        async advance(ms: number): Promise<void> {
            at += ms;

            const due = waiters.filter((waiter) => waiter.due <= at);
            waiters = waiters.filter((waiter) => waiter.due > at);
            for (const waiter of due) waiter.resolve();

            await settle();
        },
    };
}

function scheduled(options: SchedulerOptions = {}, seedInbox = true) {
    const parts = harness({ seedInbox });
    const time = clock();
    const scheduler = new Scheduler(parts.session, parts.store, parts.engine, {
        now: time.now,
        sleep: time.sleep,
        minCycleMs: 0,
        ...options,
    });

    return { ...parts, time, scheduler };
}

const pingsIn = (requests: { command: string }[]) =>
    requests.filter((request) => request.command === 'Ping').length;

describe('the ping loop', () => {
    it('syncs first, then pings the collections it primed', async () => {
        const { scheduler, server } = scheduled();
        messages(server.mailbox, 'inbox', 2);
        server.pingReturns({ status: 2, folderIds: ['inbox'] });

        scheduler.start();
        await settle();
        await scheduler.stop();

        assert.equal(server.pings[0]?.heartbeatInterval, 600);
        assert.deepEqual(server.pings[0]?.folderIds, ['inbox']);

        const firstPing = server.requests.findIndex((request) => request.command === 'Ping');
        const syncAfter = server.requests.findIndex((request, index) =>
            index > firstPing && request.command === 'Sync' && request.collectionId === 'inbox');

        assert.ok(firstPing >= 0, 'never pinged');
        assert.ok(syncAfter > firstPing, 'status 2 did not lead to a sync of the named folder');
    });

    // MS-ASCMD 2.2.3.177.11 status 2: sync each folder the response names, then ping again
    it('syncs only the folders the response names', async () => {
        const { scheduler, server } = scheduled();
        server.mailbox.addFolder(contactsFolder());
        server.pingReturns({ status: 2, folderIds: ['contacts'] });

        scheduler.start();
        await settle();

        const firstPing = server.requests.findIndex((request) => request.command === 'Ping');
        const after = server.requests
            .slice(firstPing)
            .filter((request) => request.command === 'Sync')
            .map((request) => request.collectionId);

        assert.deepEqual(after, ['contacts']);

        await scheduler.stop();
    });

    it('syncs nothing when the response names a folder it is not monitoring', async () => {
        const { scheduler, server } = scheduled();
        server.pingReturns({ status: 2, folderIds: ['some-other-mailbox'] });

        scheduler.start();
        await settle();

        const firstPing = server.requests.findIndex((request) => request.command === 'Ping');
        const after = server.requests
            .slice(firstPing)
            .filter((request) => request.command === 'Sync');

        assert.deepEqual(after, [], 'resynced folders the server never named');
        assert.equal(pingsIn(server.requests), 2, 'did not go straight back to pinging');

        await scheduler.stop();
    });

    it('parks on the held ping rather than spinning', async () => {
        const { scheduler, server } = scheduled();
        server.holdPings();

        scheduler.start();
        await settle();

        assert.equal(pingsIn(server.requests), 1);

        await scheduler.stop();
    });

    it('adopts the heartbeat the server hands back', async () => {
        const { scheduler, server } = scheduled();
        server.pingReturns({ status: 5, heartbeatInterval: 120 });

        scheduler.start();
        await settle();

        assert.equal(server.pings[0]?.heartbeatInterval, 600);
        assert.equal(server.pings[1]?.heartbeatInterval, 120);
        assert.equal(scheduler.status.heartbeatSeconds, 120);

        await scheduler.stop();
    });

    it('trims the folder set when the server says there are too many, keeping the inbox', async () => {
        const { scheduler, server } = scheduled();
        server.mailbox.addFolder(contactsFolder());
        server.pingReturns({ status: 6, maxFolders: 1 });

        scheduler.start();
        await settle();

        assert.equal(server.pings[0]?.folderIds.length, 2);
        assert.deepEqual(server.pings[1]?.folderIds, ['inbox']);

        await scheduler.stop();
    });

    it('relearns the hierarchy when the server says the folder list is stale', async () => {
        const { scheduler, server } = scheduled();
        server.pingReturns({ status: 7 });

        scheduler.start();
        await settle();

        const firstPing = server.requests.findIndex((request) => request.command === 'Ping');
        const resync = server.requests.findIndex((request, index) =>
            index > firstPing && request.command === 'FolderSync');

        assert.ok(resync > firstPing, 'status 7 did not trigger a FolderSync');

        await scheduler.stop();
    });
});

describe('when things go wrong', () => {
    it('backs off instead of hammering, and comes back when the wait is up', async () => {
        const { scheduler, server, time } = scheduled();
        server.holdPings();
        server.scriptStatus('Ping', 4);

        scheduler.start();
        await settle();

        assert.equal(pingsIn(server.requests), 1);
        assert.equal(scheduler.status.phase, 'backoff');
        assert.equal(scheduler.status.failures, 1);
        assert.equal(scheduler.status.nextAttemptAt, time.now() + 5000);

        await time.advance(4999);
        assert.equal(pingsIn(server.requests), 1, 'retried before the backoff was up');

        await time.advance(1);
        assert.equal(pingsIn(server.requests), 2);

        await scheduler.stop();
    });

    it('does not treat an expired heartbeat as a failure', async () => {
        const { scheduler, server } = scheduled();
        server.pingReturns({ status: 1 });

        scheduler.start();
        await settle();

        assert.equal(scheduler.status.phase, 'waiting');
        assert.equal(scheduler.status.failures, 0);
        assert.equal(pingsIn(server.requests), 2);

        await scheduler.stop();
    });
});

describe('waking and pausing', () => {
    it('a wake cuts the held ping short and resyncs', async () => {
        const { scheduler, server } = scheduled();
        server.holdPings();

        scheduler.start();
        await settle();
        assert.equal(pingsIn(server.requests), 1);

        const before = server.requests.length;
        scheduler.wake();
        await settle();

        const resync = server.requests.findIndex((request, index) =>
            index >= before && request.command === 'FolderSync');

        assert.ok(resync >= before, 'a wake did not force a sync');
        assert.equal(pingsIn(server.requests), 2);

        await scheduler.stop();
    });

    it('makes no requests at all while auto-sync is off', async () => {
        const { scheduler, server } = scheduled({ enabled: false });
        server.holdPings();

        scheduler.start();
        await settle();

        assert.equal(server.requests.length, 0);
        assert.equal(scheduler.status.phase, 'paused');

        scheduler.setEnabled(true);
        await settle();

        assert.ok(server.requests.length > 0, 'turning auto-sync back on did not start it');
        assert.equal(scheduler.status.phase, 'waiting');

        await scheduler.stop();
    });

    it('stops cleanly while parked on a held ping', async () => {
        const { scheduler, server } = scheduled();
        server.holdPings();

        scheduler.start();
        await settle();
        await scheduler.stop();

        assert.equal(scheduler.status.phase, 'stopped');
        assert.equal(scheduler.status.nextAttemptAt, null);
    });
});
