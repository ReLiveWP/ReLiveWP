import { Signal, computed, effect, type ReadonlySignal } from "@preact/signals"
import { createContext } from "preact"
import { useContext } from "preact/hooks";
import { createRefresher, signOutUrl, type Refresher, type SecurityToken } from "@relivewp/auth";

import {
    ENDPOINT_GET_USER,
    ENDPOINT_REFRESH_TOKENS,
    SERVICE_TARGET_PORTAL,
    SSO_AUTHORITY,
    SSO_CLIENT_ID,
    SSO_POST_LOGOUT_REDIRECT_URI,
    SSO_REDIRECT_URI,
} from "../util/endpoints";
import { User } from "../util/auth-types";

export type TokenSet = Record<string, SecurityToken>;

type AppState = {
    tokens: Signal<TokenSet>,
    persistent: Signal<boolean>,
    user: Signal<User>,
    token: ReadonlySignal<string | null>,
    isAuthenticated: ReadonlySignal<boolean>,
    authenticatedFetch: ReadonlySignal<typeof fetch | undefined>,
    refresher: Refresher,
    signIn: (tokens: SecurityToken[], persistent: boolean) => void,
    signOut: () => void,
    refreshUser: () => Promise<void>
}

const STORAGE_KEY = "relivewp.portal.tokens";
const LEGACY_STORAGE_KEY = "token";

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

function readTokens(): TokenSet {
    let stored: string | null = null;
    try {
        localStorage.removeItem(LEGACY_STORAGE_KEY);
        sessionStorage.removeItem(LEGACY_STORAGE_KEY);

        stored = localStorage.getItem(STORAGE_KEY) ?? sessionStorage.getItem(STORAGE_KEY);
    } catch {
        return {};
    }

    if (stored === null) return {};

    try {
        return JSON.parse(stored) as TokenSet;
    } catch {
        return {};
    }
}

function clearStored(): void {
    try {
        localStorage.removeItem(STORAGE_KEY);
        sessionStorage.removeItem(STORAGE_KEY);
    } catch {
        // private windows and full quotas, not worth a dialog
    }
}

function write(value: TokenSet, persistent: boolean): void {
    try {
        if (Object.keys(value).length === 0) {
            clearStored();
            return;
        }

        const text = JSON.stringify(value);
        if (persistent) {
            localStorage.setItem(STORAGE_KEY, text);
            sessionStorage.removeItem(STORAGE_KEY);
        }
        else {
            sessionStorage.setItem(STORAGE_KEY, text);
            localStorage.removeItem(STORAGE_KEY);
        }
    } catch {
        // private windows and full quotas, not worth a dialog
    }
}

function live(tokens: TokenSet, target: string): string | null {
    const entry = tokens[target];
    if (entry === undefined) return null;

    return Date.parse(entry.expires) > Date.now() ? entry.token : null;
}

function forgiving(inner: typeof fetch): typeof fetch {
    return async (url, init) => {
        try {
            return await inner(url, init);
        }
        catch (e) {
            if ((e as Error).name === "AbortError") throw e;

            return new Response(null, { status: 503, statusText: "Service Unavailable" });
        }
    };
}

function createAppState() {
    const appState = createAppStateSignals();
    configureAppStateEffects(appState);

    return appState;
}

async function loadUser({ user, authenticatedFetch, tokens }: AppState) {
    const _fetch = authenticatedFetch.value;
    if (_fetch === undefined) return;

    try {
        const response = await _fetch(ENDPOINT_GET_USER, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });

        if (response.status === 401 || response.status === 403) {
            tokens.value = {};
            return;
        }

        if (!response.ok) return;

        user.value = await response.json() as User;
    }
    catch {
        // network trouble is not a reason to throw the session away
    }
}

function createAppStateSignals(): AppState {
    const tokens = new Signal<TokenSet>(readTokens());
    const persistent = new Signal(sessionStorage.getItem(STORAGE_KEY) === null);
    const user = new Signal();

    const refresher = createRefresher({
        endpoint: ENDPOINT_REFRESH_TOKENS,
        read: () => tokens.value,
        write: (set) => { tokens.value = set; },
        onSignedOut: () => { tokens.value = {}; },
        reload: readTokens,
    });

    const state: AppState = {
        tokens,
        persistent,
        user,
        refresher,
        token: computed(() => live(tokens.value, SERVICE_TARGET_PORTAL)),
        refreshUser: () => loadUser(state),
        isAuthenticated: computed(() => tokens.value[SERVICE_TARGET_PORTAL] !== undefined),
        authenticatedFetch: computed(() =>
            tokens.value[SERVICE_TARGET_PORTAL] === undefined
                ? undefined
                : forgiving(refresher.authenticatedFetch(SERVICE_TARGET_PORTAL))),
        signIn: (issued, remember) => {
            persistent.value = remember;
            tokens.value = Object.fromEntries(issued.map((t) => [t.service_target, t]));
        },
        signOut: () => {
            clearStored();
            
            window.location.assign(signOutUrl({
                authority: SSO_AUTHORITY,
                clientId: SSO_CLIENT_ID,
                redirectUri: SSO_REDIRECT_URI,
                serviceTargets: [],
            }, SSO_POST_LOGOUT_REDIRECT_URI));
        },
    };

    return state;
}

function configureAppStateEffects(state: AppState) {
    const { tokens, persistent } = state;

    effect(() => {
        write(tokens.value, persistent.value);

        if (Object.keys(tokens.value).length === 0) return;

        loadUser(state);
    });
}

export { useAppState, createAppState, useAuthenticatedFetch, AppStateProvider, AppState }
