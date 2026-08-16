import type { EasClient } from "@relivewp/eas-sync/host";
import { useCallback, useEffect, useRef, useState } from "preact/hooks";

import { useFolderChanges } from "~/hooks/useFolderChanges";

const EMPTY: ReadonlyMap<string, string> = new Map();

// one call for the whole folder rather than one per row: a blob crosses the worker boundary by
// reference, so the expensive part would have been the round trips, not the bytes
export function usePhotos(
    client: EasClient | null, folderId: string | null): ReadonlyMap<string, string> {
    const [urls, setUrls] = useState<ReadonlyMap<string, string>>(EMPTY);

    // a read that is superseded, or outlives the hook, has no map to revoke what it created
    const generation = useRef(0);

    const load = useCallback(() => {
        generation.current++;

        if (client === null || folderId === null) {
            setUrls(EMPTY);
            return;
        }

        const mine = generation.current;

        client.contactPhotos(folderId).then((photos) => {
            const next = new Map<string, string>();
            for (const [id, blob] of photos) next.set(id, URL.createObjectURL(blob));

            if (mine !== generation.current) {
                for (const url of next.values()) URL.revokeObjectURL(url);
                return;
            }

            setUrls(next);
        }).catch(() => {
            // a contact without a picture is the normal case, so a failed read is not worth
            // surfacing next to the list it decorates
            if (mine === generation.current) setUrls(EMPTY);
        });
    }, [client, folderId]);

    useEffect(() => load(), [load]);
    useEffect(() => () => { generation.current++; }, []);

    useFolderChanges(client, [folderId], load);

    // revoking on replacement rather than on create keeps each url alive until the map that
    // replaced it has rendered, so a row never points at a url that has already been released
    useEffect(() => () => {
        for (const url of urls.values()) URL.revokeObjectURL(url);
    }, [urls]);

    return urls;
}
