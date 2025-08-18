import { useAppState } from "../../../state/app-state";

import "./sign-out.scss"

export default function SignOut() {
    const { token } = useAppState();
    const signOut = () => {
        token.value = null;
    }

    return (
        <div class="sign-out">
            <h5>To sign out, click below.</h5>
            <button onClick={signOut}>sign out</button>
        </div>
    );
}