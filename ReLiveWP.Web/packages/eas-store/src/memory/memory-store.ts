import type {
    Account,
    Contact,
    Cursor,
    Event,
    Folder,
    Item,
    ItemClass,
    Lease,
    Message,
    Task,
} from '../model.ts';
import { ITEM_CLASSES, isFavourite, isMeContact } from '../model.ts';
import { contactTokens, matchesTerms, messageTokens, searchTerms } from '../search.ts';
import {
    APPLIED,
    compareMessageKeys,
    messageKey,
    newAccount,
    newCursor,
    stale,
    toPhotoBlob,
    type ApplyResult,
    type ChangeSet,
    type ContactQuery,
    type CursorInit,
    type EasStore,
    type EventQuery,
    type FolderChangeSet,
    type FolderCounts,
    type MessageQuery,
    type Page,
    type SearchQuery,
    type TaskQuery,
} from '../store.ts';

export interface MemoryStoreOptions {
    userId: string;
    deviceId: string;
    deviceType: string;
}

interface Stored {
    item: Item;
    tokens: string[];
}

function tokensFor(itemClass: ItemClass, item: Item): string[] {
    if (itemClass === 'Email') return messageTokens(item as Message);
    if (itemClass === 'Contact') return contactTokens(item as Contact);
    return [];
}

export class MemoryStore implements EasStore {
    private readonly identity: MemoryStoreOptions;
    private current: Account;
    private folderMap = new Map<string, Folder>();
    private cursorMap = new Map<string, Cursor>();
    private itemMap = new Map<string, Map<string, Stored>>();
    private photoMap = new Map<string, Map<string, Blob>>();
    private lease: Lease | undefined;

    constructor(options: MemoryStoreOptions) {
        this.identity = options;
        this.current = newAccount(options.userId, options.deviceId, options.deviceType);
    }

    account(): Promise<Account> {
        return Promise.resolve(structuredClone(this.current));
    }

    saveAccount(patch: Partial<Account>): Promise<Account> {
        this.current = structuredClone({ ...this.current, ...patch });
        return this.account();
    }

    folders(): Promise<Folder[]> {
        return Promise.resolve(structuredClone([...this.folderMap.values()]));
    }

    applyFolderChanges(change: FolderChangeSet): Promise<ApplyResult> {
        if (this.current.folderSyncKey !== change.fromSyncKey)
            return Promise.resolve(stale(this.current.folderSyncKey));

        for (const id of change.remove) {
            this.folderMap.delete(id);
            this.cursorMap.delete(id);
            this.dropFolder(id);
        }

        for (const folder of change.upsert) this.folderMap.set(folder.id, structuredClone(folder));

        this.current.folderSyncKey = change.toSyncKey;
        return Promise.resolve(APPLIED);
    }

    cursors(): Promise<Cursor[]> {
        return Promise.resolve(structuredClone([...this.cursorMap.values()]));
    }

    cursor(folderId: string): Promise<Cursor | undefined> {
        const found = this.cursorMap.get(folderId);
        return Promise.resolve(found === undefined ? undefined : structuredClone(found));
    }

    ensureCursor(init: CursorInit): Promise<Cursor> {
        const existing = this.cursorMap.get(init.folderId);
        if (existing !== undefined) return Promise.resolve(structuredClone(existing));

        const created = newCursor(init);
        this.cursorMap.set(init.folderId, structuredClone(created));
        return Promise.resolve(created);
    }

    replaceCursor(init: CursorInit): Promise<Cursor> {
        this.dropFolder(init.folderId);

        const created = newCursor(init);
        this.cursorMap.set(init.folderId, structuredClone(created));
        return Promise.resolve(created);
    }

    applyChanges(change: ChangeSet): Promise<ApplyResult> {
        const cursor = this.cursorMap.get(change.folderId);
        if (cursor === undefined) return Promise.resolve(stale(null));
        if (cursor.syncKey !== change.fromSyncKey) return Promise.resolve(stale(cursor.syncKey));

        const items = this.ensureBucket(change.class, change.folderId);
        for (const id of change.remove) items.delete(id);
        for (const item of change.upsert)
            items.set(item.id, {
                item: structuredClone(item) as Item,
                tokens: tokensFor(change.class, item),
            });

        if (change.class === 'Contact') {
            const photos = this.ensurePhotos(change.folderId);
            for (const id of change.remove) photos.delete(id);

            for (const photo of change.photos) {
                if (photo.bytes.length === 0) photos.delete(photo.id);
                else photos.set(photo.id, toPhotoBlob(photo.bytes));
            }
        }

        cursor.syncKey = change.toSyncKey;
        cursor.primed = true;
        cursor.moreAvailable = change.moreAvailable;
        cursor.lastSyncedAt = change.syncedAt;
        cursor.lastError = null;
        return Promise.resolve(APPLIED);
    }

