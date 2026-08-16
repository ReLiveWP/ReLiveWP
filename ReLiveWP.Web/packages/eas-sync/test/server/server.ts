import { parseBase64QueryString, WBXML_CONTENT_TYPE, type EasCommand } from '@relivewp/eas-client';
import {
    decode,
    elements,
    encode,
    int,
    pick,
    tags,
    text,
    type EasNode,
} from '@relivewp/eas-client/nodes';

import { FakeMailbox, key, type ChangeEvent, type FakeFolder } from './mailbox.ts';

const {
    AirSync: A,
    AirSyncBase: AB,
    Email: E,
    FolderHierarchy: F,
    ItemOperations: IO,
    Ping: P,
    Provision: V,
} = tags;

export interface ServerQuirks {
    // answer Status 1 to any FolderSync key instead of 9
    folderSyncAcceptsAnyKey: boolean;
    // only work out MoreAvailable when the client sent WindowSize
    moreAvailableNeedsWindowSize: boolean;
    // keep reporting MoreAvailable while delivering nothing, like a stuck watermark
    neverAdvances: boolean;
    // answer an unchanged collection with headers and no body
    emptyResponseWhenUnchanged: boolean;
}

export const CONFORMANT: ServerQuirks = {
    folderSyncAcceptsAnyKey: false,
    moreAvailableNeedsWindowSize: false,
    neverAdvances: false,
    emptyResponseWhenUnchanged: true,
};

export const AS_DEPLOYED: ServerQuirks = {
    folderSyncAcceptsAnyKey: true,
    moreAvailableNeedsWindowSize: true,
    neverAdvances: false,
    emptyResponseWhenUnchanged: true,
};

interface Scripted {
    status?: number | undefined;
    httpStatus?: number | undefined;
    retryAfter?: string | undefined;
}

export interface PingOutcome {
    status: number;
    folderIds?: string[] | undefined;
    heartbeatInterval?: number | undefined;
    maxFolders?: number | undefined;
}

export interface PingRecord {
    heartbeatInterval: number | undefined;
    folderIds: string[];
}

function aborted(): Error {
    return Object.assign(new Error('aborted'), { name: 'AbortError' });
}

function pingResponse(outcome: PingOutcome): EasNode {
    return P.Ping(
        P.Status(outcome.status),
        ...(outcome.heartbeatInterval === undefined
            ? []
            : [P.HeartbeatInterval(outcome.heartbeatInterval)]),
        ...(outcome.maxFolders === undefined ? [] : [P.MaxFolders(outcome.maxFolders)]),
        ...(outcome.folderIds === undefined
            ? []
            : [P.Folders(...outcome.folderIds.map((id) => P.Folder(id)))]));
}

export interface RequestRecord {
    command: EasCommand | 'OPTIONS';
    collectionId: string | undefined;
    syncKey: string | undefined;
    windowSize: number | undefined;
    empty: boolean;
}

function payload(bytes: Uint8Array): ArrayBuffer {
    return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer;
}

// prefixed so a checkpoint can't collide with a log sequence number
const CHECKPOINT = /^c(\d+)$/;

export class FakeEasServer {
    readonly mailbox = new FakeMailbox();
    readonly requests: RequestRecord[] = [];
    // raw request bodies, so a test can assert on what actually went out
    readonly sent: { command: EasCommand; body: Uint8Array | undefined }[] = [];
    // what each Ping actually asked for, so a test can see a corrected heartbeat take effect
    readonly pings: PingRecord[] = [];
    readonly fetch: typeof fetch;

    quirks: ServerQuirks;
    policyKey = 1010101;

    private readonly scripted = new Map<string, Scripted[]>();
    private readonly cachedSync = new Map<string, EasNode>();
    private readonly pingQueue: PingOutcome[] = [];
    private pingHolds = false;
    private hierarchyKey = 0;
    private wipeRequested = false;
    private throttle = 0;
    private throttleAfter: string | undefined;

    constructor(quirks: ServerQuirks = AS_DEPLOYED) {
        this.quirks = quirks;
        this.fetch = ((input: string | URL | Request, init?: RequestInit) =>
            this.handle(String(input), init)) as unknown as typeof fetch;
    }

    throttleNext(count: number, retryAfterSeconds?: number): void {
        this.throttle = count;
        this.throttleAfter = retryAfterSeconds === undefined ? undefined : String(retryAfterSeconds);
    }

    // queued outcomes answer in order. once they run out a held server blocks until the client
    // gives up on the request, which is what a real Ping does for most of its life.
    pingReturns(...outcomes: PingOutcome[]): void {
        this.pingQueue.push(...outcomes);
        this.pingHolds = true;
    }

    holdPings(holds = true): void {
        this.pingHolds = holds;
    }

    scriptStatus(command: EasCommand, status: number): void {
        const queue = this.scripted.get(command) ?? [];
        queue.push({ status });
        this.scripted.set(command, queue);
    }

    requestRemoteWipe(): void {
        this.wipeRequested = true;
    }

    private next(command: EasCommand): Scripted | undefined {
        const queue = this.scripted.get(command);
        return queue === undefined || queue.length === 0 ? undefined : queue.shift();
    }

