import { Signal, computed, effect } from "@preact/signals"

import { ENDPOINT_GET_USER } from "../util/endpoints";
import { User } from "../util/auth-types";
import { createContext } from "preact"
import { useContext } from "preact/hooks";

type AppState = {
    token: Signal<string | null>,
    persistent: Signal<boolean>,
    user: Signal<User>,
    isAuthenticated: Signal<boolean>,
    authenticatedFetch: Signal<typeof fetch | undefined>,
    refreshUser: () => Promise<void>
}

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

function createAppState() {
    const appState = createAppStateSignals();
    configureAppStateEffects(appState);

    return appState;
}

async function loadUser({ token, user, authenticatedFetch }: AppState) {
    const value = token.value;
    if (!value) return;

    try {
        const _fetch = authenticatedFetch.value;
        const response = await _fetch!(ENDPOINT_GET_USER, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
            }
        });

        if (!response.ok) {
            token.value = null;
            return;
        }

        user.value = await response.json() as User;
    }
    catch {
        token.value = null;
    }
}

function createAppStateSignals(): AppState {
    const tokenValue = localStorage.getItem("token") ?? sessionStorage.getItem("token");

    const token = new Signal<string | null>(tokenValue);
    const persistent = new Signal(sessionStorage.getItem("token") === null);
    const user = new Signal();

    const state: AppState = {
        token,
        persistent,
        user,
        refreshUser: () => loadUser(state),
        isAuthenticated: computed(() => !!token.value),
        authenticatedFetch: computed(() => {
            const value = token.value;
            if (!value)
                return undefined;

            return async (url, opts) => {
                const options = { ...opts };
                options.headers = {
                    ...options.headers,
                    'Authorization': `Bearer ${value}`
                };

                try {
                    return await fetch(url, options);
                }
                catch {
                    // CORS and network failures surface as thrown errors with no response
                    return new Response(null, { status: 503, statusText: 'Service Unavailable' });
                }
            }
        })
    };

    return state;
}

function configureAppStateEffects(state: AppState) {
    const { token, persistent } = state;

    effect(() => {
        const value = token.value;
        if (!value) {
            localStorage.removeItem("token");
            sessionStorage.removeItem("token");
        }
        else if (persistent.value) {
            localStorage.setItem("token", value);
            sessionStorage.removeItem("token");
        }
        else {
            sessionStorage.setItem("token", value);
            localStorage.removeItem("token");
        }

        if (!value) return;

        loadUser(state);
    });
}

export { useAppState, createAppState, useAuthenticatedFetch, AppStateProvider, AppState }