let ENDPOINT_REQUEST_TOKENS: string, ENDPOINT_GET_USER: string;

if (process.env.NODE_ENV === "production") {
    ENDPOINT_REQUEST_TOKENS = "https://login.relivewp.net/auth/request_tokens";
    ENDPOINT_GET_USER = "https://login.relivewp.net/auth/user/@me";
}
else {
    ENDPOINT_REQUEST_TOKENS = "http://localhost:10002/auth/request_tokens";
    ENDPOINT_GET_USER = "http://localhost:10002/auth/user/@me";
}

export { ENDPOINT_REQUEST_TOKENS, ENDPOINT_GET_USER };