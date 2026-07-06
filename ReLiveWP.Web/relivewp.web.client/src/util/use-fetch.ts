import { Signal, useSignal } from "@preact/signals";
import { useCallback, useEffect } from "preact/hooks";

import { useAuthenticatedFetch } from "~/state/app-state";

export type FetchSignal<T> = {
    data: Signal<T | null>;
    error: Signal<string | null>;
    loading: Signal<boolean>;
    refresh: () => Promise<void>;
};

export function useFetchSignal<T>(url: string, onData?: (data: T) => void): FetchSignal<T> {
    const fetch = useAuthenticatedFetch();
    const data = useSignal<T | null>(null);
    const error = useSignal<string | null>(null);
    const loading = useSignal(false);

    const refresh = useCallback(async (signal?: AbortSignal) => {
        if (!url) return;

        loading.value = true;
        error.value = null;
        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                signal,
            });

            if (!response.ok) {
                error.value = `request failed: ${response.status} ${response.statusText}`;
                return;
            }

            const json = await response.json() as T;
            data.value = json;
            onData?.(json);
        }
        catch (e) {
            if ((e as Error).name === 'AbortError') return;
            error.value = (e as Error).message;
        }
        finally {
            loading.value = false;
        }
    }, [url]);

    useEffect(() => {
        const controller = new AbortController();
        refresh(controller.signal);
        return () => controller.abort();
    }, [refresh]);

    return { data, error, loading, refresh };
}
