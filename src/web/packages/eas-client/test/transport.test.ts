import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { EasTransport } from '../src/transport/transport.ts';
import { EasAccountError, EasRedirectError, EasThrottledError, parseRetryAfter } from '../src/transport/http-status.ts';
import { parseBase64QueryString } from '../src/transport/querystring.ts';
import { encode } from '../src/wbxml/encode.ts';
import { el } from '../src/nodes/node.ts';

const ENDPOINT = 'https://sync.example.net/Microsoft-Server-ActiveSync';
const WBXML = { 'Content-Type': 'application/vnd.ms-sync.wbxml' };

interface Call {
    url: string;
    method: string;
    headers: Headers;
    body: Uint8Array | undefined;
}

function stub(replies: (() => Response)[]) {
    const calls: Call[] = [];
    let at = 0;

    const fetchStub = (async (input: string | URL | Request, init?: RequestInit) => {
        const body = init?.body;
        calls.push({
            url: String(input),
            method: init?.method ?? 'GET',
            headers: new Headers(init?.headers),
            body: body instanceof Uint8Array ? body : undefined,
        });

        const reply = replies[Math.min(at++, replies.length - 1)];
        return reply!();
    }) as unknown as typeof fetch;

    return { fetchStub, calls };
}

function transport(fetchStub: typeof fetch, overrides: Record<string, unknown> = {}) {
    return new EasTransport({
        endpoint: ENDPOINT,
        deviceId: 'B0045E20000000000000000000000001',
        deviceType: 'Browser',
        authorization: () => 'Basic dGVzdDp0ZXN0',
        sleep: async () => {},
        fetch: fetchStub,
        ...overrides,
    });
}

const ok = (body = new Uint8Array(0)) => () => new Response(body, { status: 200, headers: WBXML });

describe('request shape', () => {
    it('puts the version, policy key and command in the query value, not in headers', async () => {
        const { fetchStub, calls } = stub([ok()]);
        await transport(fetchStub).post('FolderSync', el('FolderHierarchy', 'FolderSync'), { policyKey: 42 });

        const call = calls[0]!;
        const parsed = parseBase64QueryString(new URL(call.url).search.slice(1));

        assert.equal(parsed.command, 'FolderSync');
        assert.equal(parsed.protocolVersion, '14.1');
        assert.equal(parsed.policyKey, 42);
        assert.equal(call.headers.get('MS-ASProtocolVersion'), null);
        assert.equal(call.headers.get('X-MS-PolicyKey'), null);
        assert.equal(call.headers.get('Content-Type'), 'application/vnd.ms-sync.wbxml');
    });

    it('sends them as headers when using the plain text query value', async () => {
        const { fetchStub, calls } = stub([ok()]);
        await transport(fetchStub, { queryFormat: 'plain' })
            .post('FolderSync', el('FolderHierarchy', 'FolderSync'), { policyKey: 42 });

        const call = calls[0]!;
        assert.match(call.url, /Cmd=FolderSync/);
        assert.equal(call.headers.get('MS-ASProtocolVersion'), '14.1');
        assert.equal(call.headers.get('X-MS-PolicyKey'), '42');
    });

    it('sends MS-ASAcceptMultiPart only for ItemOperations', async () => {
        const { fetchStub, calls } = stub([ok(), ok()]);
        const t = transport(fetchStub, { queryFormat: 'plain' });

        await t.post('ItemOperations', el('ItemOperations', 'ItemOperations'), { acceptMultiPart: true });
        await t.post('Sync', el('AirSync', 'Sync'), { acceptMultiPart: true });

        assert.equal(calls[0]!.headers.get('MS-ASAcceptMultiPart'), 'T');
        assert.equal(calls[1]!.headers.get('MS-ASAcceptMultiPart'), null);
    });

    it('never follows a redirect implicitly', async () => {
        const { fetchStub, calls } = stub([ok()]);
        await transport(fetchStub).probe();

        assert.equal(calls[0]!.method, 'OPTIONS');
        assert.equal(calls.length, 1);
    });

    it('sends a body only when there is one', async () => {
        const { fetchStub, calls } = stub([ok(), ok()]);
        const t = transport(fetchStub);

        await t.post('Sync', el('AirSync', 'Sync'));
        await t.post('Ping', undefined);

        assert.deepEqual(calls[0]!.body, encode(el('AirSync', 'Sync')));
        assert.equal(calls[1]!.body, undefined);
    });

    it('sends a raw body with its own content type', async () => {
        const { fetchStub, calls } = stub([ok()]);
        const mime = new TextEncoder().encode('From: a@b\r\n');

        await transport(fetchStub).post('SendMail', undefined, {
            rawBody: { bytes: mime, contentType: 'message/rfc822' },
            saveInSent: true,
        });

        assert.equal(calls[0]!.headers.get('Content-Type'), 'message/rfc822');
        assert.deepEqual(calls[0]!.body, mime);
        assert.equal(parseBase64QueryString(new URL(calls[0]!.url).search.slice(1)).saveInSent, true);
    });
});

