import type { EasChild } from '@relivewp/eas-client/nodes';

export interface FakeFolder {
    id: string;
    parentId: string;
    name: string;
    type: number;
}

export interface FakeItem {
    id: string;
    folderId: string;
    data: EasChild[];
    whole?: EasChild[];
}

export type EventKind = 'Add' | 'Change' | 'Delete';

export interface ChangeEvent {
    sequence: number;
    folderId: string;
    itemId: string;
    kind: EventKind;
}

export function key(folderId: string, id: string): string {
    return `${folderId}\u0000${id}`;
}

export class FakeMailbox {
    readonly folders: FakeFolder[] = [];
    readonly items = new Map<string, FakeItem>();
    readonly log: ChangeEvent[] = [];

    private sequence = 0;
    private hierarchySequence = 0;

    addFolder(folder: FakeFolder): void {
        this.folders.push(folder);
        this.hierarchySequence++;
    }

    removeFolder(id: string): void {
        const at = this.folders.findIndex((folder) => folder.id === id);
        if (at !== -1) this.folders.splice(at, 1);
        this.hierarchySequence++;
    }

    get hierarchyVersion(): number {
        return this.hierarchySequence;
    }

    addItem(item: FakeItem): FakeItem {
        this.items.set(key(item.folderId, item.id), item);
        this.record(item.folderId, item.id, 'Add');
        return item;
    }

    changeItem(folderId: string, id: string, data: EasChild[]): void {
        const existing = this.items.get(key(folderId, id));
        if (existing === undefined) return;

        this.items.set(key(folderId, id), { ...existing, data });
        this.record(folderId, id, 'Change');
    }

    deleteItem(folderId: string, id: string): void {
        const existing = this.items.get(key(folderId, id));
        if (existing === undefined) return;

        this.items.delete(key(folderId, id));
        this.record(folderId, id, 'Delete');
    }

    // everything after `since`, collapsed to one event per item
    since(folderId: string, from: number): ChangeEvent[] {
        const latest = new Map<string, ChangeEvent>();

        for (const event of this.log) {
            if (event.sequence <= from || event.folderId !== folderId) continue;
            latest.set(event.itemId, event);
        }

        return [...latest.values()].sort((a, b) => a.sequence - b.sequence);
    }

    get head(): number {
        return this.sequence;
    }

    private record(folderId: string, itemId: string, kind: EventKind): void {
        this.log.push({ sequence: ++this.sequence, folderId, itemId, kind });
    }
}
