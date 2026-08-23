import type { SecurityToken, SecurityTokensResponse } from "./index.ts";

export type TokenSet = Record<string, SecurityToken>;

export type Refresher = {
    accessToken(target: string): Promise<string | null>;
    authenticatedFetch(target: string): typeof fetch;
};

export type RefresherOptions = {
    endpoint: string,
    read: () => TokenSet,
    write: (tokens: TokenSet) => void,
    onSignedOut: () => void,
    reload?: () => TokenSet,
};

const SKEW_MS = 60_000;
const LOCK_NAME = "relivewp.sso.refresh";

function isLive(token: SecurityToken | undefined): boolean {
    return token !== undefined && Date.parse(token.expires) - SKEW_MS > Date.now();
}

function sameRefreshTokens(a: TokenSet, b: TokenSet): boolean {
    const targets = new Set([...Object.keys(a), ...Object.keys(b)]);
    for (const target of targets)
        if (a[target]?.refresh_token !== b[target]?.refresh_token) return false;

    return true;
}

async function exclusive(work: () => Promise<TokenSet>): Promise<TokenSet> {
    const locks: LockManager | undefined = navigator.locks;
    if (locks === undefined) return await work();

    return await locks.request(LOCK_NAME, work);
}

export function createRefresher(options: RefresherOptions): Refresher {
    let inFlight: Promise<TokenSet> | null = null;

    async function refresh(): Promise<TokenSet> {
        const persisted = options.reload?.();
        if (persisted !== undefined && !sameRefreshTokens(persisted, options.read())) {
            options.write(persisted);
            return persisted;
        }

        const current = options.read();
        const refreshTokens = Object.values(current)
            .map((t) => t.refresh_token)
            .filter((t): t is string => typeof t === "string" && t.length > 0);

        if (refreshTokens.length === 0) {
            options.onSignedOut();
            return {};
        }

        const response = await fetch(options.endpoint, {
            method: "POST",
            headers: { "Accept": "application/json", "Content-Type": "application/json" },
            body: JSON.stringify({ refresh_tokens: refreshTokens }),
        });

        if (!response.ok) {
            options.onSignedOut();
            return {};
        }

        const payload = await response.json() as SecurityTokensResponse;
        const issued = (payload.security_tokens ?? []).filter((t) => t.token);
        if (issued.length === 0) {
            options.onSignedOut();
            return {};
        }

        const merged: TokenSet = { ...current };
        for (const token of issued) merged[token.service_target] = token;

        options.write(merged);
        return merged;
    }

    function refreshOnce(): Promise<TokenSet> {
        inFlight ??= exclusive(refresh).finally(() => { inFlight = null; });
        return inFlight;
    }

    async function accessToken(target: string): Promise<string | null> {
        const current = options.read();
        if (isLive(current[target])) return current[target]!.token;

        const refreshed = await refreshOnce();
        return isLive(refreshed[target]) ? refreshed[target]!.token : null;
    }

    function authenticatedFetch(target: string): typeof fetch {
        return async (url, init) => {
            const token = await accessToken(target);
            if (token === null) throw new Error(`No token available for ${target}`);

            const call = (bearer: string) => fetch(url, {
                ...init,
                headers: { ...init?.headers, "Authorization": `Bearer ${bearer}` },
            });

            const response = await call(token);
            if (response.status !== 401) return response;

            const retried = await refreshOnce();
            const fresh = retried[target]?.token;
            if (!fresh || fresh === token) return response;

            return await call(fresh);
        };
    }

    return { accessToken, authenticatedFetch };
}
