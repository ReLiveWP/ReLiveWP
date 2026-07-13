import "./styles/segoe.css";
import "./styles/wpwiz.css";
import "./styles/background.css";

import { render } from "preact";
import Login from "~/components/Login";
import { readConfig } from "~/config";

const config = readConfig();

if (!document.body.className) document.body.className = "light";

const mount = document.getElementById("app");
if (mount) render(<Login config={config} />, mount);
