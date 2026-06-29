import { useCallback } from "preact/hooks";
import { type AccountType, useLinkedAccounts } from "../../state/linked-accounts";
import "../link-account-dialog.scss";

import { Dialog } from "~/components/Dialog";
import { useComputed, useSignal } from "@preact/signals";
import { useAuthenticatedFetch } from "~/state/app-state";

import { ENDPOINT_DELETE_CONNECTION } from "~/util/endpoints";


export default function UnlinkAccountDialog({ onClose, id, service: type }: {
    onClose: () => void;
    id: string;
    service: AccountType
}) {
    const fetch = useAuthenticatedFetch()
    const { linkedAccounts, doRefresh } = useLinkedAccounts();

    const isEnabled = useSignal(true);
    const error = useSignal<string | null>(null)

    const account = useComputed(() => linkedAccounts.value.connections[type]?.find(i => i.id === id));
    const unlinkAccount = useCallback(async () => {
        isEnabled.value = false;

        try {
            const params = new URLSearchParams([["connectionId", id]])
            const response = await fetch(ENDPOINT_DELETE_CONNECTION + '?' + params.toString(), {
                method: 'DELETE',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                }
            });

            if (!response.ok) {
                error.value = "Something went wrong, try again.";
                return;
            }

            doRefresh();
            onClose();
        }
        finally {
            isEnabled.value = true;
        }
    }, []);

    const className = !isEnabled.value ? ' disabled' : '';

    return (
        <Dialog class={'unlink-account-dialog' + className} onClose={onClose}>
            <h1>unlinking account</h1>
            <p>are you sure you want to unlink {account.value?.name}?</p>

            {error.value && <p class="error">{error.value}</p>}

            <div class="buttons">
                <button onClick={() => unlinkAccount()}>yes</button>
                <button onClick={() => onClose()}>no</button>
            </div>
        </Dialog>
    );
}
