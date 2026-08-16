import { AppStateProvider, createAppState } from "./state/app-state";
import { ErrorBoundary, LocationProvider, Route, Router, lazy } from "preact-iso";
import { SiteFooter, SiteHeader, ThemeProvider, useTitle, type NavItem } from "@relivewp/ui";

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
