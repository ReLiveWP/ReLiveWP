if (process.env.NODE_ENV === "development") {
    require("preact/debug");
}

import "./index.scss";

import App from "./App";
import { render } from "preact";
import { registerServiceWorker } from "~/util/service-worker";

render(<App />, document.getElementById("app")!);
registerServiceWorker();
