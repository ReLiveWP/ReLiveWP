import { useEffect } from "preact/hooks";
import { useLocation } from "preact-iso";

import { DEFAULT_FOLDER, mailPath } from "~/util/routes";

const GoHome = () => {
    const { route } = useLocation();

    useEffect(() => {
        route(mailPath(DEFAULT_FOLDER), true);
    }, [route]);

    return <p class="note">redirecting...</p>;
};

export default GoHome;
