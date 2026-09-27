import "./message-view.scss";

import type { EasClient } from "@relivewp/eas-sync/host";
import type { Address, AttachmentRef } from "@relivewp/eas-store";

import MessageBody from "./MessageBody";
import { useMessage } from "./useMessage";

const STAMP = new Intl.DateTimeFormat(undefined, {
    weekday: "short", day: "numeric", month: "short", hour: "numeric", minute: "2-digit",
});

// placeholders: the app has no write path at all yet
const ACTIONS = ["reply", "reply all", "forward", "delete"];

type Props = {
    client: EasClient | null,
    folderId: string | null,
    messageId: string | null,
};

function address(who: Address): string {
    return who.name === null || who.name === who.email ? who.email : `${who.name} <${who.email}>`;
}

function size(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function total(attachments: AttachmentRef[]): number {
    return attachments.reduce((sum, attachment) => sum + attachment.size, 0);
}

function Attachments({ attachments }: { attachments: AttachmentRef[] }) {
    const shown = attachments.filter((attachment) => !attachment.inline);
    if (shown.length === 0) return null;

    return (
        <div class="message-attachments">
            <div class="message-attachments-bar">
                <span class="message-attachments-count">
                    {shown.length} {shown.length === 1 ? "file" : "files"} attached
                </span>
                <span class="message-attachments-size">{size(total(shown))}</span>
            </div>

            <ul>
                {shown.map((attachment) => (
                    <li key={attachment.id}>
                        <span class="attachment-name">{attachment.name}</span>
                        <span class="attachment-size">{size(attachment.size)}</span>
                    </li>
                ))}
            </ul>
        </div>
    );
}

export default function MessageView({ client, folderId, messageId }: Props) {
    const { message, loading, fetching, error, retry } = useMessage(client, folderId, messageId);

    // a url can name a message that has since been deleted, or one this device has never synced.
    // saying so beats silently jumping to whatever is newest
    if (message === undefined) {
        const empty = loading
            ? "loading..."
            : messageId === null
                ? "Pick a message to read it."
                : "That message is not in this folder.";

        return <div class="message-view empty"><p class="note">{empty}</p></div>;
    }

    const recipients = message.to.map((who) => who.name ?? who.email).join(", ");

    return (
        <article class="message-view">
            <div class="message-view-bar">
                {ACTIONS.map((action) => (
                    <button key={action} type="button" class="text-button icon">{action}</button>
                ))}

                <span class="message-view-stamp">{STAMP.format(message.receivedAt)}</span>
            </div>

            <h3 class="message-view-subject">
                {message.subject.length > 0 ? message.subject : "(no subject)"}
            </h3>

            <p class="message-view-people">
                {message.from === null ? "unknown sender" : address(message.from)}
                {recipients.length > 0 && <> to {recipients}</>}
            </p>

            <Attachments attachments={message.attachments} />

            {fetching && <p class="note">loading...</p>}

            {error !== null && (
                <p class="error">
                    {error}{" "}
                    <button type="button" class="text-button" onClick={retry}>try again</button>
                </p>
            )}

            <MessageBody body={message.body} />
        </article>
    );
}
