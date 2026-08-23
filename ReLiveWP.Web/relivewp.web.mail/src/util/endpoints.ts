import * as dev from "./endpoints.dev";
import * as prod from "./endpoints.prod";

const endpoints = process.env.NODE_ENV === "production" ? prod : dev;

export const {
    ENDPOINT_REQUEST_TOKENS,
    ENDPOINT_REFRESH_TOKENS,
    ENDPOINT_GET_USER,
    ENDPOINT_EAS,
    ENDPOINT_HOME,
    ENDPOINT_SUPPORT,
    SSO_AUTHORITY,
    SSO_CLIENT_ID,
    SERVICE_TARGET_PORTAL,
    SERVICE_TARGET_EAS,
} = endpoints;

export const SSO_REDIRECT_URI = `${window.location.origin}/auth/callback`;
export const SSO_POST_LOGOUT_REDIRECT_URI = `${window.location.origin}/`;
