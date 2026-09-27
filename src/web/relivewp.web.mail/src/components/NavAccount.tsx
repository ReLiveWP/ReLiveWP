import { Link } from "@relivewp/ui";
import { Show } from "@preact/signals/utils";

import { useAppState } from "~/state/app-state";

const NavAccount = () => {
    const appState = useAppState();

    return (
        <Show when={appState.hasIdentity} fallback={<Link activeClass="active" href="/auth/login">sign in</Link>}>
            <Link activeClass="active text-accent" href="/settings">
                <Show when={appState.user} fallback={<span>hi there</span>}>
                    <span>hi, {appState.user.value?.username}</span>
                </Show>
            </Link>
        </Show>
    )
};

export default NavAccount;
