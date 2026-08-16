import type { EasClient } from "@relivewp/eas-sync/host";
import { useEffect } from "preact/hooks";

import { coalescing } from "~/util/coalesce";

const SETTLE_MS = 750;
const MAX_WAIT_MS = 2000;

// Runs `reload` when the engine reports a change to any of the watched folders. Every view that
// reads a folder needs this, so it lives here rather than being rebuilt per hook: a reader that
// forgets it shows stale data until the page is reloaded, which is how contact photos went stale.
//
// `reload` has to be stable, a useCallback in every current caller.
export function useFolderChanges(
    client: EasClient | null,
    folderIds: readonly (string | null)[],
    reload: () => void,
): void {
    // the array is rebuilt on every render, so the effect keys off its contents
    const key = JSON.stringify(folderIds.filter((id) => id !== null));

    useEffect(() => {
        const watched = new Set<string>(JSON.parse(key) as string[]);
        if (client === null || watched.size === 0) return;

        const refresh = coalescing(reload, SETTLE_MS, MAX_WAIT_MS);
        const unsubscribe = client.on((event) => {
            if (event.kind === "changed" && event.folderIds.some((id) => watched.has(id)))
                refresh.fire();
        });

        return () => {
            refresh.cancel();
            unsubscribe();
        };
    }, [client, key, reload]);
}
