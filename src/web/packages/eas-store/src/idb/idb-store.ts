import {
    isFavourite,
    isMeContact,
    type Account,
    type Contact,
    type Cursor,
    type Event,
    type Folder,
    type Item,
    type ItemClass,
    type Lease,
    type Message,
    type Task,
} from '../model.ts';
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
import { request, settled, step } from './requests.ts';
import {
    ACCOUNT_KEY,
    ALL_STORES,
    BY_DATE,
    BY_SEARCH,
    BY_SORT_NAME,
    BY_START,
    BY_UNREAD,
    CONTACTS,
    CONTACT_PHOTOS,
    CURSORS,
    DATABASE_VERSION,
    EVENTS,
    FOLDERS,
    ITEM_STORES,
    LEASE_KEY,
    META,
    MESSAGES,
    NOTES,
    TASKS,
    databaseName,
    folderRange,
    olderThan,
    prefix,
    upgrade,
} from './schema.ts';

export interface IdbStoreOptions {
    userId: string;
    deviceId: string;
    deviceType: string;
    factory?: IDBFactory | undefined;
    name?: string | undefined;
}

const STORE_FOR: Readonly<Record<ItemClass, string>> = {
    Email: MESSAGES,
    Contact: CONTACTS,
    Calendar: EVENTS,
    Task: TASKS,
    Note: NOTES,
};

interface Tokenised {
    tokens: string[];
}

type MessageRecord = Message & Tokenised & { unread: number };
type ContactRecord = Contact & Tokenised;

interface MetaRecord<T> {
    key: string;
    value: T;
}

// booleans aren't valid idb keys, so `unread` is a 0/1 mirror of `read` that can be indexed
function toStored(itemClass: ItemClass, item: Item): unknown {
    if (itemClass === 'Email') {
        const message = item as Message;
        return { ...message, tokens: messageTokens(message), unread: message.read ? 0 : 1 };
    }

    if (itemClass === 'Contact') {
        const contact = item as Contact;
        return { ...contact, tokens: contactTokens(contact) };
    }

    return item;
}

function fromMessage(record: MessageRecord): Message {
    const { tokens, unread, ...message } = record;
    return message;
}

function fromContact(record: ContactRecord): Contact {
    const { tokens, ...contact } = record;
    return contact;
}

interface PhotoRecord {
    folderId: string;
    id: string;
    blob: Blob;
}

export async function openStore(options: IdbStoreOptions): Promise<IdbStore> {
    const factory = options.factory ?? indexedDB;
    const name = options.name ?? databaseName(options.userId);

    const opening = factory.open(name, DATABASE_VERSION);
    opening.onupgradeneeded = (event) =>
        upgrade(opening.result, event.oldVersion, event.newVersion ?? DATABASE_VERSION);
    const db = await request(opening);

    const store = new IdbStore(db, options);
    await store.seed();
    return store;
}

export class IdbStore implements EasStore {
    private readonly db: IDBDatabase;
    private readonly identity: IdbStoreOptions;

    constructor(db: IDBDatabase, identity: IdbStoreOptions) {
        this.db = db;
        this.identity = identity;
    }

    async seed(): Promise<void> {
        const tx = this.db.transaction(META, 'readwrite');
        const meta = tx.objectStore(META);
        const existing = await request<MetaRecord<Account> | undefined>(meta.get(ACCOUNT_KEY));

        if (existing === undefined) meta.put({ key: ACCOUNT_KEY, value: this.fresh() });
        await settled(tx);
    }

    async account(): Promise<Account> {
        const tx = this.db.transaction(META, 'readonly');
        const found = await request<MetaRecord<Account> | undefined>(
            tx.objectStore(META).get(ACCOUNT_KEY));
        await settled(tx);
        return found?.value ?? this.fresh();
    }

    async saveAccount(patch: Partial<Account>): Promise<Account> {
        const tx = this.db.transaction(META, 'readwrite');
        const meta = tx.objectStore(META);
        const found = await request<MetaRecord<Account> | undefined>(meta.get(ACCOUNT_KEY));
        const merged = { ...(found?.value ?? this.fresh()), ...patch };

        meta.put({ key: ACCOUNT_KEY, value: merged });
        await settled(tx);
        return merged;
    }

    async folders(): Promise<Folder[]> {
        const tx = this.db.transaction(FOLDERS, 'readonly');
        const all = await request<Folder[]>(tx.objectStore(FOLDERS).getAll());
        await settled(tx);
        return all;
    }

