import { Show } from "@preact/signals/utils";
import { useAppState } from "../../state/app-state"

export default function MyRouter() {
    const appState = useAppState();

    return (
        <Show when={appState.user} fallback={<p>hang on while we sign you in...</p>}>
            <p>should be authenticated! {appState.isAuthenticated} {appState.user.value?.username}</p>
        </Show>
    )
}