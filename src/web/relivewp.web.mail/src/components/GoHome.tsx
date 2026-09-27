import { useEffect } from "preact/hooks";
import { useLocation } from "preact-iso";

import { DEFAULT_FOLDER, mailPath } from "~/util/routes";
import { useAppState } from "~/state/app-state";

const GoHome = () => {
    const { route } = useLocation();
    const { isAuthenticated } = useAppState();

    useEffect(() => {
        if (!isAuthenticated.value) {
            route('/auth/login', true);
        }
        else {
            route(mailPath(DEFAULT_FOLDER), true);
        }
    }, [route]);

    return <p class="note">redirecting...</p>;
};

export default GoHome;
