import { AppStateProvider, createAppState, useAppState } from "./state/app-state";
import { ErrorBoundary, LocationProvider, Route, Router, lazy } from "preact-iso";
import { useAccentColor, useTitle } from "./util/effects";

import AuthenticatedRoute from "./components/AuthenticatedRoute";
import Home from "./pages/index"
import NavHeader from "./components/NavHeader"
import { useSignalEffect } from "@preact/signals";

const Auth = lazy(() => import('./pages/auth'));
const My = lazy(() => import('./pages/my'));

const NotFound = () => {
    useTitle("coming soon");

    return <p>Coming soon :3</p>
}

const AccentHandler = () => {
    const { accent, accentColor } = useAppState();
    useSignalEffect(() => {
        const body = document.body;
        body.className = 'accent-' + accent.value;

        const themeTag = document.querySelector('meta[name="theme-color"]');
        themeTag.setAttribute("content", accentColor.value)
    });

    useAccentColor('red');

    return (
        <LocationProvider>
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
            <footer>
                <p><small>relive for windows phone &bull; windows phone is a trademark of Microsoft Corp.</small></p>
            </footer>
        </LocationProvider>
    );
}

const Main = () => {
    return (
        <AppStateProvider value={createAppState()}>
            <AccentHandler />
        </AppStateProvider>
    );
}

export default Main;