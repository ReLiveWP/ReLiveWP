import type { EasClient } from "@relivewp/eas-sync/host";
import type { Contact } from "@relivewp/eas-store";

import { useAsync } from "~/hooks/useAsync";

const LIMIT = 500;
const DEBOUNCE_MS = 200;

export type ContactSearch = {
    results: Contact[] | null,
    searching: boolean,
    error: string | null,
};

const NONE: Contact[] = [];

export function useContactSearch(
    client: EasClient | null, folderId: string | null, text: string,
): ContactSearch {
    const query = text.trim();

    const { value, loading, error } = useAsync(
        client === null || folderId === null || query.length === 0
            ? null
            : () => client.searchContacts({ text: query, folderId, limit: LIMIT }),
        [client, folderId, query],
        { keep: true, debounceMs: DEBOUNCE_MS });

    return { results: error === null ? value ?? null : NONE, searching: loading, error };
}
