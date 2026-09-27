import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { parseBase64QueryString } from '../src/transport/querystring.ts';
import { int, path, text } from '../src/nodes/read.ts';
import { EasSession, type EasSessionOptions } from '../src/session.ts';
import { AirSync, Ping, Provision as P, Settings as S } from '../src/generated/tags.g.ts';
import { EasTransport } from '../src/transport/transport.ts';
import { decode } from '../src/wbxml/decode.ts';
import { encode } from '../src/wbxml/encode.ts';

const ENDPOINT = 'https://sync.example.net/Microsoft-Server-ActiveSync';
const WBXML = { 'Content-Type': 'application/vnd.ms-sync.wbxml' };

interface Call {
    url: string;
    body: Uint8Array | undefined;
}

function stub(replies: (() => Response)[]) {
    const calls: Call[] = [];
    let at = 0;

    const fetchStub = (async (input: string | URL | Request, init?: RequestInit) => {
        const body = init?.body;
        calls.push({ url: String(input), body: body instanceof Uint8Array ? body : undefined });
        return replies[Math.min(at++, replies.length - 1)]!();
    }) as unknown as typeof fetch;

    return { fetchStub, calls };
}

function session(fetchStub: typeof fetch, policyKey?: number, options: EasSessionOptions = {}) {
    const transport = new EasTransport({
        endpoint: ENDPOINT,
        deviceId: 'B0045E20000000000000000000000001',
        deviceType: 'Browser',
        authorization: () => 'Basic dGVzdDp0ZXN0',
        fetch: fetchStub,
        sleep: async () => {},
    });

    return new EasSession(transport, {
        ...options,
        ...(policyKey === undefined ? {} : { policyKey }),
    });
}

const provisionReply = (key: number) => () =>
    new Response(
        encode(P.Provision(P.Status(1), P.Policies(P.Policy(P.PolicyType('MS-EAS-Provisioning-WBXML'),
            P.Status(1), P.PolicyKey(key))))),
        { status: 200, headers: WBXML });

const policyKeyOf = (call: Call) => parseBase64QueryString(new URL(call.url).search.slice(1)).policyKey;

describe('policy key handling', () => {
    it('sends no policy key until one is held', async () => {
        const { fetchStub, calls } = stub([() => new Response(encode(AirSync.Sync()), { status: 200, headers: WBXML })]);
        await session(fetchStub).send('Sync', AirSync.Sync());

        assert.equal(policyKeyOf(calls[0]!), undefined);
    });

    it('applies the held policy key to every command', async () => {
        const { fetchStub, calls } = stub([() => new Response(encode(AirSync.Sync()), { status: 200, headers: WBXML })]);
        const s = session(fetchStub, 4242);

        await s.send('Sync', AirSync.Sync());
        await s.send('FolderSync', undefined);

        assert.equal(policyKeyOf(calls[0]!), 4242);
        assert.equal(policyKeyOf(calls[1]!), 4242);
    });

    it('lets an explicit policy key win over the held one', async () => {
        const { fetchStub, calls } = stub([() => new Response(encode(AirSync.Sync()), { status: 200, headers: WBXML })]);
        await session(fetchStub, 4242).send('Sync', AirSync.Sync(), { policyKey: 7 });

        assert.equal(policyKeyOf(calls[0]!), 7);
    });
});

