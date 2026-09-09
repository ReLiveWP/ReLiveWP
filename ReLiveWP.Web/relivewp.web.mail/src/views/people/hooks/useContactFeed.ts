import { useCallback } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
import { useAppState } from "~/state/app-state";
import { fetchContactFeedAsync, forgetSocial, type SocialEntry } from "../state/social";

export type ContactFeedState = {
    shared: boolean,
    entries: SocialEntry[],
    loading: boolean,
    error: string | null,
};

const NONE: SocialEntry[] = [];

export function useContactFeed(cid: string | null): ContactFeedState & { refresh: () => void } {
    const fetcher = useAppState().authenticatedFetch.value;

    const { value, loading, error, reload } = useAsync(
        fetcher === undefined || cid === null ? null : () => fetchContactFeedAsync(fetcher, cid),
        [fetcher, cid]);

    const refresh = useCallback(() => {
        forgetSocial();
        reload();
    }, [reload]);

    return { shared: value?.shared ?? false, entries: value?.entries ?? NONE, loading, error, refresh };
}
