import { ErrorBoundary, Route, Router } from "preact-iso";

import Account from "./account";
import GoHome from "~/components/GoHome";
import { Show } from "@preact/signals/utils";
import { useAccentColor } from "@relivewp/ui";
import { useAppState } from "~/state/app-state"
import Device from "./device";

export default function MyRouter() {
    const appState = useAppState();

    return (
        <Show when={appState.user} fallback={<p>hang on while we sign you in...</p>}>
            <ErrorBoundary>
                <Router>
                    <Route path="/device/:id?" component={Device} />
                    <Route path="/account" component={Account} />
                    <Route path="/account/*" component={Account} />
                    <Route default component={GoHome} />
                </Router>
            </ErrorBoundary>
        </Show>
    )
}