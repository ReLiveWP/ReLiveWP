import "./login.scss"

import { ENDPOINT_REQUEST_TOKENS } from "~/util/endpoints";
import { useAppState } from "~/state/app-state";
import { useLocation } from "preact-iso";
import { useSignal } from "@preact/signals";
import { useTitle } from "~/util/effects";
import { toString } from "~/util/hresult";

export default function Login() {
    useTitle("login");

    const username = useSignal("");
    const password = useSignal("");
    const rememberMe = useSignal(true);
    const isDisabled = useSignal(false);
    const error = useSignal<string | null>(null)

    const appState = useAppState();
    const location = useLocation();

    const signIn = async (e: SubmitEvent) => {
        e.preventDefault();

        isDisabled.value = true;
        try {
            const payload = {
                identity: username.value,
                credentials: {
                    "ps:password": password.value
                },
                token_requests: [{
                    service_policy: "MBI",
                    service_target: "relivewp.net"
                }]
            };

            const payloadText = JSON.stringify(payload);
            const response = await fetch(ENDPOINT_REQUEST_TOKENS, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: payloadText
            });

            if (!response.ok) {
                const { error_code }: { error_code: number } = await response.json();
                error.value = toString(error_code);
                return; // todo: show errors
            }

            const { security_tokens } = await response.json();
            appState.persistent.value = rememberMe.value;
            appState.token.value = security_tokens[0].token;

            location.route("/");
        } catch (e: any) {
            // todo: show errors
            error.value = e.message;
        } finally {
            isDisabled.value = false;
        }
    };

    return (
        <div>
            <h1>sign in</h1>
            <form class="auth-form" onSubmit={signIn}>
                <label htmlFor="username">ReLive ID:</label>
                <input id="username"
                    type="text"
                    autoComplete="username,email"
                    class="textbox"
                    placeholder="example555@example.com"
                    value={username}
                    onChange={(e) => username.value = e.currentTarget.value}
                    disabled={isDisabled} />

                <label htmlFor="password">Password:</label>
                <input id="password"
                    type="password"
                    class="textbox"
                    value={password}
                    onChange={(e) => password.value = e.currentTarget.value}
                    disabled={isDisabled} />
                <a href="/auth/forgor" class="forgot-link">Forgot your password?</a>

                <label class="remember-me" htmlFor="remember-me">
                    <input id="remember-me"
                        type="checkbox"
                        class="checkbox"
                        checked={rememberMe}
                        onChange={(e) => rememberMe.value = e.currentTarget.checked}
                        disabled={isDisabled}></input>
                    <span class="remember-me-text">Remember me</span>
                </label>

                {error.value && <p class="error">{error.value}</p>}

                <input type="submit" class="submit" value="Sign in" disabled={isDisabled} />
            </form>
        </div>
    )
}