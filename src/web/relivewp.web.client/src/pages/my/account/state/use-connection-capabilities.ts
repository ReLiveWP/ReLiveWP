import { useSignal } from "@preact/signals";

import { useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_UPDATE_LINK } from "~/util/endpoints";

import { AccountInfo, enabledCaps, sharedCaps, useLinkedAccounts } from "./linked-accounts";

const applyMask = (mask: number, caps: number, on: boolean) => (on ? mask | caps : mask & ~caps) >>> 0;

export function useConnectionCapabilities(account: AccountInfo) {
    const { doRefresh } = useLinkedAccounts();
    const fetch = useAuthenticatedFetch();
    const busy = useSignal(false);

    const patch = async (body: Record<string, number>) => {
        if (busy.value) return;
        busy.value = true;

        try {
            await fetch(`${ENDPOINT_UPDATE_LINK}?connectionId=${encodeURIComponent(account.id)}`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
        } finally {
            await doRefresh();
            busy.value = false;
        }
    };

    return {
        busy,
        setEnabled: (caps: number, on: boolean) =>
            patch({ enabled_capabilities: applyMask(enabledCaps(account), caps, on) }),
        setShared: (caps: number, on: boolean) =>
            patch({
                enabled_capabilities: enabledCaps(account),
                shared_capabilities: applyMask(sharedCaps(account), caps, on),
            }),
    };
}
