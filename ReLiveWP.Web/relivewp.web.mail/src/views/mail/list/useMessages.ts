import type { EasClient } from "@relivewp/eas-sync/host";
import type { Message, MessageKey } from "@relivewp/eas-store";
import { useCallback, useEffect, useRef, useState } from "preact/hooks";

import { useFolderChanges } from "~/hooks/useFolderChanges";

const PAGE_SIZE = 50;

type State = {
    messages: Message[],
    next: MessageKey | null,
    exhausted: boolean,
    loading: boolean,
    error: string | null,
};

export type Messages = State & { loadMore: () => void };

const EMPTY: State = { messages: [], next: null, exhausted: false, loading: false, error: null };

function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}

export function useMessages(client: EasClient | null, folderId: string | null): Messages {
    const [state, setState] = useState<State>(EMPTY);

    const generation = useRef(0);
    const busy = useRef(false);
    const loaded = useRef(0);

    useEffect(() => {
        loaded.current = state.messages.length;
    }, [state.messages]);

    const load = useCallback((before: MessageKey | null, limit: number = PAGE_SIZE) => {
        if (client === null || folderId === null || busy.current) return;

        const mine = generation.current;
        busy.current = true;
        setState((prev) => ({ ...prev, loading: true, error: null }));

        client.listMessages({
            folderId,
            limit,
            ...(before === null ? {} : { before }),
        }).then((page) => {
            if (mine !== generation.current) return;
            busy.current = false;

            setState((prev) => ({
                messages: before === null ? page.items : [...prev.messages, ...page.items],
                next: page.next,
                exhausted: page.next === null,
                loading: false,
                error: null,
            }));
        }).catch((thrown: unknown) => {
            if (mine !== generation.current) return;
            busy.current = false;

            setState((prev) => ({ ...prev, loading: false, error: reason(thrown) }));
        });
    }, [client, folderId]);

    const reload = useCallback((clear: boolean) => {
        generation.current += 1;
        busy.current = false;

        if (clear) {
            loaded.current = 0;
            setState(EMPTY);
            load(null);
            return;
        }

        // a sync lands in rounds, so keep what's on screen and refetch as deep as the reader got
        setState((prev) => ({ ...prev, next: null, exhausted: false, error: null }));
        load(null, Math.max(PAGE_SIZE, loaded.current));
    }, [load]);

    const restart = useCallback(() => { reload(true); }, [reload]);

    useEffect(restart, [restart]);

    const refresh = useCallback(() => { reload(false); }, [reload]);
    useFolderChanges(client, [folderId], refresh);

    const loadMore = useCallback(() => {
        if (state.next === null || state.loading) return;
        load(state.next);
    }, [load, state.next, state.loading]);

    return { ...state, loadMore };
}
