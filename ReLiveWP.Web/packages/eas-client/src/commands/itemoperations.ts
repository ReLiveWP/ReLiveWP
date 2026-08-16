import { elements, int, pick, text } from '../nodes/read.ts';
import {
    AirSync as A,
    AirSyncBase as AB,
    DocumentLibrary as DL,
    ItemOperations as IO,
    RightsManagement as RM,
    Search as SE,
} from '../generated/tags.g.ts';
import { opt } from '../nodes/tags.ts';
import { isOpaque, isText, type EasNode } from '../nodes/node.ts';
import { buildBodyPreference, type BodyPreference } from './sync.ts';

export const MAILBOX = 'Mailbox';
export const DOCUMENT_LIBRARY = 'Document Library';

export type ItemStore = typeof MAILBOX | typeof DOCUMENT_LIBRARY | (string & {});

export interface FetchOptions {
    schema?: EasNode[] | undefined;
    range?: { start: number; end: number } | undefined;
    userName?: string | undefined;
    password?: string | undefined;
    mimeSupport?: number | undefined;
    bodyPreference?: BodyPreference[] | undefined;
    bodyPartPreference?: BodyPreference[] | undefined;
    rightsManagementSupport?: boolean | undefined;
}

export interface FetchOperation {
    kind: 'Fetch';
    store: ItemStore;
    serverId?: string | undefined;
    collectionId?: string | undefined;
    linkId?: string | undefined;
    longId?: string | undefined;
    fileReference?: string | undefined;
    removeRightsManagementProtection?: boolean | undefined;
    options?: FetchOptions | undefined;
}

export interface EmptyFolderContentsOperation {
    kind: 'EmptyFolderContents';
    collectionId: string;
    deleteSubFolders?: boolean | undefined;
}

export interface MoveOperation {
    kind: 'Move';
    conversationId: string;
    dstFldId: string;
    moveAlways?: boolean | undefined;
}

export type ItemOperation = FetchOperation | EmptyFolderContentsOperation | MoveOperation;

export interface ItemOperationsRequest {
    operations: ItemOperation[];
}

export interface FetchResponse {
    kind: 'Fetch';
    status: number | undefined;
    class: string | undefined;
    serverId: string | undefined;
    collectionId: string | undefined;
    linkId: string | undefined;
    longId: string | undefined;
    fileReference: string | undefined;
    properties: EasNode | undefined;
    data: Uint8Array | undefined;
    part: number | undefined;
    total: number | undefined;
    range: string | undefined;
    version: string | undefined;
    node: EasNode;
}

export interface EmptyFolderContentsResponse {
    kind: 'EmptyFolderContents';
    status: number | undefined;
    collectionId: string | undefined;
    node: EasNode;
}

export interface MoveResponse {
    kind: 'Move';
    status: number | undefined;
    conversationId: string | undefined;
    node: EasNode;
}

export type ItemOperationResponse = FetchResponse | EmptyFolderContentsResponse | MoveResponse;

export interface ItemOperationsResponse {
    status: number | undefined;
    operations: ItemOperationResponse[];
    node: EasNode;
}

function fetchOptions(options: FetchOptions): EasNode | undefined {
    const children = [
        ...(options.schema ?? []).map((node) => IO.Schema(node.children)),
        ...(options.range === undefined
            ? []
            : [IO.Range(`${options.range.start}-${options.range.end}`)]),
        ...(options.userName === undefined ? [] : [IO.UserName(options.userName)]),
        ...(options.password === undefined ? [] : [IO.Password(options.password)]),
        ...(options.mimeSupport === undefined ? [] : [A.MIMESupport(options.mimeSupport)]),
        ...(options.bodyPreference ?? []).map((preference) => buildBodyPreference(preference)),
        ...(options.bodyPartPreference ?? []).map(
            (preference) => buildBodyPreference(preference, AB.BodyPartPreference)),
        ...(options.rightsManagementSupport === undefined
            ? []
            : [RM.RightsManagementSupport(options.rightsManagementSupport)]),
    ];

    return children.length === 0 ? undefined : IO.Options(children);
}

function operation(entry: ItemOperation): EasNode {
    if (entry.kind === 'EmptyFolderContents')
        return IO.EmptyFolderContents(
            A.CollectionId(entry.collectionId),
            entry.deleteSubFolders === undefined
                ? undefined
                : IO.Options(IO.DeleteSubFolders(entry.deleteSubFolders)));

    if (entry.kind === 'Move')
        return IO.Move(
            IO.ConversationId(entry.conversationId),
            IO.DstFldId(entry.dstFldId),
            entry.moveAlways === undefined
                ? undefined
                : IO.Options(IO.MoveAlways(entry.moveAlways)));

    return IO.Fetch(
        IO.Store(entry.store),
        opt(entry.serverId, A.ServerId),
        opt(entry.collectionId, A.CollectionId),
        opt(entry.linkId, DL.LinkId),
        opt(entry.longId, SE.LongId),
        opt(entry.fileReference, AB.FileReference),
        entry.options === undefined ? undefined : fetchOptions(entry.options),
        opt(entry.removeRightsManagementProtection, RM.RemoveRightsManagementProtection));
}

export function buildItemOperations(request: ItemOperationsRequest): EasNode {
    return IO.ItemOperations(request.operations.map(operation));
}

// comes back as an opaque run or as base64 text, callers want bytes either way
function payload(node: EasNode | undefined): Uint8Array | undefined {
    if (node === undefined) return undefined;

    for (const child of node.children) if (isOpaque(child)) return child.opaque;

    let encoded = '';
    for (const child of node.children) if (isText(child)) encoded += child.text;
    if (encoded.length === 0) return undefined;

    const binary = atob(encoded.replace(/\s+/g, ''));
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}

function parseFetch(node: EasNode): FetchResponse {
    const properties = pick(node, IO.Properties);

    return {
        kind: 'Fetch',
        status: int(node, IO.Status),
        class: text(node, A.Class),
        serverId: text(node, A.ServerId),
        collectionId: text(node, A.CollectionId),
        linkId: text(node, DL.LinkId),
        longId: text(node, SE.LongId),
        fileReference: text(node, AB.FileReference),
        properties,
        data: payload(pick(properties, IO.Data) ?? pick(node, IO.Data)),
        part: int(properties, IO.Part) ?? int(node, IO.Part),
        total: int(properties, IO.Total) ?? int(node, IO.Total),
        range: text(properties, IO.Range) ?? text(node, IO.Range),
        version: text(properties, IO.Version) ?? text(node, IO.Version),
        node,
    };
}

function parseOperation(node: EasNode): ItemOperationResponse | undefined {
    if (node.name === 'Fetch') return parseFetch(node);

    if (node.name === 'EmptyFolderContents')
        return {
            kind: 'EmptyFolderContents',
            status: int(node, IO.Status),
            collectionId: text(node, A.CollectionId),
            node,
        };

    if (node.name === 'Move')
        return {
            kind: 'Move',
            status: int(node, IO.Status),
            conversationId: text(node, IO.ConversationId),
            node,
        };

    return undefined;
}

export function parseItemOperations(root: EasNode): ItemOperationsResponse {
    return {
        status: int(root, IO.Status),
        operations: elements(pick(root, IO.Response))
            .map(parseOperation)
            .filter((entry): entry is ItemOperationResponse => entry !== undefined),
        node: root,
    };
}
