import { ErrorBoundary, LocationProvider, Route, Router, lazy, useLocation } from "preact-iso";
import { SiteFooter, SiteHeader, ThemeProvider, type NavItem, type SearchProps } from "@relivewp/ui";
import { useMemo } from "preact/hooks";

import AuthenticatedRoute from "~/components/AuthenticatedRoute";
import GoHome from "~/components/GoHome";
import NavAccount from "~/components/NavAccount";
import { AppStateProvider, createAppState } from "~/state/app-state";
import { ShellProvider, createShellState, useShell } from "~/state/shell";
import { SyncProvider, createSyncState } from "~/state/sync";
import { ENDPOINT_HOME } from "~/util/endpoints";

const Mail = lazy(() => import("~/views/mail"));
const People = lazy(() => import("~/views/people"));
const Calendar = lazy(() => import("~/views/calendar"));
const Todo = lazy(() => import("~/views/todo"));
const Auth = lazy(() => import("~/pages/auth"));

const NAV_ITEMS: NavItem[] = [
    { href: "/mail", label: "mail" },
    { href: "/people", label: "people" },
    { href: "/calendar", label: "calendar" },
    { href: "/todo", label: "to-do" },
];

function AppHeader() {
    const shell = useShell();
    const request = shell.search.value;

    const search: SearchProps | undefined = request === null ? undefined : {
        value: shell.query.value,
        placeholder: request.placeholder,
        onInput: (value) => { shell.query.value = value; },
        onSubmit: (value) => request.onSubmit?.(value),
    };

    return (
        <SiteHeader
            items={NAV_ITEMS}
            search={search}
            trailing={<NavAccount />}
        />
    );
}

function AppFooter() {
    const { path } = useLocation();

    return path.startsWith("/auth") ? <SiteFooter /> : null;
}

export default function App() {
    const appState = useMemo(createAppState, []);
    const shell = useMemo(createShellState, []);
    const sync = useMemo(() => createSyncState(appState), [appState]);

    return (
        <ThemeProvider accent="blue" titleSuffix="relive mail">
            <AppStateProvider value={appState}>
                <SyncProvider value={sync}>
                    <ShellProvider value={shell}>
                        <LocationProvider>
                            <AppHeader />
                            <main>
                                <ErrorBoundary>
                                    <Router>
                                        <AuthenticatedRoute path="/mail/:slug?/:messageId?" requiredAuthState={true} component={Mail} />
                                        <AuthenticatedRoute path="/calendar/:mode?/:date?" requiredAuthState={true} component={Calendar} />
                                        <AuthenticatedRoute path="/people" requiredAuthState={true} component={People} />
                                        <AuthenticatedRoute path="/todo" requiredAuthState={true} component={Todo} />
                                        <AuthenticatedRoute path="/auth/*" requiredAuthState={false} component={Auth} />
                                        <Route default component={GoHome} />
                                    </Router>
                                </ErrorBoundary>
                            </main>
                            <AppFooter />
                        </LocationProvider>
                    </ShellProvider>
                </SyncProvider>
            </AppStateProvider>
        </ThemeProvider>
    );
}
