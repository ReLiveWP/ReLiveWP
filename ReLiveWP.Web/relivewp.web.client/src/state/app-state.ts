import { Signal, computed, effect } from "@preact/signals"

import { ENDPOINT_GET_USER } from "../util/endpoints";
import { User } from "../util/auth-types";
import { createContext } from "preact"
import { useContext } from "preact/hooks";

export type AccentColor = 'red' | 'purple' | 'teal' | 'pink' | 'green' | 'yellow' | 'blue' | 'magenta' | 'zune';

type AppState = {
    token: Signal<string | null>,
    persistent: Signal<boolean>,
    user: Signal<User>,
    isAuthenticated: Signal<boolean>,
    accentStack: Signal<AccentColor[]>
    accent: Signal<AccentColor>,
    accentColor: Signal<string>,
    authenticatedFetch: Signal<typeof fetch | undefined>
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

function createAppStateSignals(): AppState {
    const tokenValue = localStorage.getItem("token") ?? sessionStorage.getItem("token");

    const token = new Signal<string | null>(tokenValue);
    const persistent = new Signal(sessionStorage.getItem("token") === null);
    const user = new Signal();
    const accentStack = new Signal<AccentColor[]>(['red']);
    const accent = computed(() => accentStack.value[accentStack.value.length - 1]);

    return {
        token,
        persistent,
        user,
        isAuthenticated: computed(() => !!token.value),
        accent,
        accentStack,
        accentColor: computed(() => ({
            red: "#e60c00",
            teal: "#00ABA9",
            purple: "#A200FF",
            pink: "#E671B8",
            green: "#339933",
            yellow: "#F09609",
            blue: "#1BA1E2",
            magenta: "#D80073",
            zune: "#f10da1",
        })[accent.value]),
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
}

function configureAppStateEffects({ token, persistent, user, authenticatedFetch }: AppState) {
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

        (async () => {
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
        })();
    });
}

export { useAppState, createAppState, useAuthenticatedFetch, AppStateProvider, AppState }