import type { CollectionClass, FolderChange, PingClass } from '@relivewp/eas-client';
import type { Folder, FolderRole, ItemClass } from '@relivewp/eas-store';

interface FolderKind {
    role: FolderRole;
    class: ItemClass | null;
}

// drafts can't be synced at any version we speak, and the recipient cache isn't items at all
const KINDS: Readonly<Record<number, FolderKind>> = {
    1: { role: 'user', class: null },
    2: { role: 'inbox', class: 'Email' },
    3: { role: 'drafts', class: null },
    4: { role: 'deleted', class: 'Email' },
    5: { role: 'sent', class: 'Email' },
    6: { role: 'outbox', class: 'Email' },
    7: { role: 'tasks', class: 'Task' },
    8: { role: 'calendar', class: 'Calendar' },
    9: { role: 'contacts', class: 'Contact' },
    10: { role: 'notes', class: 'Note' },
    11: { role: 'user', class: null },
    12: { role: 'user', class: 'Email' },
    13: { role: 'calendar', class: 'Calendar' },
    14: { role: 'contacts', class: 'Contact' },
    15: { role: 'tasks', class: 'Task' },
    16: { role: 'user', class: null },
    17: { role: 'notes', class: 'Note' },
    18: { role: 'user', class: null },
    19: { role: 'user', class: null },
};

const UNKNOWN: FolderKind = { role: 'user', class: null };

const COLLECTION_CLASS: Readonly<Record<ItemClass, PingClass>> = {
    Email: 'Email',
    Contact: 'Contacts',
    Calendar: 'Calendar',
    Task: 'Tasks',
    Note: 'Notes',
};

export function folderKind(type: number | undefined): FolderKind {
    return (type === undefined ? undefined : KINDS[type]) ?? UNKNOWN;
}

export function collectionClassOf(item: ItemClass): CollectionClass {
    return COLLECTION_CLASS[item];
}

export function pingClassOf(item: ItemClass): PingClass {
    return COLLECTION_CLASS[item];
}

export function readFolder(change: FolderChange): Folder | undefined {
    if (change.serverId === undefined) return undefined;

    const kind = folderKind(change.type);
    return {
        id: change.serverId,
        parentId: change.parentId === undefined || change.parentId === '0'
            ? null
            : change.parentId,
        name: change.displayName ?? '',
        type: change.type ?? 18,
        role: kind.role,
        class: kind.class,
    };
}
