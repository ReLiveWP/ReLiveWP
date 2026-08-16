import { decode, type DecodeResult } from '../wbxml/decode.ts';
import { encode } from '../wbxml/encode.ts';
import type { EasNode } from '../nodes/node.ts';
import type { EasCommand } from './wire.ts';
import {
    buildBase64QueryString,
    buildPlainQueryString,
    type QueryParameters,
} from './querystring.ts';
import { EasRedirectError, EasThrottledError, errorForStatus } from './http-status.ts';

export const WBXML_CONTENT_TYPE = 'application/vnd.ms-sync.wbxml';

export type QueryFormat = 'base64' | 'plain';

export interface EasTransportOptions {
    endpoint: string;
    deviceId: string;
    deviceType: string;
    authorization: () => string | Promise<string>;
    // omit to auto-negotiate
    protocolVersion?: string;
    // goes out as the User parameter, just for the server's logs. auth is the Authorization header.
    user?: string;
    queryFormat?: QueryFormat;
    locale?: number;
    acceptLanguage?: string;
    userAgent?: string;
    followRedirects?: boolean;
    // 1 disables retrying
    maxAttempts?: number;
    fetch?: typeof fetch;
    onExchange?: (record: ExchangeRecord) => void;
    onEndpointChanged?: (endpoint: string) => void;
    onResyncRequired?: (protocolVersions: string) => void;
    onCredentialsExpiring?: (days: number) => void;
    sleep?: (ms: number) => Promise<void>;
}

export interface ExchangeRecord {
    command: EasCommand | 'OPTIONS';
    status: number;
    bytesSent: number;
    bytesReceived: number;
    encodeMs: number;
    decodeMs: number;
    totalMs: number;
    unknownTokens: readonly string[];
    attempt: number;
}

export interface EasResponse {
    status: number;
    headers: Headers;
    raw: Uint8Array;
    body: DecodeResult | undefined;
}

export interface OptionsResponse {
    status: number;
    headers: Headers;
    protocolVersions: readonly string[];
    protocolCommands: readonly string[];
}

export type PostOptions = Omit<QueryParameters, 'command' | 'deviceId' | 'deviceType' | 'user'> & {
    policyKey?: number;
    // for commands that send MIME instead of wbxml
    rawBody?: { bytes: Uint8Array; contentType: string };
    signal?: AbortSignal;
};

const defaultSleep = (ms: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, ms));

export class EasTransport {
    private readonly options: EasTransportOptions;
    private readonly http: typeof fetch;
    private readonly cookies = new Map<string, string>();
    private endpoint: string;

    constructor(options: EasTransportOptions) {
        this.options = options;
        // bound because window.fetch throws "Illegal invocation" when it's detached
        this.http = options.fetch ?? globalThis.fetch.bind(globalThis);
        this.endpoint = options.endpoint;
    }

    get currentEndpoint(): string {
        return this.endpoint;
    }

    async probe(signal?: AbortSignal): Promise<OptionsResponse> {
        const started = performance.now();
        const response = await this.send('OPTIONS', this.endpoint, undefined, undefined, signal);

        this.absorbResponseHeaders(response.headers);

        this.options.onExchange?.({
            command: 'OPTIONS',
            status: response.status,
            bytesSent: 0,
            bytesReceived: 0,
            encodeMs: 0,
            decodeMs: 0,
            totalMs: performance.now() - started,
            unknownTokens: [],
            attempt: 1,
        });

        return {
            status: response.status,
            headers: response.headers,
            protocolVersions: split(response.headers.get('MS-ASProtocolVersions')),
            protocolCommands: split(response.headers.get('MS-ASProtocolCommands')),
        };
    }

