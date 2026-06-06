import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import { useAuthenticatedFetch } from "~/state/app-state";
import { ExtendedDeviceInfo } from "~/util/device-types";
import { ENDPOINT_GET_EXTENDED_DEVICE_INFO, populateEndpoint } from "~/util/endpoints";
import DeviceInfo from "../components/DeviceInfo";
import { DeviceInfoContext } from "../data/context";
import { useTitle } from "~/util/effects";



export default function DevicePage({ id }: { id?: string }) {
    const fetch = useAuthenticatedFetch();
    const deviceInfo = useSignal<ExtendedDeviceInfo | null>(null);
    const error = useSignal<string | null>(null);

    useTitle(`${deviceInfo.value?.friendly_name} - my device`)

    useEffect(() => {
        if (!id) return;

        const fetchDeviceInfo = async () => {
            const resp = await fetch(populateEndpoint(ENDPOINT_GET_EXTENDED_DEVICE_INFO, { deviceId: id }), {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });

            if (!resp.ok) {
                error.value = `failed to fetch device info: ${resp.status} ${resp.statusText}`;
                return;
            }

            const data = await resp.json();
            deviceInfo.value = data;
        };

        fetchDeviceInfo();
    }, [id]);

    return (
        <DeviceInfoContext.Provider value={deviceInfo.value}>
            {!deviceInfo.value ? (
                error.value ? <p class="error">{error.value}</p> : <p>fetching device info...</p>
            ) : (
                <DeviceInfo />
            )}
        </DeviceInfoContext.Provider>
    );
}