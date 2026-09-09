import type { Contact } from "@relivewp/eas-store";
import { ago } from "@relivewp/ui";
import { useState } from "preact/hooks";

import ContactName from "./ContactName";
import { initialFor, initialOf } from "../state/groups";
import Replies from "./Replies";
import type { SocialEntry } from "../state/social";
import Tile from "./Tile";

export type Viewer = {
    name: string,
    photo: string | undefined,
};

type Props = {
    entry: SocialEntry,
    contact: Contact | undefined,
    photo: string | undefined,
    byCid: ReadonlyMap<string, Contact>,
    photos: ReadonlyMap<string, string>,
    viewer: Viewer,
    onSelect: ((contact: Contact) => void) | undefined,
};

function repliesLabel(count: number | null): string {
    if (count === null || count === 0) return "reply";
    return count === 1 ? "1 reply" : `${count} replies`;
}

export default function ActivityCard({ entry, contact, photo, byCid, photos, viewer, onSelect }: Props) {
    const { author } = entry;
    const [avatarBroken, setAvatarBroken] = useState(false);
    const [open, setOpen] = useState(false);

    const tileUrl = contact === undefined
        ? (avatarBroken || author.avatar_url === "" ? undefined : author.avatar_url)
        : photo;
    const initial = contact === undefined ? initialFor(author.display_name) : initialOf(contact);

    const name = contact === undefined
        ? <span class="activity-name">{author.display_name}</span>
        : (
            <button type="button" class="activity-name activity-contact" onClick={() => { onSelect?.(contact); }}>
                <ContactName contact={contact} />
            </button>
        );

    const hasReplies = entry.reply_count !== null && entry.reply_count > 0;
    const canOpen = entry.can_reply || hasReplies;

    return (
        <article class="activity-card">
            <Tile class="activity-tile" url={tileUrl} initial={initial} onError={() => { setAvatarBroken(true); }} />

            <div class="activity-body">
                <p class="activity-author">
                    {name}
                    {author.screen_name !== "" && <span class="activity-handle">{author.screen_name}</span>}
                    <a class="activity-time" href={entry.canonical_url} target="_blank" rel="noopener noreferrer">
                        {ago(entry.published)}
                    </a>
                </p>

                <p class="activity-text">{entry.content}</p>

                {entry.photos.length > 0 && (
                    <div class="activity-photos">
                        {entry.photos.map((item) => (
                            <a key={item.full_size_url} href={item.full_size_url} target="_blank" rel="noopener noreferrer">
                                <img src={item.thumbnail_url} alt="" loading="lazy" decoding="async" />
                            </a>
                        ))}
                    </div>
                )}

                {canOpen && (
                    <button
                        type="button"
                        class={open ? "activity-toggle open" : "activity-toggle"}
                        onClick={() => { setOpen((prev) => !prev); }}
                    >
                        {repliesLabel(entry.reply_count)}
                    </button>
                )}

                {open && (
                    <Replies
                        activityId={entry.id}
                        canReply={entry.can_reply}
                        byCid={byCid}
                        photos={photos}
                        me={viewer}
                    />
                )}
            </div>
        </article>
    );
}
