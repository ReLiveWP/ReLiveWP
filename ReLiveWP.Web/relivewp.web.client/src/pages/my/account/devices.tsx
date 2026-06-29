import "./devices.scss"
import { ENDPOINT_GET_DEVICES } from "~/util/endpoints";
import DeviceView from "./components/DeviceView";
import { Devices } from "~/util/device-types";
import { useFetchSignal } from "~/util/use-fetch";


export default function Devices() {
    const { data: devices } = useFetchSignal<Devices>(ENDPOINT_GET_DEVICES);

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