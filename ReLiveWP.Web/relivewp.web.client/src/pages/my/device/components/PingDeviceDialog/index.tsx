import { useSignal } from "@preact/signals";
import { useCallback } from "preact/hooks";
import { Dialog } from "~/components/Dialog";
import { useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_PING_DEVICE, populateEndpoint } from "~/util/endpoints";

export default function PingDeviceDialog({ onClose, deviceId }: {
    onClose: () => void;
    deviceId: string;
}) {
    const fetch = useAuthenticatedFetch();
    const error = useSignal<string | null>(null);
    const pingDevice = useCallback(async () => {
        try {
            const resp = await fetch(populateEndpoint(ENDPOINT_PING_DEVICE, { deviceId: deviceId }), { method: "PUT" });
            if (!resp.ok) {
                throw new Error(`failed to ping device: ${resp.status} ${resp.statusText}`);
            }
            onClose();
        }
        catch (e) {
            error.value = (e as Error).message;
        }
    }, [deviceId]);

    return (
        <Dialog onClose={onClose}>
            <h1>ping this phone?</h1>
            <p>this will cause your device to ring at full volume for 60 seconds, ping this phone now?</p>

            {error.value && <p class="error">{error.value}</p>}

            <div class="buttons">
                <button onClick={() => pingDevice()}>ping</button>
                <button onClick={() => onClose()}>cancel</button>
            </div>
        </Dialog>
    );
}

