import { Route, RouteProps, useLocation } from "preact-iso"

import { useAppState } from "~/state/app-state";
import { useSignalEffect } from "@preact/signals";
import { DEFAULT_FOLDER, mailPath } from "~/util/routes";

export default function AuthenticatedRoute<Props>({ requiredAuthState, ...props }: RouteProps<Props> & Partial<Props> & { requiredAuthState: boolean }) {
    const { isAuthenticated, hasIdentity } = useAppState();
    const { route } = useLocation();

    useSignalEffect(() => {
        if (requiredAuthState && !hasIdentity.value)
            route('/auth/login', true);
        if (!requiredAuthState && isAuthenticated.value)
            route(mailPath(DEFAULT_FOLDER), true);
    });

    return <Route {...props} />
}
