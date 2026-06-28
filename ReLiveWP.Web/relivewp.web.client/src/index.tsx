if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import './index.scss';
import './segoe.scss';

import Main from "./Main";
import { render } from "preact"

if (window.location.pathname === '/login-complete') {
    const connectionId = new URLSearchParams(window.location.search).get('connectionId') ?? '';
    new BroadcastChannel("a0eb0210-bc9a-4bc5-be15-44ff49b71027").postMessage({ connectionId });
    window.close();
}
else {
    if (typeof window !== "undefined") {
        render(<Main />, document.getElementById("app")!);
    }
}