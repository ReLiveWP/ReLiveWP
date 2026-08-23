import { Signal, computed, effect, type ReadonlySignal } from "@preact/signals";
import { createContext } from "preact";
import { useContext } from "preact/hooks";
import { createRefresher, signOutUrl, type Refresher } from "@relivewp/auth";

import {
    ENDPOINT_GET_USER,
    ENDPOINT_REFRESH_TOKENS,
    SERVICE_TARGET_EAS,
    SERVICE_TARGET_PORTAL,
    SSO_AUTHORITY,
    SSO_CLIENT_ID,
    SSO_POST_LOGOUT_REDIRECT_URI,
    SSO_REDIRECT_URI,
} from "~/util/endpoints";
import { bootstrapUser } from "~/util/bootstrap";
import type { SecurityToken, User } from "~/util/auth-types";

export type TokenSet = Record<string, SecurityToken>;

export type AppState = {
    tokens: Signal<TokenSet>,
    persistent: Signal<boolean>,
    user: Signal<User | undefined>,
    online: Signal<boolean>,
    token: ReadonlySignal<string | null>,
    easToken: ReadonlySignal<string | null>,
    storedEasToken: ReadonlySignal<string | null>,
    isAuthenticated: ReadonlySignal<boolean>,
    hasIdentity: ReadonlySignal<boolean>,
    authenticatedFetch: ReadonlySignal<typeof fetch | undefined>,
    refresher: Refresher,
    signIn: (tokens: SecurityToken[], persistent: boolean) => void,
    signOut: () => void,
}

const STORAGE_KEY = "relivewp.mail.tokens";
const USER_KEY = "relivewp.mail.user";

const AppStateContext = createContext<AppState>(null!);
const AppStateProvider = AppStateContext.Provider;

function useAppState() {
    return useContext(AppStateContext);
}

function useAuthenticatedFetch(): typeof fetch {
    const appState = useAppState();
    const func = appState.authenticatedFetch.value;
    if (func === undefined)
        throw new Error("Attempted to use authenticated fetch while unauthenticated!");

    return func;
}

function read<T>(key: string): T | undefined {
    let stored: string | null = null;
    try {
        stored = localStorage.getItem(key) ?? sessionStorage.getItem(key);
    } catch {
        return undefined;
    }

    if (stored === null) return undefined;

    try {
        return JSON.parse(stored) as T;
    } catch {
        return undefined;
    }
}

function write(key: string, value: unknown, persistent: boolean): void {
    try {
        if (value === undefined) {
            localStorage.removeItem(key);
            sessionStorage.removeItem(key);
            return;
        }

        const text = JSON.stringify(value);
        if (persistent) {
            localStorage.setItem(key, text);
            sessionStorage.removeItem(key);
        }
        else {
            sessionStorage.setItem(key, text);
            localStorage.removeItem(key);
        }
    } catch {
        // private windows and full quotas, not worth a dialog
    }
}

function readTokens(): TokenSet {
    return read<TokenSet>(STORAGE_KEY) ?? {};
}

function live(tokens: TokenSet, target: string): string | null {
    const entry = tokens[target];
    if (entry === undefined) return null;

    return Date.parse(entry.expires) > Date.now() ? entry.token : null;
}

function stored(tokens: TokenSet, target: string): string | null {
    return tokens[target]?.token ?? null;
}

function createAppState(): AppState {
    const appState = createAppStateSignals();
    configureAppStateEffects(appState);

    return appState;
}

function createAppStateSignals(): AppState {
    const tokens = new Signal<TokenSet>(readTokens());
    const persistent = new Signal(sessionStorage.getItem(STORAGE_KEY) === null);
    const user = new Signal<User | undefined>(read<User>(USER_KEY));
    const online = new Signal(navigator.onLine);

    const token = computed(() => live(tokens.value, SERVICE_TARGET_PORTAL));
    const easToken = computed(() => live(tokens.value, SERVICE_TARGET_EAS));

    const refresher = createRefresher({
        endpoint: ENDPOINT_REFRESH_TOKENS,
        read: () => tokens.value,
        write: (set) => { tokens.value = set; },
        onSignedOut: () => { tokens.value = {}; user.value = undefined; },
        reload: readTokens,
    });

    return {
        refresher,
        tokens,
        persistent,
        user,
        online,
        token,
        easToken,
        storedEasToken: computed(() => stored(tokens.value, SERVICE_TARGET_EAS)),
        isAuthenticated: computed(() => token.value !== null),
        hasIdentity: computed(() => tokens.value[SERVICE_TARGET_PORTAL] !== undefined),
        authenticatedFetch: computed(() =>
            tokens.value[SERVICE_TARGET_PORTAL] === undefined
                ? undefined
                : refresher.authenticatedFetch(SERVICE_TARGET_PORTAL)),
        signIn: (issued, remember) => {
            persistent.value = remember;
            tokens.value = Object.fromEntries(issued.map((t) => [t.service_target, t]));
        },
        signOut: () => {
            write(STORAGE_KEY, undefined, persistent.peek());
            write(USER_KEY, undefined, persistent.peek());

            window.location.assign(signOutUrl({
                authority: SSO_AUTHORITY,
                clientId: SSO_CLIENT_ID,
                redirectUri: SSO_REDIRECT_URI,
                serviceTargets: [],
            }, SSO_POST_LOGOUT_REDIRECT_URI));
        },
    };
}

function configureAppStateEffects(
    { tokens, token, persistent, user, online, hasIdentity, authenticatedFetch }: AppState) {
    effect(() => {
        const set = tokens.value;
        const empty = Object.keys(set).length === 0;

        write(STORAGE_KEY, empty ? undefined : set, persistent.value);
    });

    effect(() => {
        write(USER_KEY, user.value, persistent.value);
    });

    const sync = () => { online.value = navigator.onLine; };
    window.addEventListener("online", sync);
    window.addEventListener("offline", sync);

    effect(() => {
        if (!hasIdentity.value) {
            user.value = undefined;
            return;
        }

        // an expired token now goes through the refresher rather than stalling. offline, the
        // refresh fetch throws, bootstrapUser reports unreachable, and the cached identity
        // survives so the session still reads locally.
        (async () => {
            const _fetch = authenticatedFetch.value;
            if (_fetch === undefined) return;

            const outcome = await bootstrapUser(_fetch, ENDPOINT_GET_USER);

            if (outcome.kind === "rejected") tokens.value = {};
            if (outcome.kind === "ok") user.value = outcome.user;
        })();
    });
}

export { useAppState, createAppState, useAuthenticatedFetch, AppStateProvider }
