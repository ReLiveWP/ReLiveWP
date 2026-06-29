import { ExtendedDeviceInfo } from "~/util/device-types";
import { ENDPOINT_GET_EXTENDED_DEVICE_INFO, populateEndpoint } from "~/util/endpoints";
import DeviceInfo from "../components/DeviceInfo";
import { DeviceInfoContext } from "../data/context";
import { useTitle } from "~/util/effects";
import { useFetchSignal } from "~/util/use-fetch";



export default function DevicePage({ id }: { id?: string }) {
    const url = id ? populateEndpoint(ENDPOINT_GET_EXTENDED_DEVICE_INFO, { deviceId: id }) : "";
    const { data: deviceInfo, error } = useFetchSignal<ExtendedDeviceInfo>(url);

    useTitle(`${deviceInfo.value?.friendly_name} - my device`)

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