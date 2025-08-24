import { Route, RouteProps, useLocation, useRoute } from "preact-iso"

import { useAppState } from "~/state/app-state";
import { useSignalEffect } from "@preact/signals";

export default function AuthenticatedRoute<Props>({ requiredAuthState, ...props }: RouteProps<Props> & Partial<Props> & { requiredAuthState: boolean }) {
    const { isAuthenticated } = useAppState();
    const router = useLocation();

    useSignalEffect(() => {
        if (requiredAuthState && !isAuthenticated.value)
            router.route('/auth/login', true);
        if (!requiredAuthState && isAuthenticated.value)
            router.route('/', true);
    });

    return <Route {...props} />
}