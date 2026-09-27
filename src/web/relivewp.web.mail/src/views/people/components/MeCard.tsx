import type { Contact } from "@relivewp/eas-store";

import { useAppState } from "~/state/app-state";
import ContactCard from "./ContactCard";

type Props = {
    me: Contact | undefined,
    photo: string | undefined,
};

// the me contact can be missing before the first sync, so the signed-in user stands in
export default function MeCard({ me, photo }: Props) {
    const user = useAppState().user.value;

    return (
        <ContactCard
            contact={me}
            photo={photo}
            fallbackName={user?.username ?? "you"}
            fallbackEmail={user?.email_address ?? null}
        />
    );
}
