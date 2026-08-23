import { AppStateProvider, createAppState } from "./state/app-state";
import { ErrorBoundary, LocationProvider, Route, Router, lazy, useLocation } from "preact-iso";
import { SiteFooter, SiteHeader, ThemeProvider, useTitle, type NavItem } from "@relivewp/ui";
import { useEffect } from "preact/hooks";

import AuthenticatedRoute from "./components/AuthenticatedRoute";
import { ENDPOINT_SUPPORT } from "./util/endpoints";
import Home from "./pages/index"
import NavLoginLink from "./components/NavLoginLink"

const Auth = lazy(() => import('./pages/auth'));
const My = lazy(() => import('./pages/my'));

const NAV_ITEMS: NavItem[] = [
    { href: "/", label: "discover", exact: true },
    { href: "/downloads", label: "download" },
    { href: "/marketplace", label: "marketplace" },
    { href: ENDPOINT_SUPPORT, label: "how-to" },
    { href: "/my/device", label: "my phone" },
];

const NotFound = () => {
    useTitle("coming soon");

    return <p>Coming soon :3</p>
}

// "/my/*" does not match bare "/my", so it needs its own entry rather than a default inside MyRouter
const GoAccount = () => {
    const { route } = useLocation();
    useEffect(() => route("/my/account", true), []);

    return null;
}

const Main = () => {
    return (
        <ThemeProvider accent="red">
            <AppStateProvider value={createAppState()}>
                <LocationProvider>
                    <SiteHeader items={NAV_ITEMS} trailing={<NavLoginLink />} />
                    <main>
                        <ErrorBoundary>
                            <Router>
                                <Route path="/" component={Home} />
                                <AuthenticatedRoute path="/auth/*" requiredAuthState={false} component={Auth} />
                                <Route path="/my" component={GoAccount} />
                                <AuthenticatedRoute path="/my/*" requiredAuthState={true} component={My} />
                                <Route default component={NotFound} />
                            </Router>
                        </ErrorBoundary>
                    </main>
                    <SiteFooter />
                </LocationProvider>
            </AppStateProvider>
        </ThemeProvider>
    );
}

export default Main;
