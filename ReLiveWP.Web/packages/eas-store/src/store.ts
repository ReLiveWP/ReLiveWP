import type {
    Account,
    Contact,
    Cursor,
    CursorOptions,
    Event,
    Folder,
    ItemClass,
    Lease,
    Message,
    Task,
} from './model.ts';

export interface CursorInit {
    folderId: string;
    class: ItemClass;
    options: CursorOptions;
    supportedHash: string | null;
}

export interface FolderChangeSet {
    fromSyncKey: string;
    toSyncKey: string;
    syncedAt: number;
    upsert: Folder[];
    remove: string[];
}

interface ChangeSetBase {
    folderId: string;
    fromSyncKey: string;
    toSyncKey: string;
    syncedAt: number;
    moreAvailable: boolean;
    remove: string[];
}

export interface MessageChangeSet extends ChangeSetBase {
    class: 'Email';
    upsert: Message[];
}

export interface ContactPhotoChange {
    id: string;
    bytes: Uint8Array;
}

export interface ContactChangeSet extends ChangeSetBase {
    class: 'Contact';
    upsert: Contact[];
    photos: ContactPhotoChange[];
}

export interface EventChangeSet extends ChangeSetBase {
    class: 'Calendar';
    upsert: Event[];
}

export interface TaskChangeSet extends ChangeSetBase {
    class: 'Task';
    upsert: Task[];
}

export type ChangeSet = MessageChangeSet | ContactChangeSet | EventChangeSet | TaskChangeSet;

export type ApplyResult =
    | { applied: true }
    | { applied: false; reason: 'stale-cursor'; found: string | null };

export interface MessageKey {
    receivedAt: number;
    id: string;
}

export interface MessageQuery {
    folderId: string;
    limit: number;
    before?: MessageKey | undefined;
    unreadOnly?: boolean | undefined;
}

export interface SearchQuery {
    text: string;
    limit: number;
    folderId?: string | undefined;
}

export interface ContactQuery {
    folderId: string;
    limit: number;
}

// half open window, `to` is exclusive
export interface EventQuery {
    folderId: string;
    limit: number;
    from?: number | undefined;
    to?: number | undefined;
}

export interface TaskQuery {
    folderId: string;
    limit: number;
    includeComplete?: boolean | undefined;
}

export interface Page<T> {
    items: T[];
    next: MessageKey | null;
}

export interface FolderCounts {
    total: number;
    unread: number;
}

export interface EasStore {
    account(): Promise<Account>;
    saveAccount(patch: Partial<Account>): Promise<Account>;

    folders(): Promise<Folder[]>;
    applyFolderChanges(change: FolderChangeSet): Promise<ApplyResult>;

    cursors(): Promise<Cursor[]>;
    cursor(folderId: string): Promise<Cursor | undefined>;
    ensureCursor(init: CursorInit): Promise<Cursor>;
    replaceCursor(init: CursorInit): Promise<Cursor>;
    applyChanges(change: ChangeSet): Promise<ApplyResult>;
    markFailed(folderId: string, error: string): Promise<void>;
    resetFolder(folderId: string): Promise<void>;

    listMessages(query: MessageQuery): Promise<Page<Message>>;
    message(folderId: string, id: string): Promise<Message | undefined>;
    searchMessages(query: SearchQuery): Promise<Message[]>;

    patchMessage(
        folderId: string, id: string, patch: Partial<Message>): Promise<Message | undefined>;

    listContacts(query: ContactQuery): Promise<Contact[]>;
    contact(folderId: string, id: string): Promise<Contact | undefined>;
    searchContacts(query: SearchQuery): Promise<Contact[]>;
    listFavourites(query: ContactQuery): Promise<Contact[]>;
    meContact(folderId: string): Promise<Contact | undefined>;
    contactPhoto(folderId: string, id: string): Promise<Blob | undefined>;
    // bulk because the caller is usually a list crossing a worker boundary, and one round trip
    // per row would be the expensive part. omit `ids` for the whole folder
    contactPhotos(folderId: string, ids?: readonly string[]): Promise<Map<string, Blob>>;

    listEvents(query: EventQuery): Promise<Event[]>;
    event(folderId: string, id: string): Promise<Event | undefined>;

    listTasks(query: TaskQuery): Promise<Task[]>;
    task(folderId: string, id: string): Promise<Task | undefined>;

    counts(): Promise<Record<string, FolderCounts>>;

    readLease(): Promise<Lease | undefined>;
    claimLease(candidate: Lease, staleAfterMs: number): Promise<boolean>;
    renewLease(lease: Lease): Promise<boolean>;
    releaseLease(holderId: string): Promise<void>;

    wipe(): Promise<void>;
    close(): Promise<void>;
}

export function newAccount(userId: string, deviceId: string, deviceType: string): Account {
    return {
        userId,
        deviceId,
        deviceType,
        endpoint: null,
        protocolVersion: null,
        policyKey: null,
        provisionedAt: null,
        folderSyncKey: '0',
        heartbeatInterval: null,
        maxFolders: null,
        policy: null,
    };
}

export function newCursor(init: CursorInit): Cursor {
    return {
        folderId: init.folderId,
        class: init.class,
        syncKey: '0',
        primed: false,
        moreAvailable: false,
        supportedHash: init.supportedHash,
        options: init.options,
        lastSyncedAt: null,
        lastError: null,
    };
}

export function stale(found: string | null): ApplyResult {
    return { applied: false, reason: 'stale-cursor', found };
}

export function messageKey(message: Message): MessageKey {
    return { receivedAt: message.receivedAt, id: message.id };
}

export function compareMessageKeys(a: MessageKey, b: MessageKey): number {
    if (a.receivedAt !== b.receivedAt) return b.receivedAt - a.receivedAt;
    if (a.id === b.id) return 0;
    return a.id < b.id ? 1 : -1;
}

export const APPLIED: ApplyResult = { applied: true };

// the wire declares no format, so read it off the bytes rather than assert one
function mediaType(bytes: Uint8Array): string {
    if (bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47)
        return 'image/png';
    if (bytes[0] === 0x47 && bytes[1] === 0x49 && bytes[2] === 0x46) return 'image/gif';
    if (bytes[0] === 0x42 && bytes[1] === 0x4d) return 'image/bmp';
    return 'image/jpeg';
}

// stored as a Blob rather than the raw bytes: both backends persist it as-is, and it crosses a
// worker boundary by reference instead of copying every picture
export function toPhotoBlob(bytes: Uint8Array): Blob {
    return new Blob([bytes.slice()], { type: mediaType(bytes) });
}
