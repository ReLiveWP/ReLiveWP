import type { EasSession, PingFolder, PingResponse } from '@relivewp/eas-client';
import type { EasStore, Folder } from '@relivewp/eas-store';

import { pingRecovery } from '../engine/status.ts';
import { pingClassOf } from '../project/index.ts';

export interface SyncTarget {
    synchronise(): Promise<unknown>;
    applyPing(folderIds: readonly string[]): Promise<unknown>;
}

export const DEFAULT_HEARTBEAT_SECONDS = 600;
export const MIN_HEARTBEAT_SECONDS = 60;
export const MAX_HEARTBEAT_SECONDS = 3540;
export const DEFAULT_MAX_FOLDERS = 200;
export const BACKOFF_SECONDS: readonly number[] = [5, 15, 60, 300, 900];
export const MIN_CYCLE_MS = 2000;

export type SchedulerPhase = 'stopped' | 'paused' | 'syncing' | 'waiting' | 'backoff';

export interface SchedulerStatus {
    phase: SchedulerPhase;
    heartbeatSeconds: number;
    nextAttemptAt: number | null;
    failures: number;
    lastError: string | null;
}

export interface SchedulerOptions {
    now?: (() => number) | undefined;
    sleep?: ((ms: number, signal: AbortSignal) => Promise<void>) | undefined;
    heartbeatSeconds?: number | undefined;
    maxFolders?: number | undefined;
    backoffSeconds?: readonly number[] | undefined;
    minCycleMs?: number | undefined;
    enabled?: boolean | undefined;
    onStatus?: ((status: SchedulerStatus) => void) | undefined;
}

function clampHeartbeat(seconds: number): number {
    return Math.min(MAX_HEARTBEAT_SECONDS, Math.max(MIN_HEARTBEAT_SECONDS, Math.round(seconds)));
}

function waitFor(ms: number, signal: AbortSignal): Promise<void> {
    if (signal.aborted || ms <= 0) return Promise.resolve();

    return new Promise((resolve) => {
        const timer = setTimeout(done, ms);
        signal.addEventListener('abort', done, { once: true });

        function done(): void {
            clearTimeout(timer);
            signal.removeEventListener('abort', done);
            resolve();
        }
    });
}

// the inbox is what a reader is staring at, so it survives a trim to maxFolders
function byUrgency(left: Folder, right: Folder): number {
    const rank = (folder: Folder) =>
        folder.role === 'inbox' ? 0 : folder.class === 'Email' ? 1 : 2;

    return rank(left) - rank(right) || left.id.localeCompare(right.id);
}

export class Scheduler {
    private readonly session: EasSession;
    private readonly store: EasStore;
    private readonly engine: SyncTarget;
    private readonly options: SchedulerOptions;
    private readonly now: () => number;
    private readonly sleep: (ms: number, signal: AbortSignal) => Promise<void>;
    private readonly backoffSeconds: readonly number[];
    private readonly minCycleMs: number;

    private running = false;
    private enabled: boolean;
    private woken = false;
    private resync = true;
    private failures = 0;
    private heartbeat: number;
    private maxFolders: number;
    private interrupt: AbortController | undefined;
    private loop: Promise<void> | undefined;

    private state: SchedulerStatus;

    constructor(
        session: EasSession, store: EasStore, engine: SyncTarget, options: SchedulerOptions = {}) {
        this.session = session;
        this.store = store;
        this.engine = engine;
        this.options = options;
        this.now = options.now ?? (() => Date.now());
        this.sleep = options.sleep ?? waitFor;
        this.backoffSeconds = options.backoffSeconds ?? BACKOFF_SECONDS;
        this.minCycleMs = options.minCycleMs ?? MIN_CYCLE_MS;
        this.enabled = options.enabled ?? true;
        this.heartbeat = clampHeartbeat(options.heartbeatSeconds ?? DEFAULT_HEARTBEAT_SECONDS);
        this.maxFolders = options.maxFolders ?? DEFAULT_MAX_FOLDERS;

        this.state = {
            phase: 'stopped',
            heartbeatSeconds: this.heartbeat,
            nextAttemptAt: null,
            failures: 0,
            lastError: null,
        };
    }

    get status(): SchedulerStatus {
        return this.state;
    }

    start(): void {
        if (this.running) return;

        this.running = true;
        this.resync = true;
        this.loop = this.run();
    }

    async stop(): Promise<void> {
        if (!this.running) return;

        this.running = false;
        this.interrupt?.abort();

        const loop = this.loop;
        this.loop = undefined;
        await loop;
    }

    wake(resync = true): void {
        this.resync = this.resync || resync;
        this.woken = true;
        this.interrupt?.abort();
    }

