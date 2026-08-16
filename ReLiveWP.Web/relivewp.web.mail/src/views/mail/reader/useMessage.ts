import type { EasClient } from "@relivewp/eas-sync/host";
import type { Message } from "@relivewp/eas-store";
import { useCallback, useEffect, useState } from "preact/hooks";

export type Reading = {
    message: Message | undefined,
    loading: boolean,
    fetching: boolean,
    error: string | null,
    retry: () => void,
};

function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}


export function useMessage(
    client: EasClient | null, folderId: string | null, messageId: string | null,
): Reading {
    const [message, setMessage] = useState<Message | undefined>(undefined);
    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [attempt, setAttempt] = useState(0);

    const retry = useCallback(() => { setAttempt((count) => count + 1); }, []);

    useEffect(() => {
        if (client === null || folderId === null || messageId === null) {
            setMessage(undefined);
            return;
        }

        let live = true;
        setLoading(true);
        setError(null);

        client.message(folderId, messageId)
            .then((found) => {
                if (!live) return;

                setMessage(found);
                setLoading(false);

                if (found?.body?.truncated !== true) return;

                setFetching(true);
                return client.fetchBody(folderId, messageId).then((result) => {
                    if (!live) return;

                    setFetching(false);
                    if (result.message !== undefined) setMessage(result.message);
                    if (!result.ok) setError(result.reason);
                });
            })
            .catch((thrown: unknown) => {
                if (!live) return;

                setLoading(false);
                setFetching(false);
                setError(reason(thrown));
            });

        return () => { live = false; };
    }, [client, folderId, messageId, attempt]);

    return { message, loading, fetching, error, retry };
}
