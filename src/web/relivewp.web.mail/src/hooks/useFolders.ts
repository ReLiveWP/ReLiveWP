import type { EasClient } from "@relivewp/eas-sync/host";
import type { Folder } from "@relivewp/eas-store";
import { useEffect } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";

export type Folders = {
    folders: Folder[],
    loaded: boolean,
    error: string | null,
};

const NONE: Folder[] = [];

export function useFolders(client: EasClient | null): Folders {
    const { value, error, reload } = useAsync(
        client === null ? null : () => client.folders(),
        [client],
        { keep: true });

    useEffect(() => {
        if (client === null) return;

        return client.on((event) => {
            if (event.kind === "folders") reload();
        });
    }, [client, reload]);

    return { folders: value ?? NONE, loaded: value !== undefined, error };
}