    setEnabled(enabled: boolean): void {
        if (this.enabled === enabled) return;

        this.enabled = enabled;
        this.woken = true;
        this.interrupt?.abort();
    }

    private async run(): Promise<void> {
        while (this.running) {
            const started = this.now();

            try {
                await this.cycle();
                this.failures = 0;
            } catch (thrown) {
                if (!this.running) break;
                if (this.consumeWake()) continue;

                await this.backoff(thrown);
                continue;
            }

            if (!this.running) break;
            if (this.consumeWake()) continue;

            const spent = this.now() - started;
            if (spent < this.minCycleMs) await this.pause(this.minCycleMs - spent);
        }

        this.publish({ phase: 'stopped', nextAttemptAt: null });
    }

    private async cycle(): Promise<void> {
        if (!this.enabled) {
            this.publish({ phase: 'paused', nextAttemptAt: null });
            await this.pause(this.heartbeat * 1000);
            return;
        }

        if (this.resync) {
            this.resync = false;
            this.publish({ phase: 'syncing', nextAttemptAt: null });
            await this.engine.synchronise();
            if (!this.running || this.woken) return;
        }

        const folders = await this.pingFolders();
        if (folders.length === 0) {
            this.publish({ phase: 'waiting', nextAttemptAt: this.deadline() });
            await this.pause(this.heartbeat * 1000);
            this.resync = true;
            return;
        }

        this.publish({ phase: 'waiting', nextAttemptAt: this.deadline(), lastError: null });

        const controller = new AbortController();
        this.interrupt = controller;

        try {
            const { parsed } = await this.session.ping(
                { heartbeatInterval: this.heartbeat, folders },
                { signal: controller.signal });

            if (!this.running || this.woken) return;
            await this.dispatch(parsed, folders);
        } finally {
            this.interrupt = undefined;
        }
    }

    private async dispatch(
        parsed: PingResponse | undefined, folders: PingFolder[]): Promise<void> {
        const status = parsed?.status;

        // no body and Status 1 both mean the heartbeat expired with nothing to report
        if (parsed === undefined || status === undefined || status === 1) return;

        // sync exactly what the response names and nothing else, then ping again
        if (status === 2) {
            const monitored = new Set(folders.map((folder) => folder.id));
            const changed = parsed.folderIds.filter((id) => monitored.has(id));
            if (changed.length === 0) return;

            this.publish({ phase: 'syncing', nextAttemptAt: null });
            await this.engine.applyPing(changed);
            return;
        }

        if (status === 5) {
            this.heartbeat = clampHeartbeat(parsed.heartbeatInterval ?? this.heartbeat);
            this.publish({ heartbeatSeconds: this.heartbeat });
            return;
        }

        if (status === 6) {
            this.maxFolders = Math.max(1, parsed.maxFolders ?? folders.length - 1);
            return;
        }

        // 3 is the server saying it has no cached ping for this device. we always send a full
        // body, so there is nothing to correct and the next pass just asks again.
        if (status === 3) return;

        const recovery = pingRecovery(status);
        if (recovery.kind === 'fail') throw new Error(recovery.reason);

        this.resync = true;
    }

    private async pingFolders(): Promise<PingFolder[]> {
        const [folders, cursors] = await Promise.all([this.store.folders(), this.store.cursors()]);
        const primed = new Set(cursors.filter((cursor) => cursor.primed).map((c) => c.folderId));

        return folders
            .filter((folder) => folder.class !== null && primed.has(folder.id))
            .sort(byUrgency)
            .slice(0, this.maxFolders)
            .flatMap((folder) => folder.class === null
                ? []
                : [{ id: folder.id, class: pingClassOf(folder.class) }]);
    }

    private async backoff(thrown: unknown): Promise<void> {
        const reason = thrown instanceof Error ? thrown.message : String(thrown);
        const index = Math.min(this.failures, this.backoffSeconds.length - 1);
        const seconds = this.backoffSeconds[index] ?? 0;

        this.failures++;
        this.resync = true;
        this.publish({
            phase: 'backoff',
            nextAttemptAt: this.now() + seconds * 1000,
            failures: this.failures,
            lastError: reason,
        });

        await this.pause(seconds * 1000);
    }

    private async pause(ms: number): Promise<void> {
        const controller = new AbortController();
        this.interrupt = controller;

        try {
            await this.sleep(ms, controller.signal);
        } finally {
            this.interrupt = undefined;
        }
    }

    private consumeWake(): boolean {
        if (!this.woken) return false;

        this.woken = false;
        return true;
    }

    private deadline(): number {
        return this.now() + this.heartbeat * 1000;
    }

    private publish(patch: Partial<SchedulerStatus>): void {
        this.state = { ...this.state, ...patch };
        this.options.onStatus?.(this.state);
    }
}
