import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import { useTitle } from "@relivewp/ui";

import { startSignIn } from "~/util/sso";

export default function Login() {
    useTitle("login");

    const error = useSignal<string | null>(null);

    useEffect(() => {
        startSignIn().catch((e: Error) => { error.value = e.message; });
    }, []);

    return (
        <div>
            <h1>sign in</h1>
            {error.value
                ? <p class="error">{error.value}</p>
                : <p>Taking you to sign in...</p>}
        </div>
    );
}
