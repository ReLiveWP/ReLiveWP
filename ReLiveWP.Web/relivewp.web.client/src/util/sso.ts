import { beginSignIn, type SsoConfig } from "@relivewp/auth";

import {
    SERVICE_TARGET_PORTAL,
    SSO_AUTHORITY,
    SSO_CLIENT_ID,
    SSO_REDIRECT_URI,
} from "./endpoints";

export function ssoConfig(): SsoConfig {
    return {
        authority: SSO_AUTHORITY,
        clientId: SSO_CLIENT_ID,
        redirectUri: SSO_REDIRECT_URI,
        serviceTargets: [SERVICE_TARGET_PORTAL],
    };
}

export function startSignIn(): Promise<void> {
    return beginSignIn(ssoConfig());
}
