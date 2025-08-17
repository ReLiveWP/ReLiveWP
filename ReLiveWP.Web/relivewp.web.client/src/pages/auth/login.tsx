
import { useTitle } from "../../util/title";
import { signal, useSignal } from "@preact/signals";

import "./login.scss"
import { useAppState } from "../../state/app-state";
import { useLocation } from "preact-iso";
import { ENDPOINT_REQUEST_TOKENS } from "../../util/endpoints";

export default function Login() {
    useTitle("login");

    const username = useSignal("");
    const password = useSignal("");
    const rememberMe = useSignal(true);
    const isDisabled = useSignal(false);

    const appState = useAppState();
    const location = useLocation();

    const signIn = async (e: SubmitEvent) => {
        e.preventDefault();

        isDisabled.value = true;
        try {
            const endpoint = ENDPOINT_REQUEST_TOKENS;
            const payload = {
                identity: username,
                credentials: {
                    "ps:password": password
                },
                token_requests: [{
                    service_policy: "MBI",
                    service_target: "relivewp.net"
                }]
            };

            const payloadText = JSON.stringify(payload);
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: payloadText
            });

            if (!response.ok) {
                return; // todo: show errors
            }

            const { security_tokens } = await response.json();
            console.log(security_tokens);
            appState.token.value = security_tokens[0].token;

            location.route("/");
        } catch {
            // todo: show errors
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
                        onChange={(e) => rememberMe.value = !!e.currentTarget.value}
                        disabled={isDisabled}></input>
                    <span class="remember-me-text">Remember me</span>
                </label>

                <input type="submit" class="submit" value="Sign in" disabled={isDisabled} />
            </form>
        </div>
    )
}