import Link from "./Link";
import { Show } from "@preact/signals/utils";
import { useAppState } from "~/state/app-state";

const NavLoginLink = () => {
    const appState = useAppState();

    return (
        <Show when={appState.isAuthenticated} fallback={<Link activeClass="active" href="/auth/login">sign in</Link>}>
            <Link activeClass="active text-accent" href="/my/account">
                <Show when={appState.user} fallback={<span>hi there</span>}>
                    <span>hi, {appState.user.value?.username}</span>
                </Show>
            </Link>
        </Show>
    )
};

export default NavLoginLink;