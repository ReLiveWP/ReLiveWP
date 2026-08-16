import type { EasClient } from "@relivewp/eas-sync/host";
import type { Message } from "@relivewp/eas-store";
import { useEffect, useState } from "preact/hooks";

const LIMIT = 200;
const DEBOUNCE_MS = 200;

export type MessageSearch = {
    results: Message[] | null,
    searching: boolean,
    error: string | null,
};

const IDLE: MessageSearch = { results: null, searching: false, error: null };

function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}

export function useMessageSearch(
    client: EasClient | null, folderId: string | null, text: string,
): MessageSearch {
    const [state, setState] = useState<MessageSearch>(IDLE);
    const query = text.trim();

    useEffect(() => {
        if (client === null || folderId === null || query.length === 0) {
            setState(IDLE);
            return;
        }

        let live = true;
        setState((prev) => ({ ...prev, searching: true, error: null }));

        const timer = setTimeout(() => {
            client.search({ text: query, folderId, limit: LIMIT })
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
