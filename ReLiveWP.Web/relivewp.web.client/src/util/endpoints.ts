let ENDPOINT_REQUEST_TOKENS: string,
    ENDPOINT_GET_USER: string,
    ENDPOINT_GET_LINKED_ACCOUNTS: string,
    ENDPOINT_BEGIN_ACCOUNT_LINKING: string,
    ENDPOINT_RELINK_ACCOUNT: string,
    ENDPOINT_DELETE_CONNECTION: string,
    ENDPOINT_GET_DEVICES: string,
    ENDPOINT_GET_EXTENDED_DEVICE_INFO: string,
    ENDPOINT_PING_DEVICE: string,
    ENDPOINT_DEVICE_IMAGE: string,
    ENDPOINT_AVAILABLE_LINKS: string,
    ENDPOINT_UPDATE_LINK: string;

if (process.env.NODE_ENV === "production") {
    ENDPOINT_REQUEST_TOKENS = "https://login.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "https://login.relivewp.net/auth/user/@me";
    ENDPOINT_GET_LINKED_ACCOUNTS = "https://login.relivewp.net/auth/user/@me/linked-accounts";
    ENDPOINT_BEGIN_ACCOUNT_LINKING = "https://login.relivewp.net/oauth/begin-account-link";
    ENDPOINT_RELINK_ACCOUNT = "https://login.relivewp.net/oauth/begin-relink";
    ENDPOINT_DELETE_CONNECTION = "https://login.relivewp.net/oauth/link";
    ENDPOINT_GET_DEVICES = "https://devices.relivewp.net/devices/@me";
    ENDPOINT_GET_EXTENDED_DEVICE_INFO = "https://devices.relivewp.net/devices/info/:deviceId";
    ENDPOINT_PING_DEVICE = "https://devices.relivewp.net/devices/ping/:deviceId";
    ENDPOINT_DEVICE_IMAGE = "https://devices.relivewp.net/devices/image/:size/:device";
    ENDPOINT_AVAILABLE_LINKS = "https://login.relivewp.net/oauth/available-links";
    ENDPOINT_UPDATE_LINK = "https://login.relivewp.net/oauth/link";
}
else {
    ENDPOINT_REQUEST_TOKENS = "http://login.int.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "http://login.int.relivewp.net/auth/user/@me";
    ENDPOINT_GET_LINKED_ACCOUNTS = "http://login.int.relivewp.net/auth/user/@me/linked-accounts";
    ENDPOINT_BEGIN_ACCOUNT_LINKING = "http://login.int.relivewp.net/oauth/begin-account-link";
    ENDPOINT_RELINK_ACCOUNT = "http://login.int.relivewp.net/oauth/begin-relink";
    ENDPOINT_DELETE_CONNECTION = "http://login.int.relivewp.net/oauth/link";
    ENDPOINT_GET_DEVICES = "http://devices.int.relivewp.net/devices/@me";
    ENDPOINT_GET_EXTENDED_DEVICE_INFO = "http://devices.int.relivewp.net/devices/info/:deviceId";
    ENDPOINT_PING_DEVICE = "http://devices.int.relivewp.net/devices/ping/:deviceId";
    ENDPOINT_DEVICE_IMAGE = "http://devices.int.relivewp.net/devices/image/:size/:device";
    ENDPOINT_AVAILABLE_LINKS = "http://login.int.relivewp.net/oauth/available-links";
    ENDPOINT_UPDATE_LINK = "http://login.int.relivewp.net/oauth/link";
}

function populateEndpoint(endpoint: string, params: Record<string, string>) {
    for (const [key, value] of Object.entries(params)) {
        endpoint = endpoint.replace(`:${key}`, encodeURIComponent(value));
    }  

    return endpoint;
}

export { populateEndpoint, ENDPOINT_GET_DEVICES, ENDPOINT_GET_EXTENDED_DEVICE_INFO, ENDPOINT_REQUEST_TOKENS, ENDPOINT_GET_USER, ENDPOINT_GET_LINKED_ACCOUNTS, ENDPOINT_BEGIN_ACCOUNT_LINKING, ENDPOINT_RELINK_ACCOUNT, ENDPOINT_DELETE_CONNECTION, ENDPOINT_PING_DEVICE, ENDPOINT_DEVICE_IMAGE, ENDPOINT_AVAILABLE_LINKS, ENDPOINT_UPDATE_LINK };