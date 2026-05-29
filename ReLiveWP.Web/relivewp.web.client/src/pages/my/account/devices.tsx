import { useAuthenticatedFetch } from "~/state/app-state";
import "./devices.scss"
import { useSignal } from "@preact/signals";
import { useCallback, useEffect } from "preact/hooks";
import { ENDPOINT_GET_DEVICES } from "~/util/endpoints";
import DeviceView from "./components/DeviceView";

export type Device = {
    friendly_name: string,
    manufacturer: string,
    model: string,
    operator: string | undefined,
    phone_number: string | undefined,
    os_version: string,
    locale: string
};

type Devices = Device[];

export default function Devices() {
    const fetch = useAuthenticatedFetch()
    const devices = useSignal<Devices>([])

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

    if (!devices.value) {
        return <span>Fetching your accounts...</span>;
    }

    return (
        <div class="devices">
            <h4>Connected devices</h4>
            <dl>
                {devices.value
                    .map(device => <DeviceView device={device} />)}
            </dl>
        </div>
    );
}