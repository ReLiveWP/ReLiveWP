import { useComputed, useSignal } from "@preact/signals";
import { lazy } from "preact-iso";
import { Suspense } from "preact/compat";

import { useDeviceInfo } from "../../state/device-info";
import PingDeviceDialog from "../PingDeviceDialog";
import Attention from "~/static/attention.png";

const DeviceMap = lazy(() => import("../DeviceMap"));

export default function DeviceInfoRightPanel() {
    const showPingDialog = useSignal(false);
    const { data, error, locateDevice } = useDeviceInfo();

    const lastSeen = useComputed(() => {
        if (!data.value.last_seen) return "unknown";
        const date = new Date(data.value.last_seen);
        return date.toLocaleString();
    });

    return (
        <div class="device-actions">
            <div class="map-container">
                {data.value.last_seen_latitude && data.value.last_seen_longitude ?
                    (
                        <Suspense fallback={<p>fetching device location...</p>}>
                            <DeviceMap latitude={data.value.last_seen_latitude}
                                longitude={data.value.last_seen_longitude}
                                deviceName={data.value.friendly_name}
                                model={data.value.model}
                                manufacturer={data.value.manufacturer} />
                        </Suspense>
                    ) :
                    (
                        <div class="device-map-placeholder">
                            <p><img src={Attention} /> no location data</p>
                        </div>
                    )
                }
            </div>

            <p class="last-seen-row">
                {lastSeen.value && <small><strong>last seen at {lastSeen}</strong></small>}

                <button class="text-button icon" onClick={locateDevice}>refresh</button>
            </p>

            {error.value && <p class="error">{error.value}</p>}

            <div class="find">
                <h3>lost your phone?</h3>
                <p>we're here to help, here's a few things you can try</p>
                <ul>
                    <li><button class="text-button icon" onClick={() => showPingDialog.value = true}>ring it</button></li>
                    <li><button class="text-button icon" disabled>lock it</button></li>
                    <li><button class="text-button icon" disabled>wipe it</button></li>
                </ul>
            </div>

            {
                showPingDialog.value && <PingDeviceDialog deviceId={data.value.id} onClose={() => showPingDialog.value = false} />
            }
        </div>
    )
}
