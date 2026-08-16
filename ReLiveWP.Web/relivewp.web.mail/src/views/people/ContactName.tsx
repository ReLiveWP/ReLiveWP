import type { Contact } from "@relivewp/eas-store";

import { nameOf } from "./groups";

// the surname carries the weight because it is the sort key: it is why "Steve Ballmer" sits
// under b. nothing is emphasised when there is no surname to explain
export default function ContactName({ contact }: { contact: Contact }) {
    const { lead, surname } = nameOf(contact);

    if (surname.length === 0) return <>{lead}</>;
    if (lead.length === 0) return <b class="contact-surname">{surname}</b>;

    return (
        <>
            {lead}
            {" "}
            <b class="contact-surname">{surname}</b>
        </>
    );
}
