import { useCallback } from "preact/hooks";
import { useSignal } from "@preact/signals";

import { Dialog } from "@relivewp/ui";
import { useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_DEVICE_REMOVE, populateEndpoint } from "~/util/endpoints";
import { Device } from "~/util/device-types";


export default function RemoveDeviceDialog({ device, onClose, onRemoved }: {
    device: Device;
    onClose: () => void;
    onRemoved: () => void;
}) {
    const fetch = useAuthenticatedFetch();

    const isEnabled = useSignal(true);
    const error = useSignal<string | null>(null);

    const removeDevice = useCallback(async () => {
        isEnabled.value = false;

        try {
            const response = await fetch(populateEndpoint(ENDPOINT_DEVICE_REMOVE, { deviceId: device.id }), {
                method: 'DELETE',
                headers: {
                    'Accept': 'application/json',
                }
            });

            if (!response.ok) {
                error.value = "Something went wrong, try again.";
                return;
            }

            onRemoved();
            onClose();
        }
        finally {
            isEnabled.value = true;
        }
    }, [device.id]);

    const className = !isEnabled.value ? ' disabled' : '';

    return (
        <Dialog class={'remove-device-dialog' + className} onClose={onClose}>
            <h1>removing device</h1>
            <p>are you sure you want to remove {device.friendly_name}? it will need to reconnect to use this account again.</p>

            {error.value && <p class="error">{error.value}</p>}

            <div class="buttons">
                <button onClick={() => removeDevice()}>yes</button>
                <button onClick={() => onClose()}>no</button>
            </div>
        </Dialog>
    );
}
