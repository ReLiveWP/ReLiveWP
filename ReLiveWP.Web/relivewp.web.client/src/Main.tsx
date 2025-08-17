
import { ErrorBoundary, lazy, LocationProvider, Route, Router } from "preact-iso";

import Home from "./pages/index"
import NavHeader from "./components/NavHeader"

import { AppStateProvider, createAppState } from "./state/app-state";
import { useTitle } from "./util/title";
import AuthenticatedRoute from "./components/AuthenticatedRoute";

const Auth = lazy(() => import('./pages/auth'));
const My = lazy(() => import('./pages/my'));

const NotFound = () => {
    useTitle("coming soon");

    return <p>Coming soon :3</p>
}

const Main = () => {
    return (
        <AppStateProvider value={createAppState()}>
            <LocationProvider>
                <div class="root">
                    <NavHeader />
                    <main>
                        <ErrorBoundary>
                            <Router>
                                <Route path="/" component={Home} />
                                <AuthenticatedRoute path="/auth/*" requiredAuthState={false} component={Auth} />
                                <AuthenticatedRoute path="/my/*" requiredAuthState={true} component={My} />
                                <Route default component={NotFound} />
                            </Router>
                        </ErrorBoundary>
                    </main>
                </div>
            </LocationProvider>
        </AppStateProvider>
    );
}

export default Main;