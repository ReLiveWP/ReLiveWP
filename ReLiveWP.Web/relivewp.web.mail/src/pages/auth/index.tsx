import { ErrorBoundary, Route, Router } from "preact-iso";

import GoHome from "~/components/GoHome";
import Login from "./login";

export default function Auth() {
    return (
        <ErrorBoundary>
            <Router>
                <Route path="/login" component={Login} />
                <Route default component={GoHome} />
            </Router>
        </ErrorBoundary>
    );
}
