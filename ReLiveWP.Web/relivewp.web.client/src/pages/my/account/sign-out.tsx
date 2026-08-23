import "./sign-out.scss"

import { useAppState } from "~/state/app-state";

export default function SignOut() {
    const { signOut } = useAppState();

    return (
        <div class="sign-out">
            <h5>To sign out, click below.</h5>
            <button onClick={signOut}>sign out</button>
        </div>
    );
}