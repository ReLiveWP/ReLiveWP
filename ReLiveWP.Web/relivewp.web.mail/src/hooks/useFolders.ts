import type { EasClient } from "@relivewp/eas-sync/host";
import type { Folder } from "@relivewp/eas-store";
import { useEffect, useState } from "preact/hooks";

export type Folders = {
    folders: Folder[],
    loaded: boolean,
    error: string | null,
};

export function useFolders(client: EasClient | null): Folders {
    const [folders, setFolders] = useState<Folder[]>([]);
    const [loaded, setLoaded] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (client === null) {
            setFolders([]);
            setLoaded(false);
            return;
        }

        let live = true;

        const load = () => {
            client.folders().then((all) => {
                if (!live) return;

                setFolders(all);
                setLoaded(true);
                setError(null);
            }).catch((thrown: unknown) => {
                if (!live) return;

                setError(thrown instanceof Error ? thrown.message : String(thrown));
            });
        };

        load();
        const unsubscribe = client.on((event) => {
            if (event.kind === "folders") load();
        });

        return () => {
            live = false;
            unsubscribe();
        };
    }, [client]);

    return { folders, loaded, error };
}