    markFailed(folderId: string, error: string): Promise<void> {
        const cursor = this.cursorMap.get(folderId);
        if (cursor !== undefined) cursor.lastError = error;
        return Promise.resolve();
    }

    resetFolder(folderId: string): Promise<void> {
        this.dropFolder(folderId);

        const cursor = this.cursorMap.get(folderId);
        if (cursor !== undefined) {
            cursor.syncKey = '0';
            cursor.primed = false;
            cursor.moreAvailable = false;
            cursor.lastSyncedAt = null;
            cursor.lastError = null;
        }

        return Promise.resolve();
    }

    listMessages(query: MessageQuery): Promise<Page<Message>> {
        const all = this.read<Message>('Email', query.folderId)
            .filter((message) => query.unreadOnly !== true || !message.read)
            .sort((a, b) => compareMessageKeys(messageKey(a), messageKey(b)));

        const before = query.before;
        const rest = before === undefined
            ? all
            : all.filter((message) => compareMessageKeys(messageKey(message), before) > 0);

        const items = structuredClone(rest.slice(0, query.limit));
        const last = items[items.length - 1];
        return Promise.resolve({
            items,
            next: rest.length > query.limit && last !== undefined ? messageKey(last) : null,
        });
    }

    message(folderId: string, id: string): Promise<Message | undefined> {
        return Promise.resolve(this.one<Message>('Email', folderId, id));
    }

    patchMessage(
        folderId: string, id: string, patch: Partial<Message>): Promise<Message | undefined> {
        const stored = this.itemMap.get(bucket('Email', folderId))?.get(id);
        if (stored === undefined) return Promise.resolve(undefined);

        const merged: Message = { ...stored.item as Message, ...patch, folderId, id };
        stored.item = merged;
        stored.tokens = tokensFor('Email', merged);

        return Promise.resolve(structuredClone(merged));
    }

    searchMessages(query: SearchQuery): Promise<Message[]> {
        const hits = this.search<Message>('Email', query);
        hits.sort((a, b) => compareMessageKeys(messageKey(a), messageKey(b)));
        return Promise.resolve(hits.slice(0, query.limit));
    }

    listContacts(query: ContactQuery): Promise<Contact[]> {
        const all = this.read<Contact>('Contact', query.folderId)
            .sort((a, b) => a.sortName.localeCompare(b.sortName));
        return Promise.resolve(structuredClone(all.slice(0, query.limit)));
    }

    contact(folderId: string, id: string): Promise<Contact | undefined> {
        return Promise.resolve(this.one<Contact>('Contact', folderId, id));
    }

    searchContacts(query: SearchQuery): Promise<Contact[]> {
        const hits = this.search<Contact>('Contact', query);
        hits.sort((a, b) => a.sortName.localeCompare(b.sortName));
        return Promise.resolve(hits.slice(0, query.limit));
    }

    listFavourites(query: ContactQuery): Promise<Contact[]> {
        const all = this.read<Contact>('Contact', query.folderId)
            .filter(isFavourite)
            .sort((a, b) => a.annotation!.favouriteOrder! - b.annotation!.favouriteOrder!);
        return Promise.resolve(structuredClone(all.slice(0, query.limit)));
    }

    meContact(folderId: string): Promise<Contact | undefined> {
        const found = this.read<Contact>('Contact', folderId).find(isMeContact);
        return Promise.resolve(found === undefined ? undefined : structuredClone(found));
    }

    // a Blob is immutable, so unlike every other read this one can hand out the stored value
    contactPhoto(folderId: string, id: string): Promise<Blob | undefined> {
        return Promise.resolve(this.photoMap.get(folderId)?.get(id));
    }

    contactPhotos(folderId: string, ids?: readonly string[]): Promise<Map<string, Blob>> {
        const found = this.photoMap.get(folderId);
        if (found === undefined) return Promise.resolve(new Map());
        if (ids === undefined) return Promise.resolve(new Map(found));

        const out = new Map<string, Blob>();
        for (const id of ids) {
            const blob = found.get(id);
            if (blob !== undefined) out.set(id, blob);
        }

        return Promise.resolve(out);
    }

    listEvents(query: EventQuery): Promise<Event[]> {
        const all = this.read<Event>('Calendar', query.folderId)
            .filter((event) => (query.from === undefined || event.endAt >= query.from)
                && (query.to === undefined || event.startAt < query.to))
            .sort((a, b) => a.startAt - b.startAt);

        return Promise.resolve(structuredClone(all.slice(0, query.limit)));
    }

