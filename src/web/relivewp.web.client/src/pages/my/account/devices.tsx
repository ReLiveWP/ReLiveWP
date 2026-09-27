import "./devices.scss"
import { useSignal } from "@preact/signals";
import { ENDPOINT_GET_DEVICES } from "~/util/endpoints";
import DeviceView from "./components/DeviceView";
import RemoveDeviceDialog from "./components/RemoveDeviceDialog";
import { Devices } from "~/util/device-types";
import { useFetchSignal } from "~/util/use-fetch";


export default function Devices() {
    const { data: devices, refresh } = useFetchSignal<Devices>(ENDPOINT_GET_DEVICES);
    const pendingRemoval = useSignal<string | null>(null);

    const deviceToRemove = devices.value?.find(d => d.id === pendingRemoval.value);

    return (
        <div class="devices">
            {!devices.value ?
                <span>Fetching your devices...</span> :
                (
                    <dl>
                        {devices.value
                            .map(device => <DeviceView device={device} onRemove={id => pendingRemoval.value = id} />)}
                    </dl>
                )}

            {deviceToRemove && (
                <RemoveDeviceDialog
                    device={deviceToRemove}
                    onClose={() => pendingRemoval.value = null}
                    onRemoved={() => refresh()} />
            )}
        </div>
    );
}