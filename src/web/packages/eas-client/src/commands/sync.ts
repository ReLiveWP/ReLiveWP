import { elements, flag, int, pick, pickAll, text } from '../nodes/read.ts';
import {
    AirSync as A,
    AirSyncBase as AB,
    RightsManagement as RM,
    WindowsLive as WL,
} from '../generated/tags.g.ts';
import { opt, type Tag } from '../nodes/tags.ts';
import type { EasNode } from '../nodes/node.ts';
import type { CollectionClass } from './classes.ts';

export interface BodyPreference {
    type: number;
    truncationSize?: number | undefined;
    allOrNone?: boolean | undefined;
    preview?: number | undefined;
}

export interface CollectionOptions {
    // goes here, not on the collection
    class?: CollectionClass | undefined;
    filterType?: number | undefined;
    conflict?: number | undefined;
    mimeSupport?: number | undefined;
    mimeTruncation?: number | undefined;
    truncation?: number | undefined;
    maxItems?: number | undefined;
    bodyPreference?: BodyPreference[] | undefined;
    bodyPartPreference?: BodyPreference[] | undefined;
    rightsManagementSupport?: boolean | undefined;
    annotations?: string[] | undefined;
}

export type SyncCommand =
    | { kind: 'Add'; clientId: string; class?: CollectionClass | undefined; applicationData: EasNode }
    | {
        kind: 'Change'; serverId: string; class?: CollectionClass | undefined;
        instanceId?: string | undefined; applicationData: EasNode;
    }
    | { kind: 'Delete'; serverId: string; instanceId?: string | undefined }
    | { kind: 'SoftDelete'; serverId: string }
    | { kind: 'Fetch'; serverId: string };

export interface SyncCollectionRequest {
    collectionId: string;
    syncKey: string;
    windowSize?: number | undefined;
    getChanges?: boolean | undefined;
    deletesAsMoves?: boolean | undefined;
    conversationMode?: boolean | undefined;
    supported?: EasNode[] | undefined;
    options?: CollectionOptions[] | undefined;
    commands?: SyncCommand[] | undefined;
}

export interface SyncRequest {
    collections: SyncCollectionRequest[];
    wait?: number | undefined;
    heartbeatInterval?: number | undefined;
    windowSize?: number | undefined;
    partial?: boolean | undefined;
}

export type SyncChange =
    | { kind: 'Add' | 'Change'; serverId: string; class: string | undefined; applicationData: EasNode | undefined }
    | { kind: 'Delete' | 'SoftDelete'; serverId: string };

export interface SyncResponseEntry {
    kind: string;
    clientId: string | undefined;
    serverId: string | undefined;
    status: number | undefined;
    applicationData: EasNode | undefined;
}

export interface SyncCollectionResponse {
    collectionId: string | undefined;
    syncKey: string | undefined;
    class: string | undefined;
    status: number | undefined;
    moreAvailable: boolean;
    changes: SyncChange[];
    responses: SyncResponseEntry[];
    node: EasNode;
}

export interface SyncResponse {
    status: number | undefined;
    limit: number | undefined;
    collections: SyncCollectionResponse[];
    node: EasNode;
}

export function buildBodyPreference(preference: BodyPreference, container: Tag = AB.BodyPreference): EasNode {
    return container(
        AB.Type(preference.type),
        opt(preference.truncationSize, AB.TruncationSize),
        opt(preference.allOrNone, AB.AllOrNone),
        opt(preference.preview, AB.Preview));
}

function collectionOptions(options: CollectionOptions): EasNode {
    return A.Options(
        opt(options.class, A.Class),
        opt(options.filterType, A.FilterType),
        opt(options.conflict, A.Conflict),
        opt(options.mimeSupport, A.MIMESupport),
        opt(options.mimeTruncation, A.MIMETruncation),
        opt(options.truncation, A.Truncation),
        (options.bodyPreference ?? []).map((preference) => buildBodyPreference(preference)),
        (options.bodyPartPreference ?? []).map(
            (preference) => buildBodyPreference(preference, AB.BodyPartPreference)),
        opt(options.rightsManagementSupport, RM.RightsManagementSupport),
        opt(options.maxItems, A.MaxItems),
        annotations(options.annotations));
}

