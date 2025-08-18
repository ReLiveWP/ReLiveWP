import { Show } from "@preact/signals/utils";
import { useAppState } from "../../state/app-state"
import { ErrorBoundary, Route, Router } from "preact-iso";
import GoHome from "../../components/GoHome";
import Account from "./account";
import { useAccentColor } from "../../util/effects";


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