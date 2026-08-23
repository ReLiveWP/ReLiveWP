import { challengeFor, createState, createVerifier } from "./pkce.ts";

export type SecurityToken = {
    service_target: string,
    token: string,
    token_type: string,
    created: string,
    expires: string,
    refresh_token?: string | null,
    refresh_token_expires?: string | null,
};

export type SecurityTokensResponse = {
    puid: number,
    cid: string,
    username: string,
    email_address: string,
    security_tokens: SecurityToken[],
};

export type SsoConfig = {
    authority: string,
    clientId: string,
    redirectUri: string,
    serviceTargets: string[],
};

export type SsoCallbackResult =
    | { kind: "none" }
    | { kind: "error", error: string, description?: string | undefined }
    | { kind: "signed-in", tokens: SecurityToken[], response: SecurityTokensResponse, persistent: boolean };

const STATE_KEY = "relivewp.sso.state";
const VERIFIER_KEY = "relivewp.sso.verifier";

function authorizeUrl(config: SsoConfig, state: string, challenge: string, method: string, prompt?: "none"): string {
    const params = new URLSearchParams({
        client_id: config.clientId,
        redirect_uri: config.redirectUri,
        state,
        scope: config.serviceTargets.join(" "),
        code_challenge: challenge,
        code_challenge_method: method,
    });

    if (prompt) params.set("prompt", prompt);

    return `${config.authority}/sso/authorize?${params}`;
}

export async function beginSignIn(config: SsoConfig, prompt?: "none"): Promise<void> {
    const state = createState();
    const verifier = createVerifier();
    const { challenge, method } = await challengeFor(verifier);

    sessionStorage.setItem(STATE_KEY, state);
    sessionStorage.setItem(VERIFIER_KEY, verifier);

    window.location.assign(authorizeUrl(config, state, challenge, method, prompt));
}

export function signOutUrl(config: SsoConfig, postLogoutRedirectUri: string): string {
    const params = new URLSearchParams({
        client_id: config.clientId,
        post_logout_redirect_uri: postLogoutRedirectUri,
    });

    return `${config.authority}/sso/signout?${params}`;
}

export async function completeSignIn(config: SsoConfig): Promise<SsoCallbackResult> {
    const fragment = window.location.hash.replace(/^#/, "");
    if (!fragment) return { kind: "none" };

    const params = new URLSearchParams(fragment);
    const code = params.get("code");
    const error = params.get("error");
    const state = params.get("state");
    const persistent = params.get("persistent") === "1";
    if (!code && !error) return { kind: "none" };

    const expectedState = sessionStorage.getItem(STATE_KEY);
    const verifier = sessionStorage.getItem(VERIFIER_KEY);
    sessionStorage.removeItem(STATE_KEY);
    sessionStorage.removeItem(VERIFIER_KEY);

    history.replaceState(null, "", window.location.pathname + window.location.search);

    if (!expectedState || state !== expectedState)
        return { kind: "error", error: "invalid_state" };

    if (error)
        return { kind: "error", error, description: params.get("error_description") ?? undefined };

    const body: Record<string, string> = {
        code: code!,
        client_id: config.clientId,
        redirect_uri: config.redirectUri,
    };

    if (verifier) body["code_verifier"] = verifier;

    const response = await fetch(`${config.authority}/sso/token`, {
        method: "POST",
        headers: { "Accept": "application/json", "Content-Type": "application/json" },
        body: JSON.stringify(body),
    });

    if (!response.ok) {
        const detail = await response.json().catch(() => ({}));
        return { kind: "error", error: (detail as { error?: string }).error ?? "invalid_grant" };
    }

    const payload = await response.json() as SecurityTokensResponse;
    return { kind: "signed-in", tokens: payload.security_tokens, response: payload, persistent };
}

export { createRefresher } from "./refresh.ts";
export type { Refresher, TokenSet } from "./refresh.ts";
