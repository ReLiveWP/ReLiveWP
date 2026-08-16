import type { EasStore } from '@relivewp/eas-store';

export interface LeaderOptions {
    store: EasStore;
    holderId: string;
    name?: string | undefined;
    leaseMs?: number | undefined;
    renewMs?: number | undefined;
    now?: (() => number) | undefined;
    onChange?: ((leader: boolean) => void) | undefined;
    locks?: LockManagerLike | undefined;
}

export interface LockManagerLike {
    request(
        name: string,
        options: { mode: 'exclusive'; signal?: AbortSignal },
        callback: () => Promise<void>): Promise<void>;
}

function lockManager(): LockManagerLike | undefined {
    const candidate = (globalThis as { navigator?: { locks?: LockManagerLike } }).navigator?.locks;
    return typeof candidate?.request === 'function' ? candidate : undefined;
}

interface Settings {
    store: EasStore;
    holderId: string;
    name: string;
    leaseMs: number;
    renewMs: number;
    now: () => number;
    onChange: ((leader: boolean) => void) | undefined;
    locks: LockManagerLike | undefined;
}

// the lease decides who syncs, the lock just stops the losers spinning. a frozen tab holds its
// lock exactly like a live one, so the lock can't be the source of truth.
export class Leader {
    private readonly options: Settings;
    private timer: ReturnType<typeof setInterval> | undefined;
    private abort: AbortController | undefined;
    private held = false;

    constructor(options: LeaderOptions) {
        this.options = {
            store: options.store,
            holderId: options.holderId,
            name: options.name ?? 'eas-sync',
            leaseMs: options.leaseMs ?? 15_000,
            renewMs: options.renewMs ?? 5_000,
            now: options.now ?? (() => Date.now()),
            onChange: options.onChange,
            locks: options.locks ?? lockManager(),
        };
    }

    get isLeader(): boolean {
        return this.held;
    }

    async start(): Promise<void> {
        if (this.timer !== undefined) return;

        this.timer = setInterval(() => void this.beat(), this.options.renewMs);
        const { locks } = this.options;

        if (locks === undefined) {
            await this.beat();
            return;
        }

        this.abort = new AbortController();
        void locks
            .request(this.options.name, { mode: 'exclusive', signal: this.abort.signal },
                async () => {
                    // being granted it means the last holder is gone, which the lease can't see
                    await this.beat(true);
                    return this.hold();
                })
            .catch(() => undefined);

        await this.beat();
    }

    async stop(): Promise<void> {
        if (this.timer !== undefined) clearInterval(this.timer);
        this.timer = undefined;
        this.abort?.abort();
        this.abort = undefined;

        if (this.held) {
            this.held = false;
            this.options.onChange?.(false);
        }

        await this.options.store.releaseLease(this.options.holderId);
    }

    // only pass force when something else has already established the holder is gone
    async beat(force = false): Promise<boolean> {
        const candidate = { holderId: this.options.holderId, renewedAt: this.options.now() };
        const stale = force ? 0 : this.options.leaseMs;

        const won = this.held
            ? await this.options.store.renewLease(candidate)
                || await this.options.store.claimLease(candidate, stale)
            : await this.options.store.claimLease(candidate, stale);

        if (won !== this.held) {
            this.held = won;
            this.options.onChange?.(won);
        }

        return won;
    }

    private hold(): Promise<void> {
        return new Promise((resolve) => {
            this.abort?.signal.addEventListener('abort', () => resolve(), { once: true });
        });
    }
}
