import { useCallback, useEffect, useState } from "preact/hooks";

import { InlineConfig } from "~/config";
import { HostProps, finalBack, finalNext, setHeaderText, setProperty, setWizardButtons } from "~/external";
import { InlineLoginResponse, inlineLogin } from "~/util/api";
import { toString } from "~/util/hresult";

function pushSuccess(data: InlineLoginResponse, fallbackName: string) {
    setProperty(HostProps.DAToken, data.da_token);
    setProperty(HostProps.DASessionKey, data.da_session_key);
    setProperty(HostProps.DAStartTime, data.da_start_time);
    setProperty(HostProps.DAExpires, data.da_expires);
    if (data.sts_inline_flow_token) setProperty(HostProps.STSInlineFlowToken, data.sts_inline_flow_token);
    if (data.cid) setProperty(HostProps.CID, data.cid);
    if (data.puid) setProperty(HostProps.PUID, data.puid);
    setProperty(HostProps.Username, data.username || fallbackName);
    setProperty(HostProps.SigninName, data.username || fallbackName);
    if (data.first_name) setProperty(HostProps.FirstName, data.first_name);
    if (data.last_name) setProperty(HostProps.LastName, data.last_name);
}

export default function Login({ config }: { config: InlineConfig }) {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const submit = useCallback(async () => {
        if (busy) return;
        setBusy(true);
        setError(null);
        try {
            const result = await inlineLogin(config, username, password);
            if (!result.ok) {
                // retryable
                setProperty(HostProps.ErrorCode, String(result.error >>> 0));
                setProperty(HostProps.ErrorString, toString(result.error));
                setError(toString(result.error));
                return;
            }
            pushSuccess(result.data, username);
            finalNext();
        } catch (e) {
            const message = e && (e as Error).message ? (e as Error).message : "This service isn't available at the moment, please try again later.";
            setError(message);
        } finally {
            setBusy(false);
        }
    }, [busy, config, username, password]);

    useEffect(() => {
        window.OnNext = () => { void submit(); };
        window.OnBack = () => finalBack();
        return () => { window.OnNext = undefined; window.OnBack = undefined; };
    }, [submit]);

    useEffect(() => {
        setHeaderText("Sign in");
        setWizardButtons(false, true, true);
    }, []);

    const onFormSubmit = (e: Event) => {
        e.preventDefault();
        void submit();
    };

    return (
        <form id="idForm_WP_Window" method="POST" target="_top" novalidate onSubmit={onFormSubmit}>
            <div class="wpBluePage">
                <div class="pageTitleContainer"><div>Sign in</div></div>
                <div class="pageContent">
                    <div class="row">Sign in with your ReLive account to get your email, photos, files and settings on all your devices.</div>
                    <div class="row">
                        <div class="inlineLabel"><label for="idTxtBx_WPIL_Username">ReLive account</label></div>
                        <div>
                            <input type="email" name="loginfmt" id="idTxtBx_WPIL_Username" maxLength={113}
                                placeholder="Email or phone" autocomplete="username" autocorrect="off"
                                value={username} disabled={busy}
                                onInput={(e) => setUsername((e.target as HTMLInputElement).value)} />
                        </div>
                    </div>
                    <div class="row">
                        <div class="inlineLabel"><label for="idTxtBx_WPIL_Password">Password</label></div>
                        <div>
                            <input type="password" name="passwd" id="idTxtBx_WPIL_Password" maxLength={127}
                                autocomplete="off" value={password} disabled={busy}
                                onInput={(e) => setPassword((e.target as HTMLInputElement).value)} />
                        </div>
                        {error && <div class="error" role="alert">{error}</div>}
                    </div>
                </div>
            </div>
            <input type="submit" class="hiddenSubmit" value="" aria-hidden="true" />
        </form>
    );
}
