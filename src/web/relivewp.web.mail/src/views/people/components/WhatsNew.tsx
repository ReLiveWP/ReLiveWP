import type { Contact } from "@relivewp/eas-store";
import type { ComponentChildren } from "preact";

import { ENDPOINT_HOME } from "~/util/endpoints";
import type { Viewer } from "./ActivityCard";
import ActivityFeed from "./ActivityFeed";
import { useWhatsNew, type WhatsNewState } from "../hooks/useWhatsNew";

type Props = {
    tabs: ComponentChildren,
    contacts: Contact[],
    photos: ReadonlyMap<string, string>,
    viewer: Viewer,
    onSelect: (contact: Contact) => void,
};

function noteFor({ connected, entries, loading }: WhatsNewState): ComponentChildren {
    if (!connected) {
        return (
            <>
                No accounts linked yet! Link one to
                {" "}
                <a href={`${ENDPOINT_HOME}my/`}>your account</a>
                {" "}
                to keep up with what your contacts are doing.
            </>
        );
    }

    if (entries.length > 0) return null;

    return loading ? "Loading." : "Nothing new.";
}

export default function WhatsNew({ tabs, contacts, photos, viewer, onSelect }: Props) {
    const feed = useWhatsNew();
    const { entries, loading, error, refresh } = feed;
    const note = error === null ? noteFor(feed) : null;

    return (
        <section class="whats-new">
            <div class="panel-head">
                {tabs}
                <button type="button" class="panel-action" onClick={refresh} disabled={loading}>
                    {loading ? "refreshing" : "refresh"}
                </button>
            </div>

            {error !== null && <p class="error">{error}</p>}
            {note !== null && <p class="note">{note}</p>}

            {entries.length > 0 && (
                <ActivityFeed entries={entries} contacts={contacts} photos={photos} viewer={viewer} onSelect={onSelect} />
            )}
        </section>
    );
}
