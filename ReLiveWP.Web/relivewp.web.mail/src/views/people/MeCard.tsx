import type { Contact } from "@relivewp/eas-store";

import { useAppState } from "~/state/app-state";
import ContactName from "./ContactName";
import { initialOf } from "./groups";

type Props = {
    me: Contact | undefined,
    photo: string | undefined,
};

export default function MeCard({ me, photo }: Props) {
    const user = useAppState().user.value;
    const name = me?.displayName ?? user?.username ?? "you";
    const email = me?.emails[0]?.value ?? user?.email_address ?? null;
    const initial = me === undefined ? name.trim().charAt(0).toUpperCase() : initialOf(me);

    return (
        <aside class="me-card">
            <span class="me-accent" />

            <div class="me-identity">
                {photo === undefined
                    ? <span class="me-tile">{initial === "" ? "?" : initial}</span>
                    : <img class="me-tile" src={photo} alt="" decoding="async" />}
            </div>

            <p class="me-name">
                <span class="me-heading">{me === undefined ? name : <ContactName contact={me} />}</span>
                {email !== null && <span class="me-email">{email}</span>}
            </p>

            <hr class="me-rule" />

            <h3 class="panel-label">what&rsquo;s new</h3>
            <p class="note">Nothing to show yet.</p>
        </aside>
    );
}
