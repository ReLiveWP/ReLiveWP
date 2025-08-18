import { useLocation } from "preact-iso";
import { useEffect } from "preact/hooks";

const GoHome = () => {
    const location = useLocation();
    useEffect(() => {
        location.route("/");
    })

    return <p>redirecting...</p>
}

export default GoHome;