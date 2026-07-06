import DeviceInfo from "./components/DeviceInfo";
import { DeviceInfoContext, useDeviceInfoState } from "./state/device-info";
import { useTitle } from "~/util/effects";

export default function DevicePage({ id }: { id?: string }) {
    const deviceInfoState = useDeviceInfoState(id);

    useTitle(`${deviceInfoState.data.value?.friendly_name} - my device`)

    return (
        <DeviceInfoContext.Provider value={deviceInfoState}>
            {!deviceInfoState.data.value ? (
                deviceInfoState.error.value ? <p class="error">{deviceInfoState.error.value}</p> : <p>fetching device info...</p>
            ) : (
                <DeviceInfo />
            )}
        </DeviceInfoContext.Provider>
    );
}
