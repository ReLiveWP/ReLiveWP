import type { EasClient } from "@relivewp/eas-sync/host";
import type { Message } from "@relivewp/eas-store";

import { useAsync } from "~/hooks/useAsync";

const LIMIT = 200;
const DEBOUNCE_MS = 200;

export type MessageSearch = {
    results: Message[] | null,
    searching: boolean,
    error: string | null,
};

const NONE: Message[] = [];

export function useMessageSearch(
    client: EasClient | null, folderId: string | null, text: string,
): MessageSearch {
    const query = text.trim();

    const { value, loading, error } = useAsync(
        client === null || folderId === null || query.length === 0
            ? null
            : () => client.search({ text: query, folderId, limit: LIMIT }),
        [client, folderId, query],
        { keep: true, debounceMs: DEBOUNCE_MS });

    return { results: error === null ? value ?? null : NONE, searching: loading, error };
}