    async applyFolderChanges(change: FolderChangeSet): Promise<ApplyResult> {
        const tx = this.db.transaction([META, FOLDERS, CURSORS, ...ITEM_STORES], 'readwrite');
        const meta = tx.objectStore(META);
        const found = await request<MetaRecord<Account> | undefined>(meta.get(ACCOUNT_KEY));
        const account = found?.value ?? this.fresh();

        if (account.folderSyncKey !== change.fromSyncKey) {
            await settled(tx);
            return stale(account.folderSyncKey);
        }

        const folders = tx.objectStore(FOLDERS);
        const cursors = tx.objectStore(CURSORS);

        for (const id of change.remove) {
            folders.delete(id);
            cursors.delete(id);
            for (const name of ITEM_STORES) tx.objectStore(name).delete(folderRange(id));
        }

        for (const folder of change.upsert) folders.put(folder);

        meta.put({ key: ACCOUNT_KEY, value: { ...account, folderSyncKey: change.toSyncKey } });
        await settled(tx);
        return APPLIED;
    }

    async cursors(): Promise<Cursor[]> {
        const tx = this.db.transaction(CURSORS, 'readonly');
        const all = await request<Cursor[]>(tx.objectStore(CURSORS).getAll());
        await settled(tx);
        return all;
    }

    async cursor(folderId: string): Promise<Cursor | undefined> {
        const tx = this.db.transaction(CURSORS, 'readonly');
        const found = await request<Cursor | undefined>(tx.objectStore(CURSORS).get(folderId));
        await settled(tx);
        return found;
    }

    async ensureCursor(init: CursorInit): Promise<Cursor> {
        const tx = this.db.transaction(CURSORS, 'readwrite');
        const cursors = tx.objectStore(CURSORS);
        const existing = await request<Cursor | undefined>(cursors.get(init.folderId));
        const result = existing ?? newCursor(init);

        if (existing === undefined) cursors.put(result);
        await settled(tx);
        return result;
    }

    async replaceCursor(init: CursorInit): Promise<Cursor> {
        const tx = this.db.transaction([CURSORS, ...ITEM_STORES], 'readwrite');
        for (const name of ITEM_STORES) tx.objectStore(name).delete(folderRange(init.folderId));

        const created = newCursor(init);
        tx.objectStore(CURSORS).put(created);

        await settled(tx);
        return created;
    }

    async applyChanges(change: ChangeSet): Promise<ApplyResult> {
        const name = STORE_FOR[change.class];
        const scope = change.class === 'Contact'
            ? [CURSORS, name, CONTACT_PHOTOS]
            : [CURSORS, name];

        const tx = this.db.transaction(scope, 'readwrite');
        const cursors = tx.objectStore(CURSORS);
        const cursor = await request<Cursor | undefined>(cursors.get(change.folderId));

        if (cursor === undefined || cursor.syncKey !== change.fromSyncKey) {
            await settled(tx);
            return stale(cursor?.syncKey ?? null);
        }

        const items = tx.objectStore(name);
        for (const id of change.remove) items.delete([change.folderId, id]);
        for (const item of change.upsert) items.put(toStored(change.class, item));

        if (change.class === 'Contact') {
            const photos = tx.objectStore(CONTACT_PHOTOS);
            for (const id of change.remove) photos.delete([change.folderId, id]);

            for (const photo of change.photos) {
                const key = [change.folderId, photo.id];
                if (photo.bytes.length === 0) photos.delete(key);
                else
                    photos.put({
                        folderId: change.folderId,
                        id: photo.id,
                        blob: toPhotoBlob(photo.bytes),
                    });
            }
        }

        cursors.put({
            ...cursor,
            syncKey: change.toSyncKey,
            primed: true,
            moreAvailable: change.moreAvailable,
            lastSyncedAt: change.syncedAt,
            lastError: null,
        });

        await settled(tx);
        return APPLIED;
    }

    async markFailed(folderId: string, error: string): Promise<void> {
        const tx = this.db.transaction(CURSORS, 'readwrite');
        const cursors = tx.objectStore(CURSORS);
        const cursor = await request<Cursor | undefined>(cursors.get(folderId));

        if (cursor !== undefined) cursors.put({ ...cursor, lastError: error });
        await settled(tx);
    }

    async resetFolder(folderId: string): Promise<void> {
        const scoped = [...ITEM_STORES, CONTACT_PHOTOS];
        const tx = this.db.transaction([CURSORS, ...scoped], 'readwrite');
        const cursors = tx.objectStore(CURSORS);
        const cursor = await request<Cursor | undefined>(cursors.get(folderId));

        for (const name of scoped) tx.objectStore(name).delete(folderRange(folderId));

        if (cursor !== undefined)
            cursors.put({
                ...cursor,
                syncKey: '0',
                primed: false,
                moreAvailable: false,
                lastSyncedAt: null,
                lastError: null,
            });

        await settled(tx);
    }

