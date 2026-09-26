import { useCallback, useEffect, useState } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
import { useAppState } from "~/state/app-state";
import { reason } from "~/util/reason";
import {
    bindIdentityAsync,
    fetchIdentitiesAsync,
    fetchProvidersAsync,
    SocialError,
    unbindIdentityAsync,
    type SocialIdentity,
    type SocialProvider,
} from "../state/social";

export type ContactIdentitiesState = {
    cid: string | null,
    identities: SocialIdentity[],
    providers: SocialProvider[],
    loading: boolean,
    error: string | null,
    busy: boolean,
    bindError: string | null,
};

const NONE: SocialIdentity[] = [];
const NO_PROVIDERS: SocialProvider[] = [];

const REASONS: Record<string, string> = {
    handle_not_found: "we couldn't find that handle",
    invalid_handle: "enter a handle",
    unknown_provider: "we don't recognize that provider",
    contact_not_found: "that contact couldn't be found, it may have been deleted",
};

function bindReason(thrown: unknown): string {
    if (thrown instanceof SocialError) return REASONS[thrown.code] ?? thrown.message;
    return reason(thrown);
}

export function useContactIdentities(serverId: string | null): ContactIdentitiesState & {
    bind: (provider: string, handle: string) => Promise<boolean>,
    unbind: (provider: string) => Promise<void>,
} {
    const fetcher = useAppState().authenticatedFetch.value;
    const [busy, setBusy] = useState(false);
    const [bindError, setBindError] = useState<string | null>(null);

    const listed = useAsync(
        fetcher === undefined || serverId === null ? null : () => fetchIdentitiesAsync(fetcher, serverId),
        [fetcher, serverId],
        { describe: bindReason });

    const providers = useAsync(
        fetcher === undefined ? null : () => fetchProvidersAsync(fetcher),
        [fetcher]);

    useEffect(() => {
        setBusy(false);
        setBindError(null);
    }, [fetcher, serverId]);

    const cid = listed.value?.cid ?? null;

    const bind = useCallback(async (provider: string, handle: string): Promise<boolean> => {
        if (fetcher === undefined || serverId === null) return false;

        setBusy(true);
        setBindError(null);

        try {
            const bound = await bindIdentityAsync(fetcher, serverId, provider, handle);
            listed.set((prev) => ({
                cid: bound.cid,
                identities: [
                    ...(prev?.identities ?? NONE).filter((identity) => identity.provider !== provider),
                    ...bound.identities,
                ],
            }));
            setBusy(false);
            return true;
        }
        catch (thrown: unknown) {
            setBusy(false);
            setBindError(bindReason(thrown));
            return false;
        }
    }, [fetcher, serverId, listed.set]);

    const unbind = useCallback(async (provider: string): Promise<void> => {
        if (fetcher === undefined || serverId === null || cid === null) return;

        setBusy(true);
        setBindError(null);

        try {
            await unbindIdentityAsync(fetcher, serverId, cid, provider);
            listed.set((prev) => (prev === undefined ? undefined : {
                ...prev,
                identities: prev.identities.filter((identity) => identity.provider !== provider),
            }));
            setBusy(false);
        }
        catch (thrown: unknown) {
            setBusy(false);
            setBindError(bindReason(thrown));
        }
    }, [fetcher, serverId, cid, listed.set]);

    return {
        cid,
        identities: listed.value?.identities ?? NONE,
        providers: providers.value?.providers ?? NO_PROVIDERS,
        loading: listed.loading,
        error: listed.error,
        busy,
        bindError,
        bind,
        unbind,
    };
}