    async post(command: EasCommand, body: EasNode | undefined, post: PostOptions = {}): Promise<EasResponse> {
        const started = performance.now();
        const { policyKey, rawBody, signal, ...query } = post;

        const encodeStart = performance.now();
        const payload = rawBody?.bytes ?? (body === undefined ? new Uint8Array(0) : encode(body));
        const encodeMs = performance.now() - encodeStart;

        const maxAttempts = this.options.maxAttempts ?? 3;
        const sleep = this.options.sleep ?? defaultSleep;

        let attempt = 0;
        let redirected = false;

        for (;;) {
            attempt++;

            const url = `${this.endpoint}?${this.queryString(command, query, policyKey)}`;
            const response = await this.send(
                'POST', url, payload, this.postHeaders(command, policyKey, post, rawBody), signal);

            const raw = new Uint8Array(await response.arrayBuffer());

            let decoded: DecodeResult | undefined;
            const decodeStart = performance.now();
            if (raw.length > 0 && (response.headers.get('Content-Type') ?? '').startsWith('application/vnd.ms-sync'))
                decoded = decode(raw);
            const decodeMs = performance.now() - decodeStart;

            this.options.onExchange?.({
                command,
                status: response.status,
                bytesSent: payload.length,
                bytesReceived: raw.length,
                encodeMs,
                decodeMs,
                totalMs: performance.now() - started,
                unknownTokens: (decoded?.unknownTokens ?? []).map((t) => `${t.ns}:${t.name}`),
                attempt,
            });

            this.absorbResponseHeaders(response.headers);

            if (response.status >= 200 && response.status <= 299)
                return { status: response.status, headers: response.headers, raw, body: decoded };

            const error = errorForStatus(response.status, response.headers);

            if (error instanceof EasRedirectError
                && error.location !== undefined
                && (this.options.followRedirects ?? true)
                && !redirected) {
                redirected = true;
                this.endpoint = error.location;
                this.options.onEndpointChanged?.(error.location);
                continue;
            }

            if (error instanceof EasThrottledError && attempt < maxAttempts) {
                const seconds = error.retryAfterSeconds ?? Math.min(2 ** attempt, 60);
                await sleep(seconds * 1000);
                continue;
            }

            throw error;
        }
    }

    private queryString(command: EasCommand, query: Omit<PostOptions, 'policyKey' | 'rawBody' | 'signal'>, policyKey: number | undefined): string {
        const shared: QueryParameters = {
            command,
            deviceId: this.options.deviceId,
            deviceType: this.options.deviceType,
            ...(this.options.user !== undefined ? { user: this.options.user } : {}),
            ...query,
        };

        if ((this.options.queryFormat ?? 'base64') === 'plain')
            return buildPlainQueryString(shared);

        return buildBase64QueryString({
            ...shared,
            protocolVersion: this.protocolVersion,
            ...(policyKey !== undefined ? { policyKey } : {}),
            ...(this.options.locale !== undefined ? { locale: this.options.locale } : {}),
        });
    }

    private postHeaders(
        command: EasCommand,
        policyKey: number | undefined,
        post: PostOptions,
        rawBody: PostOptions['rawBody']): Record<string, string> {
        const headers: Record<string, string> = {
            'Content-Type': rawBody?.contentType ?? WBXML_CONTENT_TYPE,
        };

        if ((this.options.queryFormat ?? 'base64') === 'plain') {
            headers['MS-ASProtocolVersion'] = this.protocolVersion;
            if (policyKey !== undefined) headers['X-MS-PolicyKey'] = String(policyKey);
            if (post.acceptMultiPart && command === 'ItemOperations') headers['MS-ASAcceptMultiPart'] = 'T';
        }

        return headers;
    }

    private get protocolVersion(): string {
        return this.options.protocolVersion ?? '14.1';
    }

    private async send(
        method: string,
        url: string,
        body: Uint8Array | undefined,
        extraHeaders: Record<string, string> | undefined,
        signal: AbortSignal | undefined): Promise<Response> {
        const headers: Record<string, string> = {
            Authorization: await this.options.authorization(),
            ...extraHeaders,
        };

        if (this.options.acceptLanguage !== undefined) headers['Accept-Language'] = this.options.acceptLanguage;
        if (this.options.userAgent !== undefined) headers['User-Agent'] = this.options.userAgent;

        const cookie = [...this.cookies].map(([name, value]) => `${name}=${value}`).join('; ');
        if (cookie.length > 0) headers['Cookie'] = cookie;

        return this.http(url, {
            method,
            headers,
            ...(body !== undefined && body.length > 0 ? { body } : {}),
            // otherwise a 451 gets followed silently and the rebind in post() never runs
            redirect: 'manual',
            ...(signal ? { signal } : {}),
        });
    }

    private absorbResponseHeaders(headers: Headers): void {
        const resync = headers.get('X-MS-RP');
        if (resync !== null) this.options.onResyncRequired?.(resync);

        const expiring = headers.get('X-MS-Credentials-Expire');
        if (expiring !== null && Number.isInteger(Number(expiring)))
            this.options.onCredentialsExpiring?.(Number(expiring));

        const setCookie = typeof headers.getSetCookie === 'function' ? headers.getSetCookie() : [];
        for (const header of setCookie) {
            const pair = header.split(';', 1)[0] ?? '';
            const eq = pair.indexOf('=');
            if (eq > 0) this.cookies.set(pair.slice(0, eq).trim(), pair.slice(eq + 1).trim());
        }
    }
}

function split(value: string | null): string[] {
    return value === null ? [] : value.split(',').map((v) => v.trim()).filter(Boolean);
}
