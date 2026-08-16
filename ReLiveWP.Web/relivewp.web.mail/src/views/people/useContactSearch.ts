import type { EasClient } from "@relivewp/eas-sync/host";
import type { Contact } from "@relivewp/eas-store";
import { useEffect, useState } from "preact/hooks";

const LIMIT = 500;
const DEBOUNCE_MS = 200;

export type ContactSearch = {
    results: Contact[] | null,
    searching: boolean,
    error: string | null,
};

const IDLE: ContactSearch = { results: null, searching: false, error: null };

function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}

export function useContactSearch(
    client: EasClient | null, folderId: string | null, text: string,
): ContactSearch {
    const [state, setState] = useState<ContactSearch>(IDLE);
    const query = text.trim();

    useEffect(() => {
        if (client === null || folderId === null || query.length === 0) {
            setState(IDLE);
            return;
        }

        let live = true;
        setState((prev) => ({ ...prev, searching: true, error: null }));

        const timer = setTimeout(() => {
            client.searchContacts({ text: query, folderId, limit: LIMIT })
                .then((results) => {
                    if (live) setState({ results, searching: false, error: null });
                })
                .catch((thrown: unknown) => {
                    if (live) setState({ results: [], searching: false, error: reason(thrown) });
                });
        }, DEBOUNCE_MS);

        return () => {
            live = false;
            clearTimeout(timer);
        };
    }, [client, folderId, query]);

    return state;
}
