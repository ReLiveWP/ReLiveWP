import { computed, effect, Signal } from "@preact/signals"
import { createContext } from "preact"
import { useContext } from "preact/hooks";
import { ENDPOINT_GET_USER } from "../util/endpoints";
import { type User } from "../util/auth-types";

type AppState = {
    token: Signal<string>,
    user: Signal<User>,
    isAuthenticated: Signal<boolean>
}

const AppStateContext = createContext<AppState>(null);
const AppStateProvider = AppStateContext.Provider;

function useAppState() {
    return useContext(AppStateContext);
}

function createAppState(): AppState {
    const tokenValue = localStorage.getItem("token");

    const token = new Signal(tokenValue === null ? undefined : tokenValue);
    const user = new Signal();

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

    return {
        token,
        user,
        isAuthenticated: computed(() => !!token.value)
    };
}

export { useAppState, createAppState, AppStateProvider, AppState }