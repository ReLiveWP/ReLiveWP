import { useEffect } from "preact/hooks";
import { useLocation } from "preact-iso";

const GoHome = () => {
    const location = useLocation();
    useEffect(() => {
        location.route("/");
    })

    return <p>redirecting...</p>
}

export default GoHome;