// a Name with no Value is the subscription form: it asks for the name rather than setting it
function annotations(names: string[] | undefined): EasNode | undefined {
    if (names === undefined || names.length === 0) return undefined;
    return WL.Annotations(names.map((name) => WL.Annotation(WL.Name(name))));
}

function command(entry: SyncCommand): EasNode {
    switch (entry.kind) {
        case 'Add':
            return A.Add(
                opt(entry.class, A.Class),
                A.ClientId(entry.clientId),
                A.ApplicationData(entry.applicationData.children));
        case 'Change':
            return A.Change(
                opt(entry.class, A.Class),
                A.ServerId(entry.serverId),
                opt(entry.instanceId, AB.InstanceId),
                A.ApplicationData(entry.applicationData.children));
        case 'Delete':
            return A.Delete(A.ServerId(entry.serverId), opt(entry.instanceId, AB.InstanceId));
        case 'SoftDelete':
            return A.SoftDelete(A.ServerId(entry.serverId));
        case 'Fetch':
            return A.Fetch(A.ServerId(entry.serverId));
    }
}

// source order is wire order, and the server is strict about it
function collection(request: SyncCollectionRequest): EasNode {
    return A.Collection(
        A.SyncKey(request.syncKey),
        A.CollectionId(request.collectionId),
        request.supported === undefined ? undefined : A.Supported(request.supported),
        opt(request.deletesAsMoves, A.DeletesAsMoves),
        opt(request.getChanges, A.GetChanges),
        opt(request.windowSize, A.WindowSize),
        opt(request.conversationMode, A.ConversationMode),
        (request.options ?? []).map(collectionOptions),
        request.commands === undefined ? undefined : A.Commands(request.commands.map(command)));
}

export function buildSync(request: SyncRequest): EasNode {
    return A.Sync(
        A.Collections(request.collections.map(collection)),
        request.partial === true ? A.Partial() : undefined,
        opt(request.wait, A.Wait),
        opt(request.heartbeatInterval, A.HeartbeatInterval),
        opt(request.windowSize, A.WindowSize));
}

function parseChange(node: EasNode): SyncChange | undefined {
    const serverId = text(node, A.ServerId);
    if (serverId === undefined) return undefined;

    if (node.name === 'Delete' || node.name === 'SoftDelete') return { kind: node.name, serverId };
    if (node.name !== 'Add' && node.name !== 'Change') return undefined;

    return {
        kind: node.name,
        serverId,
        class: text(node, A.Class),
        applicationData: pick(node, A.ApplicationData),
    };
}

function parseCollection(node: EasNode): SyncCollectionResponse {
    return {
        collectionId: text(node, A.CollectionId),
        syncKey: text(node, A.SyncKey),
        class: text(node, A.Class),
        status: int(node, A.Status),
        moreAvailable: flag(node, A.MoreAvailable),
        changes: elements(pick(node, A.Commands))
            .map(parseChange)
            .filter((c): c is SyncChange => c !== undefined),
        responses: elements(pick(node, A.Responses)).map((entry) => ({
            kind: entry.name,
            clientId: text(entry, A.ClientId),
            serverId: text(entry, A.ServerId),
            status: int(entry, A.Status),
            applicationData: pick(entry, A.ApplicationData),
        })),
        node,
    };
}

export function parseSync(root: EasNode): SyncResponse {
    return {
        status: int(root, A.Status),
        limit: int(root, A.Limit),
        collections: pickAll(pick(root, A.Collections), A.Collection).map(parseCollection),
        node: root,
    };
}
