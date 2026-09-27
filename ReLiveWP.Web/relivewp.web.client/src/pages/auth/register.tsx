import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import { useTitle } from "@relivewp/ui";

import { startSignUp } from "~/util/sso";

export default function Register() {
    useTitle("sign up");

    const error = useSignal<string | null>(null);

    useEffect(() => {
        startSignUp().catch((e: Error) => { error.value = e.message; });
    }, []);

    return (
        <div>
            <h1>sign up</h1>
            {error.value
                ? <p class="error">{error.value}</p>
                : <p>Taking you to sign up...</p>}
        </div>
    );
}
