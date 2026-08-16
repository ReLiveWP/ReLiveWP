import type { EasClient } from "@relivewp/eas-sync/host";
import type { Folder, FolderCounts } from "@relivewp/eas-store";
import { useMemo } from "preact/hooks";

import { useCounts } from "~/hooks/useCounts";
import { useFolders } from "~/hooks/useFolders";
import { findFolder } from "~/util/routes";

const ORDER: Readonly<Record<string, number>> = {
    inbox: 0, drafts: 1, sent: 2, outbox: 3, deleted: 4, junk: 5,
};

export type Mailbox = {
    folders: Folder[],
    counts: Record<string, FolderCounts>,
    folder: Folder | null,
    // the slug named a folder that is not in a tree we have actually seen, so it is stale rather
    // than merely early
    missing: boolean,
    loaded: boolean,
    error: string | null,
};

export function mailFolders(folders: Folder[]): Folder[] {
    return folders
        .filter((folder) => folder.class === "Email" || folder.role in ORDER)
        .sort((a, b) => (ORDER[a.role] ?? 9) - (ORDER[b.role] ?? 9) || a.name.localeCompare(b.name));
}

export function useMailbox(client: EasClient | null, slug: string): Mailbox {
    const { folders: all, loaded, error } = useFolders(client);
    const counts = useCounts(client);

    const folders = useMemo(() => mailFolders(all), [all]);
    const folder = useMemo(() => findFolder(folders, slug) ?? null, [folders, slug]);

    return { folders, counts, folder, missing: loaded && folder === null, loaded, error };
}

export function summarise(counts: FolderCounts | undefined): string | undefined {
    if (counts === undefined) return undefined;

    const messages = `${counts.total} ${counts.total === 1 ? "message" : "messages"}`;

    return counts.unread === 0 ? messages : `${messages} · ${counts.unread} unread`;
}
