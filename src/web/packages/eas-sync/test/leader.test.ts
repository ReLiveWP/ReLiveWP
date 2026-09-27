import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { MemoryStore } from '@relivewp/eas-store';

import { Leader } from '../src/host.ts';

function pair() {
    const store = new MemoryStore({ userId: 'u', deviceId: 'D1', deviceType: 'Browser' });
    let clock = 0;

    const make = (holderId: string, seen: boolean[]) => new Leader({
        store,
        holderId,
        leaseMs: 15_000,
        now: () => clock,
        onChange: (leader) => seen.push(leader),
    });

    return { store, make, advance: (ms: number) => { clock += ms; } };
}

describe('leader election', () => {
    it('gives the lease to whoever asks first', async () => {
        const { make } = pair();
        const oneSaw: boolean[] = [];
        const twoSaw: boolean[] = [];

        assert.equal(await make('one', oneSaw).beat(), true);
        assert.equal(await make('two', twoSaw).beat(), false);
        assert.deepEqual(oneSaw, [true]);
        assert.deepEqual(twoSaw, []);
    });

    it('keeps it while the holder keeps renewing', async () => {
        const { make, advance } = pair();
        const one = make('one', []);
        const two = make('two', []);

        await one.beat();
        for (let i = 0; i < 5; i++) {
            advance(5_000);
            assert.equal(await one.beat(), true);
            assert.equal(await two.beat(), false);
        }
    });

    it('hands over once the holder stops renewing', async () => {
        const { make, advance } = pair();
        const oneSaw: boolean[] = [];
        const one = make('one', oneSaw);
        const two = make('two', []);

        await one.beat();
        advance(20_000);

        assert.equal(await two.beat(), true);
        assert.equal(await one.beat(), false);
        assert.deepEqual(oneSaw, [true, false]);
    });

    it('reports a change of leadership exactly once per change', async () => {
        const { make } = pair();
        const seen: boolean[] = [];
        const one = make('one', seen);

        await one.beat();
        await one.beat();
        await one.beat();

        assert.deepEqual(seen, [true]);
        assert.equal(one.isLeader, true);
    });

    // a closed tab leaves a lease that still looks fresh, the released lock is the only signal
    it('takes a fresh lease outright when told the holder is gone', async () => {
        const { make } = pair();
        const one = make('one', []);
        const twoSaw: boolean[] = [];
        const two = make('two', twoSaw);

        await one.beat();
        assert.equal(await two.beat(), false);

        assert.equal(await two.beat(true), true);
        assert.deepEqual(twoSaw, [true]);
        assert.equal(await one.beat(), false);
    });

    it('claims the lease as soon as the lock is granted, without waiting for staleness', async () => {
        const { store, make, advance } = pair();
        const one = make('one', []);
        await one.beat();

        let grant: (() => void) | undefined;
        const locks = {
            request: async (
                _name: string,
                _options: { mode: 'exclusive'; signal?: AbortSignal },
                callback: () => Promise<void>) => {
                await new Promise<void>((resolve) => { grant = resolve; });
                await callback();
            },
        };

        const seen: boolean[] = [];
        const two = new Leader({
            store, holderId: 'two', leaseMs: 15_000, now: () => 0, locks,
            onChange: (leader) => seen.push(leader),
        });

        await two.start();
        assert.equal(two.isLeader, false, 'still a follower while the lock is held elsewhere');

        advance(1_000);
        grant?.();
        await new Promise((resolve) => setTimeout(resolve, 0));

        assert.equal(two.isLeader, true);
        assert.deepEqual(seen, [true]);
        assert.equal((await store.readLease())?.holderId, 'two');
        await two.stop();
    });

    it('frees the lease on stop so the next tab does not wait it out', async () => {
        const { store, make } = pair();
        const one = make('one', []);
        const two = make('two', []);

        await one.beat();
        await one.stop();

        assert.equal(await store.readLease(), undefined);
        assert.equal(await two.beat(), true);
        assert.equal(one.isLeader, false);
    });
});
