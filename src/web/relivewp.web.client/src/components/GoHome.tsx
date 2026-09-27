import { useEffect } from "preact/hooks";
import { useLocation } from "preact-iso";

const GoHome = () => {
    const { route } = useLocation();
    useEffect(() => {
        route("/");
    })

    return <p>redirecting...</p>
}

export default GoHome;