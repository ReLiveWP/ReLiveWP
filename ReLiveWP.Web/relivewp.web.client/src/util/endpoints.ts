let ENDPOINT_REQUEST_TOKENS: string,
    ENDPOINT_GET_USER: string,
    ENDPOINT_GET_LINKED_ACCOUNTS: string,
    ENDPOINT_BEGIN_ACCOUNT_LINKING: string

if (process.env.NODE_ENV === "production") {
    ENDPOINT_REQUEST_TOKENS = "https://login.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "https://login.relivewp.net/auth/user/@me";
    ENDPOINT_GET_LINKED_ACCOUNTS = "https://login.relivewp.net/auth/user/@me/linked-accounts";
    ENDPOINT_BEGIN_ACCOUNT_LINKING = "https://login.relivewp.net/oauth/begin-account-link";
}
else {
    ENDPOINT_REQUEST_TOKENS = "https://login.int.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "https://login.int.relivewp.net/auth/user/@me";
    ENDPOINT_GET_LINKED_ACCOUNTS = "https://login.int.relivewp.net/auth/user/@me/linked-accounts";
    ENDPOINT_BEGIN_ACCOUNT_LINKING = "https://login.int.relivewp.net/oauth/begin-account-link";
}

export { ENDPOINT_REQUEST_TOKENS, ENDPOINT_GET_USER, ENDPOINT_GET_LINKED_ACCOUNTS, ENDPOINT_BEGIN_ACCOUNT_LINKING };