if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import './index.scss';
import './segoe.scss';

import { render } from "preact"

import Main from "./Main";

if (typeof window !== "undefined") {
    render(<Main />, document.getElementById("app"));
}