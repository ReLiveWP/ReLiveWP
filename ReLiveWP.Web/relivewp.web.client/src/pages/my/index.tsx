import { ErrorBoundary, Route, Router } from "preact-iso";

import Account from "./account";
import GoHome from "~/components/GoHome";
import { Show } from "@preact/signals/utils";
import { useAccentColor } from "~/util/effects";
import { useAppState } from "~/state/app-state"

export default function MyRouter() {
    const appState = useAppState();

    useAccentColor('teal');

    return (
        <Show when={appState.user} fallback={<p>hang on while we sign you in...</p>}>
            <ErrorBoundary>
                <Router>
                    <Route path="/account" component={Account} />
                    <Route path="/account/*" component={Account} />
                    <Route default component={GoHome} />
                </Router>
            </ErrorBoundary>
        </Show>
    )
}