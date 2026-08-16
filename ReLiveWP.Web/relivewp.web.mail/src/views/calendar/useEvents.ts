import type { EasClient } from "@relivewp/eas-sync/host";
import type { Event } from "@relivewp/eas-store";
import { useCallback, useEffect, useMemo, useRef, useState } from "preact/hooks";

import { useFolderChanges } from "~/hooks/useFolderChanges";

const LIMIT = 5000;

export type Events = {
    events: Event[],
    loading: boolean,
    error: string | null,
};

const EMPTY: Events = { events: [], loading: false, error: null };

function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}

function localMidnight(at: number): number {
    const date = new Date(at);

    return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()).getTime();
}

function floating(event: Event): Event {
    if (!event.allDay) return event;

    return { ...event, startAt: localMidnight(event.startAt), endAt: localMidnight(event.endAt) };
}

export function useEvents(client: EasClient | null, folderIds: string[]): Events {
    const [state, setState] = useState<Events>(EMPTY);
    const generation = useRef(0);

    const key = folderIds.join("\uffff");
    const ids = useMemo(() => (key === "" ? [] : key.split("\uffff")), [key]);

    const load = useCallback(() => {
        generation.current += 1;
        const mine = generation.current;

        if (client === null || ids.length === 0) {
            setState(EMPTY);
            return;
        }

        setState((prev) => ({ ...prev, loading: true, error: null }));

        Promise.all(ids.map((folderId) => client.listEvents({ folderId, limit: LIMIT }))).then((pages) => {
            if (mine !== generation.current) return;

            setState({ events: pages.flat().map(floating), loading: false, error: null });
        }).catch((thrown: unknown) => {
            if (mine !== generation.current) return;

            setState({ events: [], loading: false, error: reason(thrown) });
        });
    }, [client, ids]);

    useEffect(load, [load]);
    useFolderChanges(client, ids, load);

    return state;
}