describe('Provision', () => {
    it('runs both phases and keeps the final key', async () => {
        const { fetchStub, calls } = stub([provisionReply(111), provisionReply(222)]);
        const s = session(fetchStub);
        const result = await s.provision();

        assert.equal(result.temporaryKey, 111);
        assert.equal(result.policyKey, 222);
        assert.equal(result.downloadStatus, 1);
        assert.equal(result.ackStatus, 1);
        assert.equal(s.policyKey, 222);

        assert.equal(policyKeyOf(calls[0]!), undefined);
        assert.equal(policyKeyOf(calls[1]!), 111);
    });

    it('acknowledges with the temporary key and a Status of 1', async () => {
        const { fetchStub, calls } = stub([provisionReply(111), provisionReply(222)]);
        await session(fetchStub).provision();

        const policy = path(decode(calls[1]!.body!).root, P.Policies, P.Policy);
        assert.equal(text(policy, P.PolicyType), 'MS-EAS-Provisioning-WBXML');
        assert.equal(int(policy, P.PolicyKey), 111);
        assert.equal(int(policy, P.Status), 1);
    });

    it('sends no key at all on the download phase', async () => {
        const { fetchStub, calls } = stub([provisionReply(111), provisionReply(222)]);
        await session(fetchStub).provision();

        const policy = path(decode(calls[0]!.body!).root, P.Policies, P.Policy);
        assert.equal(text(policy, P.PolicyType), 'MS-EAS-Provisioning-WBXML');
        assert.equal(int(policy, P.PolicyKey), undefined);
    });

    it('reports a download that carried no PolicyKey without sending an acknowledgement', async () => {
        const { fetchStub, calls } = stub([
            () => new Response(encode(P.Provision(P.Status(2))), { status: 200, headers: WBXML }),
        ]);
        const s = session(fetchStub);
        const result = await s.provision();

        assert.equal(result.temporaryKey, undefined);
        assert.equal(result.policyKey, undefined);
        assert.equal(result.downloadStatus, 2);
        assert.equal(result.ack, undefined);
        assert.equal(calls.length, 1);
        assert.equal(s.policyKey, undefined);
    });

    it('describes the device on the download phase and never on the acknowledgement', async () => {
        const { fetchStub, calls } = stub([provisionReply(111), provisionReply(222)]);
        await session(fetchStub, undefined, {
            deviceInformation: { model: 'ReLiveWP Web', friendlyName: 'Chrome', os: 'Web' },
        }).provision();

        const set = path(decode(calls[0]!.body!).root, S.DeviceInformation, S.Set);
        assert.equal(text(set, S.Model), 'ReLiveWP Web');
        assert.equal(text(set, S.FriendlyName), 'Chrome');
        assert.equal(text(set, S.IMEI), undefined);
        assert.equal(path(decode(calls[1]!.body!).root, S.DeviceInformation), undefined);
    });

    it('omits DeviceInformation entirely when the caller supplies none', async () => {
        const { fetchStub, calls } = stub([provisionReply(111), provisionReply(222)]);
        await session(fetchStub).provision();

        assert.equal(path(decode(calls[0]!.body!).root, S.DeviceInformation), undefined);
    });

    it('surfaces a remote wipe directive from the download phase', async () => {
        const { fetchStub } = stub([
            () => new Response(encode(P.Provision(P.Status(1), P.RemoteWipe())), { status: 200, headers: WBXML }),
        ]);
        const result = await session(fetchStub).provision();

        assert.equal(result.remoteWipe, true);
        assert.equal(result.accountOnlyRemoteWipe, false);
        assert.equal(result.temporaryKey, undefined);
    });

    it('acknowledges a wipe with a status and nothing else', async () => {
        const { fetchStub, calls } = stub([
            () => new Response(encode(P.Provision(P.Status(1))), { status: 200, headers: WBXML }),
        ]);
        await session(fetchStub).acknowledgeRemoteWipe(1);

        const wipe = path(decode(calls[0]!.body!).root, P.RemoteWipe);
        assert.equal(int(wipe, P.Status), 1);
    });

    it('hands back the policy document so the caller can read the settings that shape Sync', async () => {
        const { fetchStub } = stub([
            () => new Response(
                encode(P.Provision(P.Status(1), P.Policies(P.Policy(
                    P.PolicyType('MS-EAS-Provisioning-WBXML'),
                    P.Status(1),
                    P.PolicyKey(111),
                    P.Data(P.EASProvisionDoc(
                        P.AllowHTMLEmail(1),
                        P.MaxEmailBodyTruncationSize(8192))))))),
                { status: 200, headers: WBXML }),
            provisionReply(222),
        ]);
        const result = await session(fetchStub).provision();

        assert.equal(int(result.provisionDoc, P.AllowHTMLEmail), 1);
        assert.equal(int(result.provisionDoc, P.MaxEmailBodyTruncationSize), 8192);
    });

    it('leaves the held key alone when the acknowledgement carries no final key', async () => {
        const { fetchStub } = stub([
            provisionReply(111),
            () => new Response(encode(P.Provision(P.Status(1))), { status: 200, headers: WBXML }),
        ]);
        const s = session(fetchStub, 9);
        const result = await s.provision();

        assert.equal(result.temporaryKey, 111);
        assert.equal(result.policyKey, undefined);
        assert.equal(s.policyKey, 9);
    });
});

describe('Ping', () => {
    const changed = () => new Response(
        encode(Ping.Ping(Ping.Status(2), Ping.Folders(Ping.Folder('2')))),
        { status: 200, headers: WBXML });

    it('sends the heartbeat and folder list when given one', async () => {
        const { fetchStub, calls } = stub([changed]);
        const result = await session(fetchStub).ping({
            heartbeatInterval: 150, folders: [{ id: '2', class: 'Email' }],
        });

        assert.notEqual(calls[0]!.body, undefined);
        assert.equal(result.parsed?.status, 2);
        assert.deepEqual(result.parsed?.folderIds, ['2']);
    });

    it('sends no body at all when the cached heartbeat and folder list are wanted', async () => {
        const { fetchStub, calls } = stub([changed]);
        await session(fetchStub).ping();

        assert.equal(calls[0]!.body, undefined);
    });
});
