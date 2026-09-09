import type { EasClient } from "@relivewp/eas-sync/host";
import type { Event } from "@relivewp/eas-store";
import { useMemo } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
import { useFolderChanges } from "~/hooks/useFolderChanges";

const LIMIT = 5000;

export type Events = {
    events: Event[],
    loading: boolean,
    error: string | null,
};

const NONE: Event[] = [];

function localMidnight(at: number): number {
    const date = new Date(at);

    return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()).getTime();
}

function floating(event: Event): Event {
    if (!event.allDay) return event;

    return { ...event, startAt: localMidnight(event.startAt), endAt: localMidnight(event.endAt) };
}

export function useEvents(client: EasClient | null, folderIds: string[]): Events {
    const key = folderIds.join("\uffff");
    const ids = useMemo(() => (key === "" ? [] : key.split("\uffff")), [key]);

    const { value, loading, error, reload } = useAsync(
        client === null || ids.length === 0
            ? null
            : async () => {
                const pages = await Promise.all(ids.map((folderId) => client.listEvents({ folderId, limit: LIMIT })));
                return pages.flat().map(floating);
            },
        [client, ids],
        { keep: true });

    useFolderChanges(client, ids, reload);

    return { events: error === null ? value ?? NONE : NONE, loading, error };
}
