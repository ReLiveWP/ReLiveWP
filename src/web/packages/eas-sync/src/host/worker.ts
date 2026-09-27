import { EasSession, EasTransport } from '@relivewp/eas-client';
import { openStore } from '@relivewp/eas-store/idb';
import type { Account, EasStore } from '@relivewp/eas-store';

import { SyncEngine } from '../engine/engine.ts';
import { Scheduler } from '../schedule/scheduler.ts';
import { Leader } from './leader.ts';
import {
    CHANGE_CHANNEL,
    type ChannelMessage,
    type Credentials,
    type EngineState,
    type FetchBodyResult,
    type HostEvent,
    type HostRequest,
    type HostResponse,
    type SyncSummary,
    type WorkerConfig,
} from './protocol.ts';

interface WorkerScope {
    addEventListener(type: 'message', listener: (event: MessageEvent) => void): void;
    postMessage(message: unknown): void;
}

function authorization(credentials: Credentials): string {
    if (credentials.kind === 'bearer') return `Bearer ${credentials.token}`;

    const { user, password } = credentials;
    return `Basic ${btoa(`${user}:${password}`)}`;
}

class WorkerHost {
    private readonly scope: WorkerScope;
    private readonly channel: BroadcastChannel;
    private readonly holderId = crypto.randomUUID();

    private store: EasStore | undefined;
    private credentials: Credentials | undefined;
    private engine: SyncEngine | undefined;
    private leader: Leader | undefined;
    private scheduler: Scheduler | undefined;
    private running: Promise<SyncSummary> | undefined;
    private chain: Promise<unknown> = Promise.resolve();
    private autoSync = true;

    private state: EngineState = {
        status: 'idle',
        leader: false,
        lastError: null,
        lastSyncAt: null,
        autoSync: true,
        nextAttemptAt: null,
    };

    constructor(scope: WorkerScope) {
        this.scope = scope;
        this.channel = new BroadcastChannel(CHANGE_CHANNEL);
        scope.addEventListener('message', (event) => void this.dispatch(event.data as HostRequest));
        this.channel.addEventListener('message', (event: MessageEvent<ChannelMessage>) => {
            if (event.data.kind === 'sync' && this.state.leader) void this.sync(event.data.folderId);
        });
    }

    private exclusive<T>(work: () => Promise<T>): Promise<T> {
        const next = this.chain.then(work, work);
        this.chain = next.then(() => undefined, () => undefined);
        return next;
    }

    private async dispatch(request: HostRequest): Promise<void> {
        try {
            this.reply({ kind: 'ok', id: request.id, result: await this.handle(request) });
        } catch (error) {
            const failure = error instanceof Error ? error : new Error(String(error));
            this.patch({ status: 'error', lastError: failure.message });
            this.reply({
                kind: 'error', id: request.id, name: failure.name, message: failure.message,
            });
        }
    }

    private async handle(request: HostRequest): Promise<unknown> {
        if (request.kind === 'configure') return this.exclusive(() => this.configure(request.config));

        if (request.kind === 'credentials') {
            this.credentials = request.credentials;
            return undefined;
        }

        const store = this.require();

        switch (request.kind) {
            case 'sync':
                return this.sync(request.folderId);
            case 'wake':
                this.scheduler?.wake();
                return undefined;
            case 'setAutoSync':
                this.autoSync = request.enabled;
                this.scheduler?.setEnabled(request.enabled);
                this.patch({ autoSync: request.enabled });
                return undefined;
            case 'folders':
                return store.folders();
            case 'counts':
                return store.counts();
            case 'account':
                return store.account();
            case 'listMessages':
                return store.listMessages(request.query);
            case 'message':
                return store.message(request.folderId, request.messageId);
            case 'fetchBody':
                return this.fetchBody(request.folderId, request.messageId);
            case 'search':
                return store.searchMessages(request.query);
            case 'listContacts':
                return store.listContacts(request.query);
            case 'listFavourites':
                return store.listFavourites(request.query);
            case 'meContact':
                return store.meContact(request.folderId);
            case 'contactPhotos':
                return store.contactPhotos(request.folderId, request.ids);
            case 'searchContacts':
                return store.searchContacts(request.query);
            case 'listEvents':
                return store.listEvents(request.query);
            case 'listTasks':
                return store.listTasks(request.query);
        }
    }

    private require(): EasStore {
        if (this.store === undefined) throw new Error('the worker has not been configured yet');
        return this.store;
    }

