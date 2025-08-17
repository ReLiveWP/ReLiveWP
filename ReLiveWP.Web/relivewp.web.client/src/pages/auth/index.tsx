import { ErrorBoundary, Route, Router } from "preact-iso";
import Login from "./login";
import Register from "./register";

export default function Index() {
    return (
        <ErrorBoundary>
            <Router>
                <Route path="/register" component={Register} />
                <Route path="/login" component={Login} />
            </Router>
        </ErrorBoundary>
    );
}