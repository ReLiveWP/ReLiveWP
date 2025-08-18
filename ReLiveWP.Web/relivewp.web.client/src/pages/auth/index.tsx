import { ErrorBoundary, Route, Router } from "preact-iso";
import Login from "./login";
import Register from "./register";
import GoHome from "../../components/GoHome";
import { useAccentColor } from "../../util/effects";

export default function Index() {
    useAccentColor('magenta');

    return (
        <ErrorBoundary>
            <Router>
                <Route path="/register" component={Register} />
                <Route path="/login" component={Login} />
                <Route default component={GoHome} />
            </Router>
        </ErrorBoundary>
    );
}