import { Signal, useSignal } from "@preact/signals";
import { useContext } from "preact/hooks";
import { createContext } from "preact";

import { useAuthenticatedFetch } from "~/state/app-state";
import { useFetchSignal } from "~/util/use-fetch";
import { ENDPOINT_CONTACT_SYNC } from "~/util/endpoints";

export type ContactSync = {
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

type ContactSyncList = { connections?: ContactSync[] };

export const ContactSyncContext = createContext<Signal<ContactSyncList | null>>(null!);

const POLL_INTERVAL_MS = 2000;
const POLL_ATTEMPTS = 120;

export function useContactSyncList() {
    return useFetchSignal<ContactSyncList>(ENDPOINT_CONTACT_SYNC);
}

export function useContactSync(connectionId: string) {
    const fetch = useAuthenticatedFetch();
    const all = useContext(ContactSyncContext);
    const busy = useSignal(false);
    const error = useSignal<string | null>(null);

    const status = all.value?.connections?.find(c => c.connection_id === connectionId);

    const put = (next: ContactSync) => {
        const others = (all.value?.connections ?? []).filter(c => c.connection_id !== next.connection_id);
        all.value = { connections: [...others, next] };
    };

    const send = async (method: "POST" | "PUT", body: object) => {
        if (busy.value) return;

        busy.value = true;
        error.value = null;

        try {
            const response = await fetch(ENDPOINT_CONTACT_SYNC, {
                method,
                headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            if (!response.ok) {
                error.value = await response.text() || "that didn't work";
                return;
            }

            const started = await response.json() as ContactSync;
            put(started);

            if (!started.running && !started.queued) 
                return;

            // the run is handed to the poller, so the reply only says it was queued
            for (let i = 0; i < POLL_ATTEMPTS; i++) {
                await new Promise(resolve => setTimeout(resolve, POLL_INTERVAL_MS));

                const params = new URLSearchParams([["connectionId", connectionId]]);
                const poll = await fetch(`${ENDPOINT_CONTACT_SYNC}?${params}`);
                if (!poll.ok) return;

                const { connections } = await poll.json() as ContactSyncList;
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
