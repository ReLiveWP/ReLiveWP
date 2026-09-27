import type { Contact } from "@relivewp/eas-store";
import { useMemo } from "preact/hooks";

import ActivityCard, { type Viewer } from "./ActivityCard";
import { cidIndex, contactFor } from "../state/authors";
import type { SocialEntry } from "../state/social";

type Props = {
    entries: SocialEntry[],
    contacts: Contact[],
    photos: ReadonlyMap<string, string>,
    viewer: Viewer,
    onSelect?: (contact: Contact) => void,
};

export default function ActivityFeed({ entries, contacts, photos, viewer, onSelect }: Props) {
    const byCid = useMemo(() => cidIndex(contacts), [contacts]);

    return (
        <div class="activity-list">
            {entries.map((entry) => {
                const contact = contactFor(byCid, entry.author.cid);

                return (
                    <ActivityCard
                        key={entry.id}
                        entry={entry}
                        contact={contact}
                        photo={contact === undefined ? undefined : photos.get(contact.id)}
                        byCid={byCid}
                        photos={photos}
                        viewer={viewer}
                        onSelect={onSelect}
                    />
                );
            })}
        </div>
    );
}
