import { useAuthenticatedFetch } from "~/state/app-state";
import "./devices.scss"
import { useSignal } from "@preact/signals";
import { useCallback, useEffect } from "preact/hooks";
import { ENDPOINT_GET_DEVICES } from "~/util/endpoints";
import DeviceView from "./components/DeviceView";
import { Devices } from "~/util/device-types";


export default function Devices() {
    const fetch = useAuthenticatedFetch()
    const devices = useSignal<Devices>(null!)

    const doRefresh = useCallback(async () => {
        const response = await fetch(ENDPOINT_GET_DEVICES, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) return;
        devices.value = await response.json();
    }, [])

    useEffect(() => {
        doRefresh();
    }, []);

    return (
        <div class="devices">
            {!devices.value ?
                <span>Fetching your devices...</span> :
                (
                    <dl>
                        {devices.value
                            .map(device => <DeviceView device={device} />)}
                    </dl>
                )}
        </div>
    );
}