describe('451 redirect', () => {
    it('rebinds to X-MS-Location and retries once', async () => {
        const moved = 'https://mail.example.net/Microsoft-Server-ActiveSync';
        const { fetchStub, calls } = stub([
            () => new Response(null, { status: 451, headers: { 'X-MS-Location': moved } }),
            ok(),
        ]);

        let announced: string | undefined;
        const t = transport(fetchStub, { onEndpointChanged: (e: string) => { announced = e; } });
        const response = await t.post('FolderSync', el('FolderHierarchy', 'FolderSync'));

        assert.equal(response.status, 200);
        assert.equal(calls.length, 2);
        assert.ok(calls[1]!.url.startsWith(moved));
        assert.equal(t.currentEndpoint, moved);
        assert.equal(announced, moved);
    });

    it('throws when the server gives no location, because that means Autodiscover', async () => {
        const { fetchStub } = stub([() => new Response(null, { status: 451 })]);

        await assert.rejects(
            transport(fetchStub).post('FolderSync', el('FolderHierarchy', 'FolderSync')),
            (e: unknown) => e instanceof EasRedirectError && e.location === undefined);
    });

    it('does not loop when the new endpoint redirects too', async () => {
        const { fetchStub, calls } = stub([
            () => new Response(null, { status: 451, headers: { 'X-MS-Location': 'https://a.example.net/eas' } }),
            () => new Response(null, { status: 451, headers: { 'X-MS-Location': 'https://b.example.net/eas' } }),
        ]);

        await assert.rejects(transport(fetchStub).post('FolderSync', el('FolderHierarchy', 'FolderSync')),
            EasRedirectError);
        assert.equal(calls.length, 2);
    });
});

describe('503 throttling', () => {
    it('waits the number of seconds in Retry-After', async () => {
        const slept: number[] = [];
        const { fetchStub, calls } = stub([
            () => new Response(null, { status: 503, headers: { 'Retry-After': '7' } }),
            ok(),
        ]);

        const t = transport(fetchStub, { sleep: async (ms: number) => { slept.push(ms); } });
        await t.post('Sync', el('AirSync', 'Sync'));

        assert.deepEqual(slept, [7000]);
        assert.equal(calls.length, 2);
    });

    it('backs off exponentially when the server offers no hint', async () => {
        const slept: number[] = [];
        const { fetchStub } = stub([() => new Response(null, { status: 503 })]);

        await assert.rejects(
            transport(fetchStub, { maxAttempts: 4, sleep: async (ms: number) => { slept.push(ms); } })
                .post('Sync', el('AirSync', 'Sync')),
            EasThrottledError);

        assert.deepEqual(slept, [2000, 4000, 8000]);
    });

    it('does not retry when maxAttempts is 1', async () => {
        const { fetchStub, calls } = stub([() => new Response(null, { status: 503, headers: { 'Retry-After': '1' } })]);

        await assert.rejects(
            transport(fetchStub, { maxAttempts: 1 }).post('Sync', el('AirSync', 'Sync')), EasThrottledError);
        assert.equal(calls.length, 1);
    });
});