    async listMessages(query: MessageQuery): Promise<Page<Message>> {
        const tx = this.db.transaction(MESSAGES, 'readonly');
        const index = tx.objectStore(MESSAGES).index(BY_DATE);
        const range = query.before === undefined
            ? folderRange(query.folderId)
            : olderThan(query.folderId, query.before.receivedAt, query.before.id);

        const walk = index.openCursor(range, 'prev');
        const items: Message[] = [];
        let more = false;

        let cursor = await step(walk);
        while (cursor !== null) {
            const record = cursor.value as MessageRecord;
            if (query.unreadOnly !== true || record.unread === 1) {
                if (items.length === query.limit) {
                    more = true;
                    break;
                }
                items.push(fromMessage(record));
            }

            cursor.continue();
            cursor = await step(walk);
        }

        const last = items[items.length - 1];
        return { items, next: more && last !== undefined ? messageKey(last) : null };
    }

    async message(folderId: string, id: string): Promise<Message | undefined> {
        const found = await this.get<MessageRecord>(MESSAGES, folderId, id);
        return found === undefined ? undefined : fromMessage(found);
    }

    async patchMessage(
        folderId: string, id: string, patch: Partial<Message>): Promise<Message | undefined> {
        const tx = this.db.transaction(MESSAGES, 'readwrite');
        const messages = tx.objectStore(MESSAGES);
        const found = await request<MessageRecord | undefined>(messages.get([folderId, id]));

        if (found === undefined) {
            await settled(tx);
            return undefined;
        }

        const merged = { ...fromMessage(found), ...patch, folderId, id };
        messages.put(toStored('Email', merged));
        await settled(tx);

        return merged;
    }

    async searchMessages(query: SearchQuery): Promise<Message[]> {
        const hits = await this.searchIn<MessageRecord>(MESSAGES, query);
        hits.sort((a, b) => compareMessageKeys(messageKey(a), messageKey(b)));
        return hits.slice(0, query.limit).map(fromMessage);
    }

    async listContacts(query: ContactQuery): Promise<Contact[]> {
        const tx = this.db.transaction(CONTACTS, 'readonly');
        const all = await request<ContactRecord[]>(
            tx.objectStore(CONTACTS).index(BY_SORT_NAME).getAll(folderRange(query.folderId)));
        await settled(tx);
        return all.slice(0, query.limit).map(fromContact);
    }

    async contact(folderId: string, id: string): Promise<Contact | undefined> {
        const found = await this.get<ContactRecord>(CONTACTS, folderId, id);
        return found === undefined ? undefined : fromContact(found);
    }

    async searchContacts(query: SearchQuery): Promise<Contact[]> {
        const hits = await this.searchIn<ContactRecord>(CONTACTS, query);
        hits.sort((a, b) => a.sortName.localeCompare(b.sortName));
        return hits.slice(0, query.limit).map(fromContact);
    }

    async listFavourites(query: ContactQuery): Promise<Contact[]> {
        const all = await this.listContacts({ ...query, limit: Number.MAX_SAFE_INTEGER });
        return all
            .filter(isFavourite)
            .sort((a, b) => a.annotation!.favouriteOrder! - b.annotation!.favouriteOrder!)
            .slice(0, query.limit);
    }

    async meContact(folderId: string): Promise<Contact | undefined> {
        const all = await this.listContacts({ folderId, limit: Number.MAX_SAFE_INTEGER });
        return all.find(isMeContact);
    }

    async contactPhoto(folderId: string, id: string): Promise<Blob | undefined> {
        const found = await this.get<PhotoRecord>(CONTACT_PHOTOS, folderId, id);
        return found?.blob;
    }

    async contactPhotos(folderId: string, ids?: readonly string[]): Promise<Map<string, Blob>> {
        const tx = this.db.transaction(CONTACT_PHOTOS, 'readonly');
        const all = await request<PhotoRecord[]>(
            tx.objectStore(CONTACT_PHOTOS).getAll(folderRange(folderId)));
        await settled(tx);

        const wanted = ids === undefined ? undefined : new Set(ids);
        const out = new Map<string, Blob>();
        for (const record of all)
            if (wanted === undefined || wanted.has(record.id)) out.set(record.id, record.blob);

        return out;
    }

    // an event can start before the window and still land in it, so the range can't do the filtering
    async listEvents(query: EventQuery): Promise<Event[]> {
        const tx = this.db.transaction(EVENTS, 'readonly');
        const all = await request<Event[]>(
            tx.objectStore(EVENTS).index(BY_START).getAll(folderRange(query.folderId)));
        await settled(tx);

        return all
            .filter((event) => (query.from === undefined || event.endAt >= query.from)
                && (query.to === undefined || event.startAt < query.to))
            .slice(0, query.limit);
    }

    async event(folderId: string, id: string): Promise<Event | undefined> {
        return this.get<Event>(EVENTS, folderId, id);
    }

