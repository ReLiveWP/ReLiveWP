import "./nav-account.scss";

import { Link } from "@relivewp/ui";
import { Show } from "@preact/signals/utils";

import { useAppState } from "~/state/app-state";

const NavAccount = () => {
    const appState = useAppState();

    return (
        <Show when={appState.hasIdentity} fallback={<Link activeClass="active" href="/auth/login">sign in</Link>}>
            <span class="account">
                <Show when={appState.user} fallback={<span>hi there</span>}>
                    <span class="text-accent">hi, {appState.user.value?.username}</span>
                </Show>
                <button type="button" class="sign-out" onClick={() => appState.signOut()}>sign out</button>
            </span>
        </Show>
    )
};

export default NavAccount;
