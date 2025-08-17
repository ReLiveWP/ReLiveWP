import { useEffect } from "preact/hooks"
import { useTitle } from "../../util/title";

export default function Register() {
    useTitle("register");

    return (
        <div>
            <h1>Register</h1>
            <p>Registration is currently disabled.</p>
        </div>
    )
}