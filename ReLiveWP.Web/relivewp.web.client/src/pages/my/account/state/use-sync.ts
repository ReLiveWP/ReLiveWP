import { Signal, useSignal } from "@preact/signals";
import { useContext } from "preact/hooks";
import { createContext } from "preact";

import { useAuthenticatedFetch } from "~/state/app-state";
import { useFetchSignal } from "~/util/use-fetch";
import { ENDPOINT_CALENDAR_SYNC, ENDPOINT_CONTACT_SYNC } from "~/util/endpoints";

export type Sync = {
    connection_id: string;
    service_id: string;
    enabled: boolean;
    running: boolean;
    queued: boolean;
    last_synced_at?: string | null;
    last_failure?: string | null;
    created: number;
    updated: number;
    deleted: number;
    skipped: number;
};

export type SyncList = { connections?: Sync[] };

const POLL_INTERVAL_MS = 2000;
const POLL_ATTEMPTS = 120;

export const ContactSyncContext = createContext<Signal<SyncList | null>>(null!);
export const CalendarSyncContext = createContext<Signal<SyncList | null>>(null!);

export function useContactSyncList() {
    return useFetchSignal<SyncList>(ENDPOINT_CONTACT_SYNC);
}

export function useCalendarSyncList() {
    return useFetchSignal<SyncList>(ENDPOINT_CALENDAR_SYNC);
}

export function useContactSync(connectionId: string) {
    return useSync(connectionId, ENDPOINT_CONTACT_SYNC, ContactSyncContext);
}

export function useCalendarSync(connectionId: string) {
    return useSync(connectionId, ENDPOINT_CALENDAR_SYNC, CalendarSyncContext);
}

function useSync(
    connectionId: string,
    endpoint: string,
    context: typeof ContactSyncContext
) {
    const fetch = useAuthenticatedFetch();
    const all = useContext(context);
    const busy = useSignal(false);
    const error = useSignal<string | null>(null);

    const status = all.value?.connections?.find(c => c.connection_id === connectionId);

    const put = (next: Sync) => {
        const others = (all.value?.connections ?? []).filter(c => c.connection_id !== next.connection_id);
        all.value = { connections: [...others, next] };
    };

    const send = async (method: "POST" | "PUT", body: object) => {
        if (busy.value) return;

        busy.value = true;
        error.value = null;

        try {
            const response = await fetch(endpoint, {
                method,
                headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            if (!response.ok) {
                error.value = await response.text() || "that didn't work";
                return;
            }

            const started = await response.json() as Sync;
            put(started);

            if (!started.running && !started.queued)
                return;

            // the run is handed to the poller, so the reply only says it was queued
            for (let i = 0; i < POLL_ATTEMPTS; i++) {
                await new Promise(resolve => setTimeout(resolve, POLL_INTERVAL_MS));

                const params = new URLSearchParams([["connectionId", connectionId]]);
                const poll = await fetch(`${endpoint}?${params}`);
                if (!poll.ok) return;

                const { connections } = await poll.json() as SyncList;
                const latest = connections?.find(c => c.connection_id === connectionId);
                if (!latest) return;

                put(latest);
                if (!latest.running && !latest.queued) return;
            }
        } catch (e) {
            error.value = e instanceof Error ? e.message : "that didn't work";
        } finally {
            busy.value = false;
        }
    };

    return {
        status,
        busy,
        error,
        syncNow: () => send("POST", { connection_id: connectionId }),
        setEnabled: (enabled: boolean) => send("PUT", { connection_id: connectionId, enabled }),
    };
}
