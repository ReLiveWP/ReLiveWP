import { ErrorBoundary, Route, Router } from "preact-iso";

import GoHome from "~/components/GoHome";
import Callback from "./callback";
import Login from "./login";
import Register from "./register";
import { useAccentColor } from "@relivewp/ui";

export default function Index() {
    useAccentColor('magenta');

    return (
        <ErrorBoundary>
            <Router>
                <Route path="/register" component={Register} />
                <Route path="/login" component={Login} />
                <Route path="/callback" component={Callback} />
                <Route default component={GoHome} />
            </Router>
        </ErrorBoundary>
    );
}