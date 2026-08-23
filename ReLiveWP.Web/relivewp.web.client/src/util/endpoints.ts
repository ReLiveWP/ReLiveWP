import * as dev from "./endpoints.dev";
import * as prod from "./endpoints.prod";

const endpoints = process.env.NODE_ENV === "production" ? prod : dev;

export const {
    ENDPOINT_REQUEST_TOKENS,
    ENDPOINT_GET_USER,
    ENDPOINT_GET_LINKED_ACCOUNTS,
    ENDPOINT_UPDATE_PROFILE,
    ENDPOINT_USER_PICTURE,
    ENDPOINT_BEGIN_ACCOUNT_LINKING,
    ENDPOINT_LINK_CREDENTIALS,
    ENDPOINT_RELINK_ACCOUNT,
    ENDPOINT_DELETE_CONNECTION,
    ENDPOINT_GET_DEVICES,
    ENDPOINT_GET_EXTENDED_DEVICE_INFO,
    ENDPOINT_DEVICE_PING,
    ENDPOINT_DEVICE_LOCATE,
    ENDPOINT_DEVICE_EVENTS,
    ENDPOINT_DEVICE_REMOVE,
    ENDPOINT_DEVICE_IMAGE,
    ENDPOINT_AVAILABLE_LINKS,
    ENDPOINT_UPDATE_LINK,
    ENDPOINT_CONTACT_SYNC,
    ENDPOINT_CALENDAR_SYNC,
    ENDPOINT_SUPPORT,
    ENDPOINT_REFRESH_TOKENS,
    SSO_AUTHORITY,
    SSO_CLIENT_ID,
    SERVICE_TARGET_PORTAL,
} = endpoints;

export const SSO_REDIRECT_URI = `${window.location.origin}/auth/callback`;
export const SSO_POST_LOGOUT_REDIRECT_URI = `${window.location.origin}/`;

function populateEndpoint(endpoint: string, params: Record<string, string>) {
    for (const [key, value] of Object.entries(params)) {
        endpoint = endpoint.replace(`:${key}`, encodeURIComponent(value));
    }

    return endpoint;
}

export { populateEndpoint };