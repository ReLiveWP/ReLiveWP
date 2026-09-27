import type { Contact } from "@relivewp/eas-store";
import { ago } from "@relivewp/ui";
import { useState } from "preact/hooks";

import type { Viewer } from "./ActivityCard";
import { contactFor } from "../state/authors";
import ContactName from "./ContactName";
import { initialFor, initialOf } from "../state/groups";
import type { SocialEntry } from "../state/social";
import Tile from "./Tile";
import { useReplies } from "../hooks/useReplies";

const MAX_LENGTH = 300;

type Props = {
    activityId: string,
    canReply: boolean,
    byCid: ReadonlyMap<string, Contact>,
    photos: ReadonlyMap<string, string>,
    me: Viewer,
};

function Reply({ entry, byCid, photos, me }: { entry: SocialEntry } & Pick<Props, "byCid" | "photos" | "me">) {
    const { author } = entry;
    const contact = contactFor(byCid, author.cid);

    const tileUrl = author.is_me ? me.photo : contact === undefined ? (author.avatar_url || undefined) : photos.get(contact.id);
    const initial = author.is_me ? initialFor(me.name) : contact === undefined ? initialFor(author.display_name) : initialOf(contact);
    const name = author.is_me ? me.name : contact === undefined ? author.display_name : <ContactName contact={contact} />;

    return (
        <div class="reply">
            <Tile class="reply-tile" url={tileUrl} initial={initial} />

            <div class="activity-body">
                <p class="activity-author">
                    <span class="activity-name">{name}</span>
                    {author.screen_name !== "" && <span class="activity-handle">{author.screen_name}</span>}
                    <span class="activity-time">{ago(entry.published)}</span>
                </p>
                <p class="activity-text">{entry.content}</p>
            </div>
        </div>
    );
}

function ReplyBox({ posting, error, onPost }: { posting: boolean, error: string | null, onPost: (text: string) => Promise<boolean> }) {
    const [text, setText] = useState("");
    const trimmed = text.trim();
    const remaining = MAX_LENGTH - text.length;
    const ready = trimmed.length > 0 && remaining >= 0 && !posting;

    const submit = async () => {
        if (!ready) return;
        if (await onPost(trimmed)) setText("");
    };

    return (
        <form
            class="reply-box"
            onSubmit={(event) => { event.preventDefault(); void submit(); }}
        >
            <textarea
                value={text}
                placeholder="Reply"
                rows={2}
                disabled={posting}
                onInput={(event) => { setText((event.target as HTMLTextAreaElement).value); }}
                onKeyDown={(event) => {
                    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                        event.preventDefault();
                        void submit();
                    }
                }}
            />
            <div class="reply-box-side">
                <button type="submit" class="text-button" disabled={!ready}>{posting ? "posting" : "reply"}</button>
                <span class={remaining < 0 ? "reply-count over" : "reply-count"}>{remaining}</span>
            </div>
            {error !== null && <p class="error">{error}</p>}
        </form>
    );
}

export default function Replies({ activityId, canReply, byCid, photos, me }: Props) {
    const { entries, loading, error, posting, postError, post } = useReplies(activityId);

    return (
        <div class="replies">
            {error !== null && <p class="error">{error}</p>}
            {error === null && loading && entries.length === 0 && <p class="note">Loading.</p>}
            {error === null && !loading && entries.length === 0 && <p class="note">No replies yet.</p>}

            {entries.map((entry) => <Reply key={entry.id} entry={entry} byCid={byCid} photos={photos} me={me} />)}

            {canReply && <ReplyBox posting={posting} error={postError} onPost={post} />}
        </div>
    );
}
