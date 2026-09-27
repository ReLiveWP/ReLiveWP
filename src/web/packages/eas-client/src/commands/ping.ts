import { elements, int, pick, text } from '../nodes/read.ts';
import { Ping as P } from '../generated/tags.g.ts';
import { opt } from '../nodes/tags.ts';
import { textOf, type EasNode } from '../nodes/node.ts';
import type { PingClass } from './classes.ts';

export interface PingFolder {
    id: string;
    class: PingClass;
}

export interface PingRequest {
    heartbeatInterval?: number | undefined;
    folders?: PingFolder[] | undefined;
}

export interface PingResponse {
    status: number | undefined;
    folderIds: string[];
    heartbeatInterval: number | undefined;
    maxFolders: number | undefined;
    node: EasNode;
}

export function buildPing(request: PingRequest): EasNode {
    return P.Ping(
        opt(request.heartbeatInterval, P.HeartbeatInterval),
        request.folders === undefined
            ? undefined
            : P.Folders(request.folders.map((folder) =>
                P.Folder(P.Id(folder.id), P.Class(folder.class)))));
}

// same tag, different shape. a request Folder has Id and Class children, a response Folder is
// just the id as text.
export function parsePing(root: EasNode): PingResponse {
    return {
        status: int(root, P.Status),
        folderIds: elements(pick(root, P.Folders))
            .filter((node) => node.name === 'Folder')
            .map((node) => text(node, P.Id) ?? textOf(node))
            .filter((id) => id.length > 0),
        heartbeatInterval: int(root, P.HeartbeatInterval),
        maxFolders: int(root, P.MaxFolders),
        node: root,
    };
}
