import { Dialog } from "@relivewp/ui";
import { useSignal } from "@preact/signals";
import { useCallback, useState } from "preact/hooks";

import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_UPDATE_PROFILE } from "~/util/endpoints";

export default function EditNameDialog({ onClose }: { onClose: () => void }) {
    const fetch = useAuthenticatedFetch();
    const { user, refreshUser } = useAppState();

    const [firstName, setFirstName] = useState(user.value.first_name ?? "");
    const [lastName, setLastName] = useState(user.value.last_name ?? "");

    const busy = useSignal(false);
    const error = useSignal<string | null>(null);

    const save = useCallback(async () => {
        busy.value = true;
        error.value = null;

        try {
            const response = await fetch(ENDPOINT_UPDATE_PROFILE, {
                method: 'PATCH',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ first_name: firstName, last_name: lastName }),
            });

            if (!response.ok) {
                error.value = "something went wrong, try again.";
                return;
            }

            await refreshUser();
            onClose();
        }
        finally {
            busy.value = false;
        }
    }, [firstName, lastName]);

    return (
        <Dialog class={"edit-name-dialog" + (busy.value ? " disabled" : "")} onClose={onClose}>
            <h1>your name</h1>
            <p>this is what your phone shows on your me tile.</p>

            <label>
                first name
                <input type="text" value={firstName}
                    onInput={e => setFirstName((e.currentTarget as HTMLInputElement).value)} />
            </label>
            <label>
                last name
                <input type="text" value={lastName}
                    onInput={e => setLastName((e.currentTarget as HTMLInputElement).value)} />
            </label>

            {error.value && <p class="error">{error.value}</p>}

            <div class="buttons">
                <button onClick={() => save()} disabled={busy.value}>save</button>
                <button onClick={() => onClose()}>cancel</button>
            </div>
        </Dialog>
    );
}
