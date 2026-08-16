import { buildFolderSync, parseFolderSync, type FolderSyncResponse } from './commands/foldersync.ts';
import {
    buildItemOperations,
    parseItemOperations,
    type ItemOperationsRequest,
    type ItemOperationsResponse,
} from './commands/itemoperations.ts';
import { buildPing, parsePing, type PingRequest, type PingResponse } from './commands/ping.ts';
import { buildSync, parseSync, type SyncRequest, type SyncResponse } from './commands/sync.ts';
import type { EasNode } from './nodes/node.ts';
import type { EasCommand } from './transport/wire.ts';
import { flag, int, path } from './nodes/read.ts';
import { opt } from './nodes/tags.ts';
import { Provision as P, Settings as S } from './generated/tags.g.ts';
import type { EasResponse, EasTransport, PostOptions } from './transport/transport.ts';

const PROVISIONING_WBXML = 'MS-EAS-Provisioning-WBXML';

export interface DeviceInformation {
    model: string;
    imei?: string | undefined;
    friendlyName?: string | undefined;
    os?: string | undefined;
    osLanguage?: string | undefined;
    phoneNumber?: string | undefined;
    mobileOperator?: string | undefined;
    userAgent?: string | undefined;
}

export interface EasSessionOptions {
    policyKey?: number | undefined;
    policyType?: string | undefined;
    deviceInformation?: DeviceInformation | undefined;
}

// parsed is undefined when there was no wbxml body, which isn't always an error. SendMail and an
// unchanged Sync both answer with nothing.
export interface CommandResult<T> {
    response: EasResponse;
    parsed: T | undefined;
}

export type FolderSyncResult = CommandResult<FolderSyncResponse>;
export type SyncResult = CommandResult<SyncResponse>;
export type PingResult = CommandResult<PingResponse>;
export type ItemOperationsResult = CommandResult<ItemOperationsResponse>;

export interface ProvisionResult {
    temporaryKey: number | undefined;
    policyKey: number | undefined;
    downloadStatus: number | undefined;
    ackStatus: number | undefined;
    remoteWipe: boolean;
    accountOnlyRemoteWipe: boolean;
    provisionDoc: EasNode | undefined;
    download: EasResponse;
    ack: EasResponse | undefined;
}

export class EasSession {
    readonly transport: EasTransport;
    private readonly policyType: string;
    private readonly device: DeviceInformation | undefined;
    private key: number | undefined;

    constructor(transport: EasTransport, options: EasSessionOptions = {}) {
        this.transport = transport;
        this.key = options.policyKey;
        this.policyType = options.policyType ?? PROVISIONING_WBXML;
        this.device = options.deviceInformation;
    }

    get policyKey(): number | undefined {
        return this.key;
    }

    send(command: EasCommand, body: EasNode | undefined, options: PostOptions = {}): Promise<EasResponse> {
        return this.transport.post(command, body, {
            ...(this.key === undefined ? {} : { policyKey: this.key }),
            ...options,
        });
    }

    folderSync(syncKey: string, options: PostOptions = {}): Promise<FolderSyncResult> {
        return this.run('FolderSync', buildFolderSync(syncKey), parseFolderSync, options);
    }

    sync(request: SyncRequest, options: PostOptions = {}): Promise<SyncResult> {
        return this.run('Sync', buildSync(request), parseSync, options);
    }

    // no request means an empty body, which asks the server to reuse the heartbeat and folders it
    // cached from the last full ping
    ping(request?: PingRequest, options: PostOptions = {}): Promise<PingResult> {
        return this.run(
            'Ping', request === undefined ? undefined : buildPing(request), parsePing, options);
    }

    itemOperations(
        request: ItemOperationsRequest, options: PostOptions = {}): Promise<ItemOperationsResult> {
        return this.run(
            'ItemOperations', buildItemOperations(request), parseItemOperations, options);
    }

    async provision(): Promise<ProvisionResult> {
        const policies = (...policy: (EasNode | undefined)[]) =>
            P.Policies(P.Policy(P.PolicyType(this.policyType), ...policy));

        const download = await this.send(
            'Provision', P.Provision(this.deviceInformation(), policies()));

        const wipe = flag(download.body?.root, P.RemoteWipe);
        const accountWipe = flag(download.body?.root, P.AccountOnlyRemoteWipe);
        const downloaded = policyOf(download);
        const temporary = downloaded === undefined ? undefined : int(downloaded, P.PolicyKey);

        if (temporary === undefined)
            return {
                temporaryKey: undefined,
                policyKey: this.key,
                downloadStatus: statusOf(download),
                ackStatus: undefined,
                remoteWipe: wipe,
                accountOnlyRemoteWipe: accountWipe,
                provisionDoc: provisionDocOf(download),
                download,
                ack: undefined,
            };

        const ack = await this.send(
            'Provision',
            P.Provision(policies(P.PolicyKey(temporary), P.Status(1))),
            { policyKey: temporary });

        const final = int(policyOf(ack), P.PolicyKey);
        if (final !== undefined) this.key = final;

        return {
            temporaryKey: temporary,
            policyKey: final,
            downloadStatus: statusOf(download),
            ackStatus: statusOf(ack),
            remoteWipe: wipe,
            accountOnlyRemoteWipe: accountWipe,
            provisionDoc: provisionDocOf(download),
            download,
            ack,
        };
    }

    acknowledgeRemoteWipe(status: number, accountOnly = false): Promise<EasResponse> {
        const tag = accountOnly ? P.AccountOnlyRemoteWipe : P.RemoteWipe;
        return this.send('Provision', P.Provision(tag(P.Status(status))));
    }

    private deviceInformation(): EasNode | undefined {
        const info = this.device;
        if (info === undefined) return undefined;

        return S.DeviceInformation(S.Set(
            S.Model(info.model),
            opt(info.imei, S.IMEI),
            opt(info.friendlyName, S.FriendlyName),
            opt(info.os, S.OS),
            opt(info.osLanguage, S.OSLanguage),
            opt(info.phoneNumber, S.PhoneNumber),
            opt(info.mobileOperator, S.MobileOperator),
            opt(info.userAgent, S.UserAgent)));
    }

    private async run<T>(
        command: EasCommand,
        body: EasNode | undefined,
        parse: (root: EasNode) => T,
        options: PostOptions): Promise<CommandResult<T>> {
        const response = await this.send(command, body, options);
        return { response, parsed: response.body && parse(response.body.root) };
    }
}

function policyOf(response: EasResponse): EasNode | undefined {
    return path(response.body?.root, P.Policies, P.Policy);
}

function provisionDocOf(response: EasResponse): EasNode | undefined {
    return path(policyOf(response), P.Data, P.EASProvisionDoc);
}

function statusOf(response: EasResponse): number | undefined {
    return int(policyOf(response), P.Status) ?? int(response.body?.root, P.Status);
}