    private async handle(url: string, init: RequestInit | undefined): Promise<Response> {
        const query = parseBase64QueryString(new URL(url).search.slice(1));
        const command = (query.command ?? 'OPTIONS') as EasCommand;

        const raw = init?.body instanceof Uint8Array ? init.body : undefined;
        const body = raw === undefined || raw.length === 0 ? undefined : decode(raw).root;
        this.sent.push({ command, body: raw });

        if (this.throttle > 0) {
            this.throttle--;
            this.requests.push({
                command, collectionId: undefined, syncKey: undefined,
                windowSize: undefined, empty: body === undefined,
            });
            return new Response(null, {
                status: 503,
                headers: this.throttleAfter === undefined ? {} : { 'Retry-After': this.throttleAfter },
            });
        }

        const scripted = this.next(command);

        switch (command) {
            case 'Provision':
                return this.wbxml(this.provision(body));
            case 'FolderSync':
                return this.wbxml(this.folderSync(body, scripted));
            case 'Sync':
                return this.sync(body, scripted);
            case 'Ping':
                return this.wbxml(await this.ping(body, scripted, init?.signal ?? undefined));
            case 'ItemOperations':
                return this.wbxml(this.itemOperations(body, scripted));
            default:
                return new Response(null, { status: 200 });
        }
    }

    private wbxml(root: EasNode): Response {
        return new Response(payload(encode(root)), {
            status: 200,
            headers: { 'Content-Type': WBXML_CONTENT_TYPE },
        });
    }

    private provision(body: EasNode | undefined): EasNode {
        this.requests.push({
            command: 'Provision', collectionId: undefined, syncKey: undefined,
            windowSize: undefined, empty: body === undefined,
        });

        if (this.wipeRequested) {
            this.wipeRequested = false;
            return V.Provision(V.Status(1), V.RemoteWipe());
        }

        const acknowledging = int(pick(pick(body, V.Policies), V.Policy), V.PolicyKey) !== undefined;

        return V.Provision(
            V.Status(1),
            V.Policies(V.Policy(
                V.PolicyType('MS-EAS-Provisioning-WBXML'),
                V.Status(1),
                V.PolicyKey(acknowledging ? this.policyKey : this.policyKey - 1),
                acknowledging
                    ? undefined
                    : V.Data(V.EASProvisionDoc(
                        V.AllowHTMLEmail(1),
                        V.MaxEmailHTMLBodyTruncationSize(4096))))));
    }

    private folderSync(body: EasNode | undefined, scripted: Scripted | undefined): EasNode {
        const requested = text(body, F.SyncKey) ?? '0';
        this.requests.push({
            command: 'FolderSync', collectionId: undefined, syncKey: requested,
            windowSize: undefined, empty: body === undefined,
        });

        if (scripted?.status !== undefined) return F.FolderSync(F.Status(scripted.status));

        const known = requested === '0' || requested === String(this.hierarchyKey);
        if (!known && !this.quirks.folderSyncAcceptsAnyKey) return F.FolderSync(F.Status(9));

        this.hierarchyKey = this.mailbox.hierarchyVersion + 1;

        if (requested !== '0')
            return F.FolderSync(F.Status(1), F.SyncKey(String(this.hierarchyKey)), F.Changes(F.Count(0)));

        return F.FolderSync(
            F.Status(1),
            F.SyncKey(String(this.hierarchyKey)),
            F.Changes(
                F.Count(this.mailbox.folders.length),
                this.mailbox.folders.map((folder) => F.Add(
                    F.ServerId(folder.id),
                    F.ParentId(folder.parentId),
                    F.DisplayName(folder.name),
                    F.Type(folder.type)))));
    }

    private async ping(
        body: EasNode | undefined,
        scripted: Scripted | undefined,
        signal: AbortSignal | undefined): Promise<EasNode> {
        this.requests.push({
            command: 'Ping', collectionId: undefined, syncKey: undefined,
            windowSize: undefined, empty: body === undefined,
        });

        this.pings.push({
            heartbeatInterval: int(body, P.HeartbeatInterval),
            folderIds: elements(pick(body, P.Folders))
                .map((folder) => text(folder, P.Id))
                .filter((id): id is string => id !== undefined),
        });

        if (scripted?.status !== undefined) {
            if (scripted.status === 5) return P.Ping(P.Status(5), P.HeartbeatInterval(60));
            if (scripted.status === 6) return P.Ping(P.Status(6), P.MaxFolders(4));
            return P.Ping(P.Status(scripted.status));
        }

        const queued = this.pingQueue.shift();
        if (queued !== undefined) return pingResponse(queued);

        if (!this.pingHolds) return P.Ping(P.Status(1));
        return this.untilAborted(signal);
    }

    private untilAborted(signal: AbortSignal | undefined): Promise<never> {
        return new Promise((_resolve, reject) => {
            if (signal === undefined) return;
            if (signal.aborted) return reject(aborted());

            signal.addEventListener('abort', () => { reject(aborted()); }, { once: true });
        });
    }

