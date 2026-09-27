import { elements, int, pick, text } from '../nodes/read.ts';
import { FolderHierarchy as F } from '../generated/tags.g.ts';
import type { EasNode } from '../nodes/node.ts';

export interface Folder {
    serverId: string | undefined;
    parentId: string | undefined;
    displayName: string | undefined;
    type: number | undefined;
}

export interface FolderChange extends Folder {
    kind: 'Add' | 'Update' | 'Delete';
}

export interface FolderSyncResponse {
    status: number | undefined;
    syncKey: string | undefined;
    count: number | undefined;
    changes: FolderChange[];
    node: EasNode;
}

export function buildFolderSync(syncKey: string): EasNode {
    return F.FolderSync(F.SyncKey(syncKey));
}

const KINDS = new Set(['Add', 'Update', 'Delete']);

export function parseFolderSync(root: EasNode): FolderSyncResponse {
    const changes = elements(pick(root, F.Changes))
        .filter((node) => KINDS.has(node.name))
        .map((node) => ({
            kind: node.name as FolderChange['kind'],
            serverId: text(node, F.ServerId),
            parentId: text(node, F.ParentId),
            displayName: text(node, F.DisplayName),
            type: int(node, F.Type),
        }));

    return {
        status: int(root, F.Status),
        syncKey: text(root, F.SyncKey),
        count: int(pick(root, F.Changes), F.Count),
        changes,
        node: root,
    };
}

