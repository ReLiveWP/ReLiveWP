import type { Contact } from "@relivewp/eas-store";
import type { ComponentChildren } from "preact";

import type { Viewer } from "./ActivityCard";
import ActivityFeed from "./ActivityFeed";
import { useContactFeed, type ContactFeedState } from "../hooks/useContactFeed";

type Props = {
    tabs: ComponentChildren,
    contact: Contact,
    cid: string | null,
    contacts: Contact[],
    photos: ReadonlyMap<string, string>,
    viewer: Viewer,
};

function noteFor({ shared, entries, loading }: ContactFeedState): string | null {
    if (loading) return entries.length === 0 ? "Loading." : null;
    if (!shared) return "Not sharing anything with you.";

    return entries.length === 0 ? "Nothing recent." : null;
}

export default function ContactActivity({ tabs, contact, cid, contacts, photos, viewer }: Props) {
    const feed = useContactFeed(cid);
    const { entries, loading, error, refresh } = feed;

    const note = cid === null
        ? `Link an account on ${contact.displayName}'s card to see their posts.`
        : (error === null ? noteFor(feed) : null);

    return (
        <section class="whats-new">
            <div class="panel-head">
                {tabs}
                {cid !== null && (
                    <button type="button" class="panel-action" onClick={refresh} disabled={loading}>
                        {loading ? "refreshing" : "refresh"}
                    </button>
                )}
            </div>

            {cid !== null && error !== null && <p class="error">{error}</p>}
            {note !== null && <p class="note">{note}</p>}

            {entries.length > 0 && <ActivityFeed entries={entries} contacts={contacts} photos={photos} viewer={viewer} />}
        </section>
    );
}