describe('terminal account errors', () => {
    for (const [status, label] of [[456, 'blocked'], [457, 'expired password']] as const) {
        it(`gives up on ${status} (${label})`, async () => {
            const { fetchStub, calls } = stub([
                () => new Response(null, {
                    status,
                    headers: { 'X-MS-Credential-Service-Url': 'https://reset.example.net' },
                }),
            ]);

            await assert.rejects(
                transport(fetchStub).post('Sync', el('AirSync', 'Sync')),
                (e: unknown) => e instanceof EasAccountError
                    && e.credentialServiceUrl === 'https://reset.example.net');
            assert.equal(calls.length, 1);
        });
    }
});

describe('response headers that change client behaviour', () => {
    it('reports X-MS-RP as a demand to resynchronise', async () => {
        const { fetchStub } = stub([
            () => new Response(new Uint8Array(0), { status: 200, headers: { ...WBXML, 'X-MS-RP': '14.1,16.0' } }),
        ]);

        let versions: string | undefined;
        await transport(fetchStub, { onResyncRequired: (v: string) => { versions = v; } })
            .post('Sync', el('AirSync', 'Sync'));

        assert.equal(versions, '14.1,16.0');
    });

    it('reports an expiring password', async () => {
        const { fetchStub } = stub([
            () => new Response(new Uint8Array(0), {
                status: 200, headers: { ...WBXML, 'X-MS-Credentials-Expire': '13' },
            }),
        ]);

        let days: number | undefined;
        await transport(fetchStub, { onCredentialsExpiring: (d: number) => { days = d; } })
            .post('Sync', el('AirSync', 'Sync'));

        assert.equal(days, 13);
    });

    it('returns cookies the server set on later requests', async () => {
        const { fetchStub, calls } = stub([
            () => new Response(new Uint8Array(0), {
                status: 200,
                headers: [['Content-Type', 'application/vnd.ms-sync.wbxml'],
                    ['Set-Cookie', 'X-Backend=node7; path=/; HttpOnly']],
            }),
            ok(),
        ]);

        const t = transport(fetchStub);
        await t.post('FolderSync', el('FolderHierarchy', 'FolderSync'));
        await t.post('Sync', el('AirSync', 'Sync'));

        assert.equal(calls[0]!.headers.get('Cookie'), null);
        assert.equal(calls[1]!.headers.get('Cookie'), 'X-Backend=node7');
    });

    it('does not decode a body the server did not send as WBXML', async () => {
        const { fetchStub } = stub([
            () => new Response('<html>go away</html>', { status: 200, headers: { 'Content-Type': 'text/html' } }),
        ]);

        const response = await transport(fetchStub).post('Sync', el('AirSync', 'Sync'));
        assert.equal(response.body, undefined);
        assert.ok(response.raw.length > 0);
    });
});

describe('Retry-After parsing', () => {
    it('reads a delay in seconds', () => {
        assert.equal(parseRetryAfter('120'), 120);
    });

    it('reads an HTTP-date relative to now', () => {
        const now = Date.parse('Mon, 02 Mar 2009 23:51:51 GMT');
        assert.equal(parseRetryAfter('Mon, 02 Mar 2009 23:52:51 GMT', now), 60);
    });

    it('never returns a negative wait for a date in the past', () => {
        const now = Date.parse('Mon, 02 Mar 2009 23:51:51 GMT');
        assert.equal(parseRetryAfter('Mon, 02 Mar 2009 23:50:51 GMT', now), 0);
    });

    it('is undefined when absent or unparseable', () => {
        assert.equal(parseRetryAfter(null), undefined);
        assert.equal(parseRetryAfter('soon'), undefined);
    });
});
