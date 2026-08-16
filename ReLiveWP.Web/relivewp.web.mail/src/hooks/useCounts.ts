import type { EasClient } from "@relivewp/eas-sync/host";
import type { FolderCounts } from "@relivewp/eas-store";
import { useEffect, useState } from "preact/hooks";

import { coalescing } from "~/util/coalesce";

const SETTLE_MS = 750;
const MAX_WAIT_MS = 2000;

export function useCounts(client: EasClient | null): Record<string, FolderCounts> {
    const [counts, setCounts] = useState<Record<string, FolderCounts>>({});

    useEffect(() => {
        if (client === null) {
            setCounts({});
            return;
        }

        let live = true;

        const load = (): void => {
            client.counts().then((all) => {
                if (live) setCounts(all);
            }).catch(() => {
                // a folder list without its badges is still a folder list
            });
        };

        load();

        const refresh = coalescing(load, SETTLE_MS, MAX_WAIT_MS);
        const unsubscribe = client.on((event) => {
            if (event.kind === "changed" || event.kind === "folders") refresh.fire();
        });

        return () => {
            live = false;
            refresh.cancel();
            unsubscribe();
        };
    }, [client]);

    return counts;
}
