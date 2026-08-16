import "./index.scss"

import { ErrorBoundary, LocationProvider, Route, Router } from "preact-iso";

import AccountDetails from "./account-details";
import { Link, useAccentColor, useTitle } from "@relivewp/ui";
import LinkedAccounts from "./linked-acounts";
import SignOut from "./sign-out";
import Devices from "./devices";
import Privacy from "./privacy";

export default function Account() {
    useTitle("my account");
    useAccentColor('teal');

    return (
        <LocationProvider>
            <div class="my-account">
                <h1>my account</h1>
                <div class="my-account-container">
                    <nav class="my-account-sidebar">
                        <ul>
                            <li>
                                <Link href="/my/account" exactMatch activeClass="active">details</Link>
                            </li>
                            <li>
                                <Link href="/my/account/links" activeClass="active">linked accounts</Link>
                            </li>
                            <li>
                                <Link href="/my/account/devices" activeClass="active">computers and devices</Link>
                            </li>
                            <li>
                                <Link href="/my/account/privacy" activeClass="active">privacy and security</Link>
                            </li>
                            <li>
                                <Link href="/my/account/sign-out" activeClass="active">sign out</Link>
                            </li>
                        </ul>
                    </nav>
                    <div class="my-account-content">
                        <ErrorBoundary>
                            <Router>
                                <Route default component={AccountDetails} />
                                <Route path="/links" component={LinkedAccounts} />
                                <Route path="/devices" component={Devices} />
                                <Route path="/privacy" component={Privacy} />
                                <Route path="/sign-out" component={SignOut} />
                            </Router>
                        </ErrorBoundary>
                    </div>
                </div>
            </div>
        </LocationProvider>
    );
}