    // a Fetch answers with the whole item, which is the entire point of the command: Sync sent a
    // clipped body because it was asked to, this one was not
    private itemOperations(body: EasNode | undefined, scripted: Scripted | undefined): EasNode {
        const fetch = pick(body, IO.Fetch);
        const collectionId = text(fetch, A.CollectionId);
        const serverId = text(fetch, A.ServerId);

        this.requests.push({
            command: 'ItemOperations', collectionId, syncKey: undefined,
            windowSize: undefined, empty: body === undefined,
        });

        if (scripted?.status !== undefined)
            return IO.ItemOperations(
                IO.Status(scripted.status),
                IO.Response(IO.Fetch(IO.Status(scripted.status))));

        const item = collectionId === undefined || serverId === undefined
            ? undefined
            : this.mailbox.items.get(key(collectionId, serverId));

        if (item === undefined || collectionId === undefined || serverId === undefined)
            return IO.ItemOperations(IO.Status(1), IO.Response(IO.Fetch(IO.Status(6))));

        return IO.ItemOperations(
            IO.Status(1),
            IO.Response(IO.Fetch(
                IO.Status(1),
                A.ServerId(serverId),
                A.CollectionId(collectionId),
                A.Class('Email'),
                IO.Properties(item.whole ?? item.data))));
    }

    private sync(body: EasNode | undefined, scripted: Scripted | undefined): Response {
        const request = body ?? this.cachedSync.get('last');
        if (request === undefined) return this.wbxml(A.Sync(A.Status(13)));
        if (body !== undefined) this.cachedSync.set('last', body);

        const collection = pick(pick(request, A.Collections), A.Collection);
        const collectionId = text(collection, A.CollectionId) ?? '';
        const syncKey = text(collection, A.SyncKey) ?? '0';
        const windowSize = int(collection, A.WindowSize);

        this.requests.push({
            command: 'Sync', collectionId, syncKey, windowSize, empty: body === undefined,
        });

        if (scripted?.status !== undefined)
            return this.wbxml(A.Sync(A.Collections(A.Collection(
                A.SyncKey(syncKey), A.CollectionId(collectionId), A.Status(scripted.status)))));

        if (syncKey === '0')
            return this.wbxml(A.Sync(A.Collections(A.Collection(
                A.SyncKey('c0'), A.CollectionId(collectionId), A.Status(1)))));

        const checkpoint = CHECKPOINT.exec(syncKey);
        if (checkpoint === null)
            return this.wbxml(A.Sync(A.Collections(A.Collection(
                A.SyncKey(syncKey), A.CollectionId(collectionId), A.Status(3)))));

        const from = Number(checkpoint[1]);

        if (this.quirks.neverAdvances)
            return this.wbxml(A.Sync(A.Collections(A.Collection(
                A.SyncKey(syncKey),
                A.CollectionId(collectionId),
                A.Status(1),
                A.MoreAvailable(),
                A.Commands()))));

        const pending = this.mailbox.since(collectionId, from);

        if (pending.length === 0) {
            if (this.quirks.emptyResponseWhenUnchanged)
                return new Response(null, { status: 200 });

            return this.wbxml(A.Sync(A.Collections(A.Collection(
                A.SyncKey(syncKey), A.CollectionId(collectionId), A.Status(1)))));
        }

        const window = windowSize ?? 100;
        const delivered = pending.slice(0, window);
        const more = pending.length > delivered.length
            && (windowSize !== undefined || !this.quirks.moreAvailableNeedsWindowSize);

        const last = delivered[delivered.length - 1]!;
        const nextKey = `c${last.sequence}`;

        return this.wbxml(A.Sync(A.Collections(A.Collection(
            A.SyncKey(nextKey),
            A.CollectionId(collectionId),
            A.Status(1),
            more ? A.MoreAvailable() : undefined,
            A.Commands(delivered.map((event) => this.command(event)))))));
    }

    private command(event: ChangeEvent): EasNode {
        if (event.kind === 'Delete') return A.Delete(A.ServerId(event.itemId));

        const item = this.mailbox.items.get(key(event.folderId, event.itemId));
        if (item === undefined) return A.Delete(A.ServerId(event.itemId));

        const data = A.ApplicationData(item.data);

        return event.kind === 'Add'
            ? A.Add(A.ServerId(item.id), data)
            : A.Change(A.ServerId(item.id), data);
    }
}

export function inboxFolder(): FakeFolder {
    return { id: 'inbox', parentId: '0', name: 'Inbox', type: 2 };
}

export function draftsFolder(): FakeFolder {
    return { id: 'drafts', parentId: '0', name: 'Drafts', type: 3 };
}

export function contactsFolder(): FakeFolder {
    return { id: 'contacts', parentId: '0', name: 'Contacts', type: 9 };
}

export function calendarFolder(): FakeFolder {
    return { id: 'calendar', parentId: '0', name: 'Calendar', type: 8 };
}

export function tasksFolder(): FakeFolder {
    return { id: 'tasks', parentId: '0', name: 'Tasks', type: 7 };
}

export function notesFolder(): FakeFolder {
    return { id: 'notes', parentId: '0', name: 'Notes', type: 10 };
}


