if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import './index.scss';
import './segoe.scss';

import { render } from "preact"

import Main from "./Main";

if (window.location.pathname === '/login-complete') {
    new BroadcastChannel("a0eb0210-bc9a-4bc5-be15-44ff49b71027").postMessage("done!");
    window.close();
}
else {
    if (typeof window !== "undefined") {
        render(<Main />, document.getElementById("app"));
    }
}