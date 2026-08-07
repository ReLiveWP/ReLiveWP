if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import './index.scss';
import './segoe.scss';

import Main from "./Main";
import { render } from "preact"
import { OAUTH_CHANNEL } from "./util/oauth";

if (window.location.pathname === '/login-complete') {
    const connectionId = new URLSearchParams(window.location.search).get('connectionId') ?? '';
    new BroadcastChannel(OAUTH_CHANNEL).postMessage({ connectionId });
    window.close();
}
else {
    if (typeof window !== "undefined") {
        render(<Main />, document.getElementById("app")!);
    }
}