    private async configure(config: WorkerConfig): Promise<Account> {
        this.patch({ status: 'configuring' });
        this.credentials = config.credentials;
        await this.leader?.stop();
        await this.scheduler?.stop();

        const store = await openStore({
            userId: config.userId,
            deviceId: config.deviceId,
            deviceType: config.deviceType,
            ...(config.databaseName === undefined ? {} : { name: config.databaseName }),
        });

        // the stored id wins over the caller's: it is the one this database's sync keys were
        // issued against. an empty patch is load-or-create, so a first run persists it now
        // rather than at first provision.
        const account = await store.saveAccount({});

        const transport = new EasTransport({
            endpoint: config.endpoint,
            deviceId: account.deviceId,
            deviceType: account.deviceType,
            authorization: () => authorization(this.credentials ?? config.credentials),
            ...(config.protocolVersion === undefined
                ? {}
                : { protocolVersion: config.protocolVersion }),
        });

        const session = new EasSession(transport, {
            deviceInformation: { model: 'ReLiveWP Web', os: 'Web' },
            ...(account.policyKey === null ? {} : { policyKey: account.policyKey }),
        });

        this.store = store;
        const engine = new SyncEngine(session, store, {
            ...(config.windowSize === undefined ? {} : { windowSize: config.windowSize }),
            ...(config.annotations === undefined ? {} : { annotations: config.annotations }),
            onProgress: (progress) => {
                this.broadcast(progress.kind === 'folders'
                    ? { kind: 'folders' }
                    : { kind: 'changed', folderIds: [progress.folderId] });
            },
        });

        const stamped = async <T>(work: () => Promise<T>): Promise<T> => {
            const result = await this.exclusive(work);
            this.patch({ lastSyncAt: Date.now() });
            return result;
        };

        this.engine = engine;
        this.scheduler = new Scheduler(session, store, {
            synchronise: () => stamped(() => engine.synchronise()),
            applyPing: (folderIds) => stamped(() => engine.applyPing(folderIds)),
        }, {
            enabled: this.autoSync,
            ...(config.heartbeatSeconds === undefined
                ? {}
                : { heartbeatSeconds: config.heartbeatSeconds }),
            onStatus: (status) => {
                if (status.phase === 'syncing') return this.patch({ status: 'syncing' });

                this.patch({
                    status: status.phase === 'stopped' ? 'idle' : status.phase,
                    nextAttemptAt: status.nextAttemptAt,
                    ...(status.lastError === null ? {} : { lastError: status.lastError }),
                });
            },
        });

        this.leader = new Leader({
            store,
            holderId: this.holderId,
            onChange: (leader) => {
                this.patch({ leader });

                if (leader) this.scheduler?.start();
                else void this.scheduler?.stop();
            },
        });

        await this.leader.start();
        this.patch({ status: 'idle', lastError: null });

        return account;
    }

    private async fetchBody(folderId: string, messageId: string): Promise<FetchBodyResult> {
        const engine = this.engine;
        if (engine === undefined) throw new Error('the worker has not been configured yet');

        const outcome = await engine.fetchBody(folderId, messageId);
        const reason = outcome.recovery.kind === 'ok' ? null : describe(outcome.recovery);

        if (outcome.fetched) this.broadcast({ kind: 'changed', folderIds: [folderId] });

        return { ok: reason === null, reason, fetched: outcome.fetched, message: outcome.message };
    }

    private async sync(folderId: string | undefined): Promise<SyncSummary> {
        if (this.leader !== undefined && !(await this.leader.beat())) {
            this.channel.postMessage({ kind: 'sync', folderId } satisfies ChannelMessage);
            return { ok: true, reason: null, delegated: true, folders: [] };
        }

        const existing = this.running;
        if (existing !== undefined) return existing;

        const started = this.run(folderId).finally(() => {
            this.running = undefined;
        });

        this.running = started;
        return started;
    }

    private async run(folderId: string | undefined): Promise<SyncSummary> {
        const engine = this.engine;
        if (engine === undefined) throw new Error('the worker has not been configured yet');

        this.patch({ status: 'syncing' });

        const outcomes = await this.exclusive(async () => folderId === undefined
            ? engine.synchronise()
            : { recovery: { kind: 'ok' as const }, folders: [await engine.syncFolder(folderId)] });

        const failed = outcomes.folders.find((outcome) => outcome.recovery.kind !== 'ok');
        const reason = outcomes.recovery.kind !== 'ok'
            ? describe(outcomes.recovery)
            : failed === undefined ? null : describe(failed.recovery);

        this.patch({
            status: reason === null ? 'idle' : 'error',
            lastError: reason,
            lastSyncAt: Date.now(),
        });

        this.scheduler?.wake(false);

        return {
            ok: reason === null,
            reason,
            delegated: false,
            folders: outcomes.folders.map((outcome) => ({
                folderId: outcome.folderId,
                delivered: outcome.delivered,
                removed: outcome.removed,
                rounds: outcome.rounds,
            })),
        };
    }

    private patch(patch: Partial<EngineState>): void {
        this.state = { ...this.state, ...patch };
        this.reply({ kind: 'state', state: this.state });

        if (this.state.leader)
            this.channel.postMessage({ kind: 'leader-state', state: this.state });
    }

    private broadcast(event: HostEvent): void {
        this.reply(event);
        if (event.kind !== 'state') this.channel.postMessage(event);
    }

    private reply(message: HostResponse | HostEvent): void {
        this.scope.postMessage(message);
    }
}

function describe(recovery: { kind: string; reason?: string }): string {
    return recovery.reason ?? recovery.kind;
}

export function runWorkerHost(scope: WorkerScope = self as unknown as WorkerScope): void {
    new WorkerHost(scope);
}
