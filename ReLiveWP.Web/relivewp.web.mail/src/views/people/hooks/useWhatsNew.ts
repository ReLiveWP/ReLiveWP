import { useCallback } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
import { useAppState } from "~/state/app-state";
import { fetchWhatsNewAsync, forgetSocial, type SocialEntry } from "../state/social";

export type WhatsNewState = {
    connected: boolean,
    entries: SocialEntry[],
    loading: boolean,
    error: string | null,
};

const NONE: SocialEntry[] = [];

// reads the network, not the local store, so nothing here listens to sync events: a folder
// drain would turn into a run on bluesky
export function useWhatsNew(): WhatsNewState & { refresh: () => void } {
    const fetcher = useAppState().authenticatedFetch.value;

    const { value, loading, error, reload } = useAsync(
        fetcher === undefined ? null : () => fetchWhatsNewAsync(fetcher),
        [fetcher],
        { keep: true });

    const refresh = useCallback(() => {
        forgetSocial();
        reload();
    }, [reload]);

    return { connected: value?.connected ?? true, entries: value?.entries ?? NONE, loading, error, refresh };
}
