export const HTTP_REDIRECT = 451;
export const HTTP_BLOCKED = 456;
export const HTTP_PASSWORD_EXPIRED = 457;
export const HTTP_SERVICE_UNAVAILABLE = 503;
export const HTTP_TOO_MANY_REQUESTS = 429;

export class EasHttpError extends Error {
    readonly status: number;
    readonly headers: Headers;

    constructor(message: string, status: number, headers: Headers) {
        super(message);
        this.name = 'EasHttpError';
        this.status = status;
        this.headers = headers;
    }
}

export class EasRedirectError extends EasHttpError {
    readonly location: string | undefined;

    constructor(status: number, headers: Headers) {
        const location = headers.get('X-MS-Location') ?? undefined;
        super(location === undefined
            ? 'server redirected without an X-MS-Location; Autodiscover is required'
            : `server redirected to ${location}`, status, headers);
        this.name = 'EasRedirectError';
        this.location = location;
    }
}

export class EasThrottledError extends EasHttpError {
    readonly retryAfterSeconds: number | undefined;

    constructor(status: number, headers: Headers) {
        super(status === HTTP_TOO_MANY_REQUESTS ? 'rate limited' : 'service unavailable', status, headers);
        this.name = 'EasThrottledError';
        this.retryAfterSeconds = parseRetryAfter(headers.get('Retry-After'));
    }
}

export class EasAccountError extends EasHttpError {
    readonly credentialServiceUrl: string | undefined;

    constructor(status: number, headers: Headers) {
        super(status === HTTP_BLOCKED
            ? 'the account is blocked; the user must contact their administrator'
            : 'the password has expired and must be reset', status, headers);
        this.name = 'EasAccountError';
        this.credentialServiceUrl = headers.get('X-MS-Credential-Service-Url') ?? undefined;
    }
}

export function errorForStatus(status: number, headers: Headers): EasHttpError {
    if (status === HTTP_REDIRECT) return new EasRedirectError(status, headers);
    if (status === HTTP_SERVICE_UNAVAILABLE || status === HTTP_TOO_MANY_REQUESTS)
        return new EasThrottledError(status, headers);
    if (status === HTTP_BLOCKED || status === HTTP_PASSWORD_EXPIRED)
        return new EasAccountError(status, headers);

    return new EasHttpError(`HTTP ${status}`, status, headers);
}

export function parseRetryAfter(value: string | null, now = Date.now()): number | undefined {
    if (value === null) return undefined;

    const seconds = Number(value.trim());
    if (Number.isInteger(seconds) && seconds >= 0) return seconds;

    const at = Date.parse(value);
    return Number.isNaN(at) ? undefined : Math.max(0, Math.round((at - now) / 1000));
}
