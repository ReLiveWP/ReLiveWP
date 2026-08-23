import { completeSignIn } from "@relivewp/auth";
import { useLocation } from "preact-iso";
import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import { useTitle } from "@relivewp/ui";

import { useAppState } from "~/state/app-state";
import { ssoConfig } from "~/util/sso";
import { DEFAULT_FOLDER, mailPath } from "~/util/routes";

export default function Callback() {
    useTitle("signing in");

    const error = useSignal<string | null>(null);
    const appState = useAppState();
    const location = useLocation();

    useEffect(() => {
        (async () => {
            const result = await completeSignIn(ssoConfig());
            if (result.kind === "signed-in") {
                appState.signIn(result.tokens, result.persistent);
                location.route(mailPath(DEFAULT_FOLDER), true);
                return;
            }

            error.value = result.kind === "error"
                ? result.description ?? result.error
                : "That sign-in did not complete.";
        })();
    }, []);

    return (
        <div>
            <h1>signing in</h1>
            {error.value
                ? <p class="error">{error.value} <a href="/auth/login">try again</a></p>
                : <p>One moment...</p>}
        </div>
    );
}
