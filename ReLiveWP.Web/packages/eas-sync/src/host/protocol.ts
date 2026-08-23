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

export type Credentials =
    | { kind: 'basic'; user: string; password: string }
    | { kind: 'bearer'; token: string };

export interface WorkerConfig {
    userId: string;
    endpoint: string;
    deviceId: string;
    deviceType: string;
    credentials: Credentials;
    protocolVersion?: string | undefined;
    windowSize?: number | undefined;
    databaseName?: string | undefined;
    heartbeatSeconds?: number | undefined;
    /** 
     * requests Windows Live contact annotations, don't use this against your every day run of the mill
     * Exchange server, it'll get quite sad. 
     */
    annotations?: readonly string[] | undefined;
}

export interface FetchBodyResult {
    ok: boolean;
    reason: string | null;
    fetched: boolean;
    message: Message | undefined;
}

export interface SyncSummary {
    ok: boolean;
    reason: string | null;
    delegated: boolean;
    folders: { folderId: string; delivered: number; removed: number; rounds: number }[];
}

export type EngineStatus =
    | 'idle'
    | 'configuring'
    | 'syncing'
    | 'error'
    | 'waiting'
    | 'backoff'
    | 'paused';

export interface EngineState {
    status: EngineStatus;
    leader: boolean;
    lastError: string | null;
    lastSyncAt: number | null;
    autoSync: boolean;
    nextAttemptAt: number | null;
}

export type HostRequest =
    | { kind: 'configure'; id: number; config: WorkerConfig }
    | { kind: 'credentials'; id: number; credentials: Credentials }
    | { kind: 'sync'; id: number; folderId?: string | undefined }
    | { kind: 'wake'; id: number }
    | { kind: 'setAutoSync'; id: number; enabled: boolean }
    | { kind: 'folders'; id: number }
    | { kind: 'counts'; id: number }
    | { kind: 'account'; id: number }
    | { kind: 'listMessages'; id: number; query: MessageQuery }
    | { kind: 'message'; id: number; folderId: string; messageId: string }
    | { kind: 'fetchBody'; id: number; folderId: string; messageId: string }
    | { kind: 'search'; id: number; query: SearchQuery }
    | { kind: 'listContacts'; id: number; query: ContactQuery }
    | { kind: 'searchContacts'; id: number; query: SearchQuery }
    | { kind: 'listFavourites'; id: number; query: ContactQuery }
    | { kind: 'meContact'; id: number; folderId: string }
    | { kind: 'contactPhotos'; id: number; folderId: string; ids?: readonly string[] | undefined }
    | { kind: 'listEvents'; id: number; query: EventQuery }
    | { kind: 'listTasks'; id: number; query: TaskQuery };

export type HostResponse =
    | { kind: 'ok'; id: number; result: unknown }
    | { kind: 'error'; id: number; name: string; message: string };

export type HostEvent =
    | { kind: 'changed'; folderIds: string[] }
    | { kind: 'folders' }
    | { kind: 'state'; state: EngineState };

export type HostMessage = HostResponse | HostEvent;

export type ChannelMessage =
    | { kind: 'changed'; folderIds: string[] }
    | { kind: 'folders' }
    | { kind: 'leader-state'; state: EngineState }
    | { kind: 'sync'; folderId?: string | undefined };

export interface ResultOf {
    configure: Account;
    credentials: void;
    sync: SyncSummary;
    folders: Folder[];
    counts: Record<string, FolderCounts>;
    account: Account;
    listMessages: Page<Message>;
    message: Message | undefined;
    fetchBody: FetchBodyResult;
    search: Message[];
    listContacts: Contact[];
    searchContacts: Contact[];
    listFavourites: Contact[];
    meContact: Contact | undefined;
    contactPhotos: Map<string, Blob>;
    listEvents: Event[];
    listTasks: Task[];
}

export const CHANGE_CHANNEL = 'relivewp-eas';

export function isResponse(message: HostMessage): message is HostResponse {
    return message.kind === 'ok' || message.kind === 'error';
}