    async listTasks(query: TaskQuery): Promise<Task[]> {
        const tx = this.db.transaction(TASKS, 'readonly');
        const all = await request<Task[]>(tx.objectStore(TASKS).getAll(folderRange(query.folderId)));
        await settled(tx);

        return all
            .filter((task) => query.includeComplete === true || !task.complete)
            .sort((a, b) => (a.dueAt ?? Infinity) - (b.dueAt ?? Infinity))
            .slice(0, query.limit);
    }

    async task(folderId: string, id: string): Promise<Task | undefined> {
        return this.get<Task>(TASKS, folderId, id);
    }

    async counts(): Promise<Record<string, FolderCounts>> {
        const tx = this.db.transaction([FOLDERS, ...ITEM_STORES], 'readonly');
        const folders = await request<Folder[]>(tx.objectStore(FOLDERS).getAll());

        const counts: Record<string, FolderCounts> = {};
        for (const folder of folders) {
            if (folder.class === null) {
                counts[folder.id] = { total: 0, unread: 0 };
                continue;
            }

            const items = tx.objectStore(STORE_FOR[folder.class]);
            const total = await request<number>(items.count(folderRange(folder.id)));
            const unread = folder.class === 'Email'
                ? await request<number>(
                    items.index(BY_UNREAD).count(IDBKeyRange.only([folder.id, 1])))
                : 0;

            counts[folder.id] = { total, unread };
        }

        await settled(tx);
        return counts;
    }

    async readLease(): Promise<Lease | undefined> {
        const tx = this.db.transaction(META, 'readonly');
        const found = await request<MetaRecord<Lease> | undefined>(
            tx.objectStore(META).get(LEASE_KEY));
        await settled(tx);
        return found?.value;
    }

    async claimLease(candidate: Lease, staleAfterMs: number): Promise<boolean> {
        const tx = this.db.transaction(META, 'readwrite');
        const meta = tx.objectStore(META);
        const found = await request<MetaRecord<Lease> | undefined>(meta.get(LEASE_KEY));
        const held = found?.value;

        const free = held === undefined
            || held.holderId === candidate.holderId
            || candidate.renewedAt - held.renewedAt >= staleAfterMs;

        if (free) meta.put({ key: LEASE_KEY, value: candidate });
        await settled(tx);
        return free;
    }

    async renewLease(lease: Lease): Promise<boolean> {
        const tx = this.db.transaction(META, 'readwrite');
        const meta = tx.objectStore(META);
        const found = await request<MetaRecord<Lease> | undefined>(meta.get(LEASE_KEY));

        const owned = found?.value.holderId === lease.holderId;
        if (owned) meta.put({ key: LEASE_KEY, value: lease });

        await settled(tx);
        return owned;
    }

    async releaseLease(holderId: string): Promise<void> {
        const tx = this.db.transaction(META, 'readwrite');
        const meta = tx.objectStore(META);
        const found = await request<MetaRecord<Lease> | undefined>(meta.get(LEASE_KEY));

        if (found?.value.holderId === holderId) meta.delete(LEASE_KEY);
        await settled(tx);
    }

    async wipe(): Promise<void> {
        const tx = this.db.transaction([...ALL_STORES], 'readwrite');
        for (const name of ALL_STORES) tx.objectStore(name).clear();

        tx.objectStore(META).put({ key: ACCOUNT_KEY, value: this.fresh() });
        await settled(tx);
    }

    close(): Promise<void> {
        this.db.close();
        return Promise.resolve();
    }

    private async get<T>(name: string, folderId: string, id: string): Promise<T | undefined> {
        const tx = this.db.transaction(name, 'readonly');
        const found = await request<T | undefined>(tx.objectStore(name).get([folderId, id]));
        await settled(tx);
        return found;
    }

    private async searchIn<T extends { folderId: string; id: string } & Tokenised>(
        name: string, query: SearchQuery): Promise<T[]> {
        const terms = searchTerms(query.text);
        const first = terms[0];
        if (first === undefined) return [];

        const tx = this.db.transaction(name, 'readonly');
        const candidates = await request<T[]>(
            tx.objectStore(name).index(BY_SEARCH).getAll(prefix(first)));
        await settled(tx);

        const seen = new Set<string>();
        const hits: T[] = [];
        for (const record of candidates) {
            const key = `${record.folderId} ${record.id}`;
            if (seen.has(key)) continue;
            seen.add(key);

            if (query.folderId !== undefined && record.folderId !== query.folderId) continue;
            if (!matchesTerms(record.tokens, terms)) continue;
            hits.push(record);
        }

        return hits;
    }

    private fresh(): Account {
        return newAccount(this.identity.userId, this.identity.deviceId, this.identity.deviceType);
    }
}
