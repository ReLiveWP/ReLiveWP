import { useCallback } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
import { useAppState } from "~/state/app-state";
import { reason } from "~/util/reason";
import {
    fetchAlbumAsync,
    fetchContactAlbumsAsync,
    fetchOwnAlbumsAsync,
    forgetSocial,
    SocialError,
    type SocialAlbum,
    type SocialAlbumSummary,
} from "../state/social";

// "own" is the signed-in user, a string is a contact's cid, null parks the hook until one is known
export type AlbumScope = "own" | string;

export type AlbumsState = {
    albums: SocialAlbumSummary[],
    shared: boolean,
    loading: boolean,
    error: string | null,
};

export type AlbumState = {
    album: SocialAlbum | undefined,
    loading: boolean,
    error: string | null,
};

const NONE: SocialAlbumSummary[] = [];

const REASONS: Record<string, string> = {
    album_not_found: "that album isn't here any more",
    invalid_cid: "that contact couldn't be found",
};

function albumReason(thrown: unknown): string {
    if (thrown instanceof SocialError) return REASONS[thrown.code] ?? thrown.message;
    return reason(thrown);
}

function cidOf(scope: AlbumScope): string | null {
    return scope === "own" ? null : scope;
}

export function useAlbums(scope: AlbumScope | null): AlbumsState & { refresh: () => void } {
    const fetcher = useAppState().authenticatedFetch.value;

    const { value, loading, error, reload } = useAsync(
        fetcher === undefined || scope === null
            ? null
            : async () => {
                if (scope === "own") {
                    const own = await fetchOwnAlbumsAsync(fetcher);
                    return { albums: own.albums, shared: true };
                }

                const contact = await fetchContactAlbumsAsync(fetcher, scope);
                return { albums: contact.albums, shared: contact.shared };
            },
        [fetcher, scope],
        { describe: albumReason });

    const refresh = useCallback(() => {
        forgetSocial();
        reload();
    }, [reload]);

    return { albums: value?.albums ?? NONE, shared: value?.shared ?? false, loading, error, refresh };
}

export function useAlbum(scope: AlbumScope | null, resourceId: string | null): AlbumState & { refresh: () => void } {
    const fetcher = useAppState().authenticatedFetch.value;

    const { value, loading, error, reload } = useAsync(
        fetcher === undefined || scope === null || resourceId === null
            ? null
            : () => fetchAlbumAsync(fetcher, cidOf(scope), resourceId),
        [fetcher, scope, resourceId],
        { describe: albumReason });

    const refresh = useCallback(() => {
        forgetSocial();
        reload();
    }, [reload]);

    return { album: value, loading, error, refresh };
}
