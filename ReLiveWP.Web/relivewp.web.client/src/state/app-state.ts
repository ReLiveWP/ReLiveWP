import { computed, effect, Signal } from "@preact/signals"
import { createContext } from "preact"
import { useContext } from "preact/hooks";
import { ENDPOINT_GET_USER } from "../util/endpoints";
import { type User } from "../util/auth-types";

export type AccentColor = 'red' | 'purple' | 'teal' | 'pink' | 'green' | 'yellow' | 'blue' | 'magenta' | 'zune';

type AppState = {
    token: Signal<string>,
    user: Signal<User>,
    isAuthenticated: Signal<boolean>,
    accentStack: Signal<AccentColor[]>
    accent: Signal<AccentColor>,
    accentColor: Signal<string>
}

const AppStateContext = createContext<AppState>(null);
const AppStateProvider = AppStateContext.Provider;

function useAppState() {
    return useContext(AppStateContext);
}

function createAppState() {
    const appState = createAppStateSignals();
    configureAppStateEffects(appState);

    return appState;
}

function createAppStateSignals(): AppState {
    const tokenValue = localStorage.getItem("token");

    const token = new Signal(tokenValue === null ? undefined : tokenValue);
    const user = new Signal();
    const accentStack = new Signal<AccentColor[]>(['red']);
    const accent = computed(() => accentStack.value[accentStack.value.length - 1]);

    return {
        token,
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
        })[accent.value])
    };
}

function configureAppStateEffects({ token, user }: AppState) {
    effect(() => {
        const value = token.value;
        if (!value) {
            localStorage.removeItem("token");
        }
        else {
            localStorage.setItem("token", value);
        }

        if (!value) return;

        (async () => {
            try {
                const response = await fetch(ENDPOINT_GET_USER, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json',
                        'Authorization': `Bearer ${value}`
                    }
                });

                if (!response.ok) {
                    token.value = null;
                }

                user.value = await response.json() as User;
            }
            catch {
                token.value = null;
            }
        })();
    });
}

export { useAppState, createAppState, AppStateProvider, AppState }