import { InlineConfig } from "~/config";

export interface InlineLoginResponse {
    da_token: string;
    da_session_key: string;
    da_start_time: string;
    da_expires: string;
    sts_inline_flow_token: string;
    cid: string;
    puid: string;
    username: string;
    first_name?: string;
    last_name?: string;
}

export interface TokensError {
    error_code: number;
}

export type InlineLoginResult =
    | { ok: true; data: InlineLoginResponse }
    | { ok: false; error: number };

export async function inlineLogin(cfg: InlineConfig, identity: string, password: string): Promise<InlineLoginResult> {
    const response = await fetch(cfg.postUrl, {
        method: "POST",
        headers: { "Accept": "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({
            identity: identity,
            credentials: { "ps:password": password },
        }),
    });

    if (!response.ok) {
        let code = 0x80048821; // PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD
        try {
            const err: TokensError = await response.json();
            if (err && typeof err.error_code === "number") code = err.error_code;
        } catch (e) { /* non-JSON body */ }
        return { ok: false, error: code };
    }

    const data: InlineLoginResponse = await response.json();
    return { ok: true, data: data };
}
