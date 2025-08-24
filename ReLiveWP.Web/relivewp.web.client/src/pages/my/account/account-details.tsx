import { useAppState } from "~/state/app-state";

export default function AccountDetails() {
    const { user } = useAppState();

    return (
        <>
            <h4>Information</h4>
            <dl>
                <dt>id</dt>
                <dd>{user.value.id}</dd>
                <dd>{user.value.cid}</dd>
                <dd>{user.value.puid}</dd>
                <dt>username</dt>
                <dd>{user.value.username} <button>change</button></dd>
                <dt>email address</dt>
                <dd>{user.value.email_address}</dd>
                <dt>password</dt>
                <dd>************ <button>change</button></dd>
            </dl>
        </>
    );
}