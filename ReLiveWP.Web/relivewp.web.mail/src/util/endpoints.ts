import * as dev from "./endpoints.dev";
import * as prod from "./endpoints.prod";

const endpoints = process.env.NODE_ENV === "production" ? prod : dev;

export const {
    ENDPOINT_REQUEST_TOKENS,
    ENDPOINT_GET_USER,
    ENDPOINT_EAS,
    ENDPOINT_HOME,
    ENDPOINT_SUPPORT,
    SERVICE_TARGET_PORTAL,
    SERVICE_TARGET_EAS,
} = endpoints;
