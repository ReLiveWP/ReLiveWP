import "./mail.scss";

import type { Message } from "@relivewp/eas-store";
import { useTitle } from "@relivewp/ui";
import { useLocation } from "preact-iso";
import { useCallback, useEffect } from "preact/hooks";

import Page from "~/components/Page";
import { useSearch, useShell } from "~/state/shell";
import { useSync } from "~/state/sync";
import { DEFAULT_FOLDER, folderSlug, mailPath } from "~/util/routes";
import FolderList from "./folders/FolderList";
import { summarise, useMailbox } from "./folders/useMailbox";
import MessageList from "./list/MessageList";
import { remember } from "./opened";
import MessageView from "./reader/MessageView";

const SEARCH = { placeholder: "search mail" };

type Props = {
    slug?: string,
    messageId?: string,
};

export default function Mail({ slug = DEFAULT_FOLDER, messageId }: Props) {
    useSearch(SEARCH);

    const { route } = useLocation();
    const shell = useShell();
    const client = useSync().client.value;

    const { folders, counts, folder, missing, error } = useMailbox(client, slug);
    const open = messageId ?? null;

    useTitle(folder === null ? "mail" : folder.name.toLowerCase());

    const select = useCallback((message: Message) => {
        route(mailPath(slug, message.id));
    }, [route, slug]);

    const fallback = useCallback((messageId: string) => {
        route(mailPath(slug, messageId), true);
    }, [route, slug]);

    useEffect(() => {
        if (folder !== null && open !== null) remember(folder.id, open);
    }, [folder, open]);

    useEffect(() => {
        if (missing && slug !== DEFAULT_FOLDER) route(mailPath(DEFAULT_FOLDER), true);
    }, [missing, slug, route]);

    useEffect(() => { shell.query.value = ""; }, [slug, shell]);

    return (
        <Page
            class="mail-page"
            title={folder?.name ?? "mail"}
            subtitle={folder === null ? undefined : summarise(counts[folder.id])}
            actions={<button type="button" class="text-button icon">new message</button>}
            sidebar={
                <FolderList
                    folders={folders}
                    counts={counts}
                    selected={folder?.id ?? null}
                    onSelect={(id) => {
                        const picked = folders.find((candidate) => candidate.id === id);
                        if (picked !== undefined) route(mailPath(folderSlug(picked)));
                    }}
                />
            }
            detail={folder === null
                ? undefined
                : <MessageView client={client} folderId={folder.id} messageId={open} />}
        >
            {error !== null && <p class="error">{error}</p>}

            {client === null && <p class="note">Connecting.</p>}
            {client !== null && folder === null && error === null
                && <p class="note">No mail folder yet. Sync to fetch the folder list.</p>}

            {folder !== null && (
                <MessageList
                    key={folder.id}
                    client={client}
                    folderId={folder.id}
                    query={shell.query.value}
                    selected={open}
                    onSelect={select}
                    onFallback={fallback}
                />
            )}
        </Page>
    );
}
