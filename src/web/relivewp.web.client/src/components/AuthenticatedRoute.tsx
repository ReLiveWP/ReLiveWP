import { Route, RouteProps, useLocation, useRoute } from "preact-iso"

import { useAppState } from "~/state/app-state";
import { useSignalEffect } from "@preact/signals";

export default function AuthenticatedRoute<Props>({ requiredAuthState, ...props }: RouteProps<Props> & Partial<Props> & { requiredAuthState: boolean }) {
    const { isAuthenticated } = useAppState();
    const { route } = useLocation();

    useSignalEffect(() => {
        if (requiredAuthState && !isAuthenticated.value)
            route('/auth/login', true);
        if (!requiredAuthState && isAuthenticated.value)
            route('/', true);
    });

    return <Route {...props} />
}