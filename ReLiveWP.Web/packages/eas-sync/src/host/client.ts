import type {
    Account,
    Contact,
    ContactQuery,
    Event,
    EventQuery,
    Folder,
    FolderCounts,
    Message,
    MessageQuery,
    Page,
    SearchQuery,
    Task,
    TaskQuery,
} from '@relivewp/eas-store';

import {
    CHANGE_CHANNEL,
    isResponse,
    type ChannelMessage,
    type EngineState,
    type FetchBodyResult,
    type HostEvent,
    type HostMessage,
    type HostRequest,
    type SyncSummary,
    type WorkerConfig,
} from './protocol.ts';

export interface EasClient {
    configure(config: WorkerConfig): Promise<Account>;
    sync(folderId?: string): Promise<SyncSummary>;
    wake(): Promise<void>;
    setAutoSync(enabled: boolean): Promise<void>;
    folders(): Promise<Folder[]>;
    counts(): Promise<Record<string, FolderCounts>>;
    account(): Promise<Account>;
    listMessages(query: MessageQuery): Promise<Page<Message>>;
    message(folderId: string, messageId: string): Promise<Message | undefined>;
    fetchBody(folderId: string, messageId: string): Promise<FetchBodyResult>;
    search(query: SearchQuery): Promise<Message[]>;
    listContacts(query: ContactQuery): Promise<Contact[]>;
    searchContacts(query: SearchQuery): Promise<Contact[]>;
    listFavourites(query: ContactQuery): Promise<Contact[]>;
    meContact(folderId: string): Promise<Contact | undefined>;
    contactPhotos(folderId: string, ids?: readonly string[]): Promise<Map<string, Blob>>;
    listEvents(query: EventQuery): Promise<Event[]>;
    listTasks(query: TaskQuery): Promise<Task[]>;
    readonly state: EngineState;
    on(listener: (event: HostEvent) => void): () => void;
    close(): void;
}

interface Pending {
    resolve: (value: unknown) => void;
    reject: (reason: unknown) => void;
}

export function connectWorker(worker: Worker): EasClient {
    const pending = new Map<number, Pending>();
    const listeners = new Set<(event: HostEvent) => void>();

    let nextId = 1;
    let state: EngineState = {
        status: 'idle',
        leader: false,
        lastError: null,
        lastSyncAt: null,
        autoSync: true,
        nextAttemptAt: null,
    };

    const emit = (event: HostEvent): void => {
        if (event.kind === 'state') state = event.state;
        for (const listener of listeners) listener(event);
    };

    worker.addEventListener('message', (event: MessageEvent<HostMessage>) => {
        const message = event.data;

        if (!isResponse(message)) {
            emit(message);
            return;
        }

        const waiting = pending.get(message.id);
        if (waiting === undefined) return;
        pending.delete(message.id);

        if (message.kind === 'ok') waiting.resolve(message.result);
        else waiting.reject(Object.assign(new Error(message.message), { name: message.name }));
    });

    // another tab's worker did the writing, this one won't hear about it any other way. our own
    // worker stays the authority on whether we hold the lease; everything else follows the leader.
    const channel = new BroadcastChannel(CHANGE_CHANNEL);
    channel.addEventListener('message', (event: MessageEvent<ChannelMessage>) => {
        const message = event.data;

        if (message.kind === 'changed' || message.kind === 'folders') 
            emit(message);
        else if (message.kind === 'leader-state' && !state.leader)
            emit({ kind: 'state', state: { ...message.state, leader: false } });
    });

    // Omit over a union keeps only the shared keys, so it has to distribute
    type WithoutId<T> = T extends { id: number } ? Omit<T, 'id'> : never;

    function send<T>(request: WithoutId<HostRequest>): Promise<T> {
        return new Promise<T>((resolve, reject) => {
            const id = nextId++;
            pending.set(id, { resolve: resolve as (value: unknown) => void, reject });
            worker.postMessage({ ...request, id } as HostRequest);
        });
    }

    return {
        configure: (config) => send<Account>({ kind: 'configure', config }),
        sync: (folderId) => send<SyncSummary>({ kind: 'sync', folderId }),
        wake: () => send<void>({ kind: 'wake' }),
        setAutoSync: (enabled) => send<void>({ kind: 'setAutoSync', enabled }),
        folders: () => send<Folder[]>({ kind: 'folders' }),
        counts: () => send<Record<string, FolderCounts>>({ kind: 'counts' }),
        account: () => send<Account>({ kind: 'account' }),
        listMessages: (query) => send<Page<Message>>({ kind: 'listMessages', query }),
        fetchBody: (folderId, messageId) =>
            send<FetchBodyResult>({ kind: 'fetchBody', folderId, messageId }),
        message: (folderId, messageId) =>
            send<Message | undefined>({ kind: 'message', folderId, messageId }),
        search: (query) => send<Message[]>({ kind: 'search', query }),
        listContacts: (query) => send<Contact[]>({ kind: 'listContacts', query }),
        searchContacts: (query) => send<Contact[]>({ kind: 'searchContacts', query }),
        listFavourites: (query) => send<Contact[]>({ kind: 'listFavourites', query }),
        meContact: (folderId) => send<Contact | undefined>({ kind: 'meContact', folderId }),
        contactPhotos: (folderId, ids) =>
            send<Map<string, Blob>>({ kind: 'contactPhotos', folderId, ids }),
        listEvents: (query) => send<Event[]>({ kind: 'listEvents', query }),
        listTasks: (query) => send<Task[]>({ kind: 'listTasks', query }),
        get state() {
            return state;
        },
        on(listener) {
            listeners.add(listener);
            return () => listeners.delete(listener);
        },
        close() {
            channel.close();
            worker.terminate();
            for (const waiting of pending.values()) waiting.reject(new Error('worker closed'));
            pending.clear();
        },
    };
}
