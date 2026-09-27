export const DATABASE_VERSION = 2;

export const META = 'meta';
export const FOLDERS = 'folders';
export const CURSORS = 'cursors';
export const MESSAGES = 'messages';
export const CONTACTS = 'contacts';
export const EVENTS = 'events';
export const TASKS = 'tasks';
export const NOTES = 'notes';
export const ATTACHMENTS = 'attachments';
export const CONTACT_PHOTOS = 'contactPhotos';
export const CHANGES = 'changes';
export const OUTBOX = 'outbox';

export const ITEM_STORES = [MESSAGES, CONTACTS, EVENTS, TASKS, NOTES, ATTACHMENTS] as const;
export const ALL_STORES = [
    META, FOLDERS, CURSORS, ...ITEM_STORES, CONTACT_PHOTOS, CHANGES, OUTBOX,
] as const;

export const BY_DATE = 'byDate';
export const BY_UNREAD = 'byUnread';
export const BY_SEARCH = 'bySearch';
export const BY_CONVERSATION = 'byConversation';
export const BY_SORT_NAME = 'bySortName';
export const BY_START = 'byStart';
export const BY_UID = 'byUid';

export const ACCOUNT_KEY = 'account';
export const LEASE_KEY = 'lease';

export function databaseName(userId: string): string {
    return `relivewp-mail-${userId}`;
}

export function upgrade(
    db: IDBDatabase, oldVersion: number, newVersion: number = DATABASE_VERSION): void {
    if (oldVersion < 1 && newVersion >= 1) upgradeToV1(db);
    if (oldVersion < 2 && newVersion >= 2) upgradeToV2(db);
}

function upgradeToV2(db: IDBDatabase): void {
    db.createObjectStore(CONTACT_PHOTOS, { keyPath: ['folderId', 'id'] });
}

function upgradeToV1(db: IDBDatabase): void {
    db.createObjectStore(META, { keyPath: 'key' });

    const folders = db.createObjectStore(FOLDERS, { keyPath: 'id' });
    folders.createIndex('byParent', 'parentId');
    folders.createIndex('byRole', 'role');

    db.createObjectStore(CURSORS, { keyPath: 'folderId' });

    const messages = db.createObjectStore(MESSAGES, { keyPath: ['folderId', 'id'] });
    messages.createIndex(BY_DATE, ['folderId', 'receivedAt', 'id']);
    messages.createIndex(BY_UNREAD, ['folderId', 'unread']);
    messages.createIndex(BY_SEARCH, 'tokens', { multiEntry: true });
    messages.createIndex(BY_CONVERSATION, 'conversationId');

    const contacts = db.createObjectStore(CONTACTS, { keyPath: ['folderId', 'id'] });
    contacts.createIndex(BY_SORT_NAME, ['folderId', 'sortName']);
    contacts.createIndex(BY_SEARCH, 'tokens', { multiEntry: true });

    const events = db.createObjectStore(EVENTS, { keyPath: ['folderId', 'id'] });
    events.createIndex(BY_START, ['folderId', 'startAt']);
    events.createIndex(BY_UID, 'uid');

    db.createObjectStore(TASKS, { keyPath: ['folderId', 'id'] });
    db.createObjectStore(NOTES, { keyPath: ['folderId', 'id'] });
    db.createObjectStore(ATTACHMENTS, { keyPath: ['folderId', 'messageId', 'id'] });

    const changes = db.createObjectStore(CHANGES, { keyPath: 'id', autoIncrement: true });
    changes.createIndex('byFolder', 'folderId');

    db.createObjectStore(OUTBOX, { keyPath: 'id', autoIncrement: true });
}

export function folderRange(folderId: string): IDBKeyRange {
    return IDBKeyRange.bound([folderId], [folderId, []]);
}

export function olderThan(folderId: string, receivedAt: number, id: string): IDBKeyRange {
    return IDBKeyRange.bound([folderId], [folderId, receivedAt, id], false, true);
}

export function prefix(term: string): IDBKeyRange {
    return IDBKeyRange.bound(term, `${term}\uffff`);
}
