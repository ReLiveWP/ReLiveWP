let ENDPOINT_REQUEST_TOKENS: string, ENDPOINT_GET_USER: string, ENDPOINT_BEGIN_ACCOUNT_LINKING: string

if (process.env.NODE_ENV === "production") {
    ENDPOINT_REQUEST_TOKENS = "https://login.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "https://login.relivewp.net/auth/user/@me";
    ENDPOINT_BEGIN_ACCOUNT_LINKING = "https://login.relivewp.net/oauth/begin-account-link";
}
else {
    ENDPOINT_REQUEST_TOKENS = "https://login.int.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "https://login.int.relivewp.net/auth/user/@me";
    ENDPOINT_BEGIN_ACCOUNT_LINKING = "https://login.int.relivewp.net/oauth/begin-account-link";
}

export { ENDPOINT_REQUEST_TOKENS, ENDPOINT_GET_USER, ENDPOINT_BEGIN_ACCOUNT_LINKING };