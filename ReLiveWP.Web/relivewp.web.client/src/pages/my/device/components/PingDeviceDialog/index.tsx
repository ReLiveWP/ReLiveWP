import { useSignal } from "@preact/signals";
import { useCallback } from "preact/hooks";
import { Dialog } from "@relivewp/ui";
import { useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_DEVICE_PING, populateEndpoint } from "~/util/endpoints";

export default function PingDeviceDialog({ onClose, deviceId }: {
    onClose: () => void;
    deviceId: string;
}) {
    const fetch = useAuthenticatedFetch();
    const error = useSignal<string | null>(null);
    const pingDevice = useCallback(async () => {
        try {
            const resp = await fetch(populateEndpoint(ENDPOINT_DEVICE_PING, { deviceId: deviceId }), { method: "PUT" });
            if (!resp.ok) {
                throw new Error(`failed to ring device: ${resp.status} ${resp.statusText}`);
            }
            onClose();
        }
        catch (e) {
            error.value = (e as Error).message;
        }
    }, [deviceId]);

    return (
        <Dialog onClose={onClose}>
            <h1>ring this phone?</h1>
            <p>this will cause your device to ring at full volume for 60 seconds, ring this phone now?</p>

            {error.value && <p class="error">{error.value}</p>}

            <div class="buttons">
                <button onClick={() => pingDevice()}>ring</button>
                <button onClick={() => onClose()}>cancel</button>
            </div>
        </Dialog>
    );
}

