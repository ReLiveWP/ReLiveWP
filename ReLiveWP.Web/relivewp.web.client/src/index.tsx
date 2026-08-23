if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import './index.scss';
import "./util/shims";

import Main from "./Main";
import { render } from "preact"
import { OAUTH_CHANNEL } from "./util/oauth";
import { postBroadcast } from "./util/broadcast";

if (window.location.pathname === '/login-complete') {
    const connectionId = new URLSearchParams(window.location.search).get('connectionId') ?? '';
    postBroadcast(OAUTH_CHANNEL, { connectionId });
    window.close();
}
else {
    if (typeof window !== "undefined") {
        render(<Main />, document.getElementById("app")!);
    }
}