    event(folderId: string, id: string): Promise<Event | undefined> {
        return Promise.resolve(this.one<Event>('Calendar', folderId, id));
    }

    listTasks(query: TaskQuery): Promise<Task[]> {
        const all = this.read<Task>('Task', query.folderId)
            .filter((task) => query.includeComplete === true || !task.complete)
            .sort((a, b) => (a.dueAt ?? Infinity) - (b.dueAt ?? Infinity));

        return Promise.resolve(structuredClone(all.slice(0, query.limit)));
    }

    task(folderId: string, id: string): Promise<Task | undefined> {
        return Promise.resolve(this.one<Task>('Task', folderId, id));
    }

    counts(): Promise<Record<string, FolderCounts>> {
        const counts: Record<string, FolderCounts> = {};

        for (const folder of this.folderMap.values()) {
            const items = folder.class === null
                ? undefined
                : this.itemMap.get(bucket(folder.class, folder.id));

            let unread = 0;
            if (folder.class === 'Email')
                for (const stored of items?.values() ?? [])
                    if (!(stored.item as Message).read) unread++;

            counts[folder.id] = { total: items?.size ?? 0, unread };
        }

        return Promise.resolve(counts);
    }

    readLease(): Promise<Lease | undefined> {
        return Promise.resolve(this.lease === undefined ? undefined : structuredClone(this.lease));
    }

    claimLease(candidate: Lease, staleAfterMs: number): Promise<boolean> {
        const held = this.lease;
        const free = held === undefined
            || held.holderId === candidate.holderId
            || candidate.renewedAt - held.renewedAt >= staleAfterMs;

        if (free) this.lease = structuredClone(candidate);
        return Promise.resolve(free);
    }

    renewLease(lease: Lease): Promise<boolean> {
        if (this.lease?.holderId !== lease.holderId) return Promise.resolve(false);

        this.lease = structuredClone(lease);
        return Promise.resolve(true);
    }

    releaseLease(holderId: string): Promise<void> {
        if (this.lease?.holderId === holderId) this.lease = undefined;
        return Promise.resolve();
    }

    wipe(): Promise<void> {
        this.folderMap = new Map();
        this.cursorMap = new Map();
        this.itemMap = new Map();
        this.photoMap = new Map();
        this.lease = undefined;
        this.current = newAccount(
            this.identity.userId, this.identity.deviceId, this.identity.deviceType);
        return Promise.resolve();
    }

    close(): Promise<void> {
        return Promise.resolve();
    }

    private read<T extends Item>(itemClass: ItemClass, folderId: string): T[] {
        const items = this.itemMap.get(bucket(itemClass, folderId));
        return [...(items?.values() ?? [])].map((stored) => stored.item as T);
    }

    private one<T extends Item>(itemClass: ItemClass, folderId: string, id: string): T | undefined {
        const found = this.itemMap.get(bucket(itemClass, folderId))?.get(id);
        return found === undefined ? undefined : structuredClone(found.item) as T;
    }

    private search<T extends Item>(itemClass: ItemClass, query: SearchQuery): T[] {
        const terms = searchTerms(query.text);
        if (terms.length === 0) return [];

        const hits: T[] = [];
        for (const [key, items] of this.itemMap) {
            if (!key.startsWith(`${itemClass}\u0000`)) continue;
            if (query.folderId !== undefined && key !== bucket(itemClass, query.folderId)) continue;

            for (const stored of items.values())
                if (matchesTerms(stored.tokens, terms)) hits.push(structuredClone(stored.item) as T);
        }

        return hits;
    }

    private ensureBucket(itemClass: ItemClass, folderId: string): Map<string, Stored> {
        const key = bucket(itemClass, folderId);
        const existing = this.itemMap.get(key);
        if (existing !== undefined) return existing;

        const created = new Map<string, Stored>();
        this.itemMap.set(key, created);
        return created;
    }

    private ensurePhotos(folderId: string): Map<string, Blob> {
        const existing = this.photoMap.get(folderId);
        if (existing !== undefined) return existing;

        const created = new Map<string, Blob>();
        this.photoMap.set(folderId, created);
        return created;
    }

    private dropFolder(folderId: string): void {
        for (const itemClass of ITEM_CLASSES) this.itemMap.delete(bucket(itemClass, folderId));
        this.photoMap.delete(folderId);
    }
}

// null separator, the one character a folder id can't contain
function bucket(itemClass: ItemClass, folderId: string): string {
    return `${itemClass}\u0000${folderId}`;
}
