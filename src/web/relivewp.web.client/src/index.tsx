if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import './index.scss';
import "./util/shims";

import Main from "./Main";
import { render } from "preact"
import { OAUTH_CHANNEL, type OAuthLinkResult } from "./util/oauth";
import { postBroadcast } from "./util/broadcast";

if (window.location.pathname === '/login-complete') {
    const params = new URLSearchParams(window.location.search);
    const error = params.get('error');
    const result: OAuthLinkResult = error !== null
        ? { error }
        : { connectionId: params.get('connectionId') ?? '' };

    postBroadcast(OAUTH_CHANNEL, result);
    window.close();
}
else {
    if (typeof window !== "undefined") {
        render(<Main />, document.getElementById("app")!);
    }
}