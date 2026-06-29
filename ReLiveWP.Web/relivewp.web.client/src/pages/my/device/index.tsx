import { useSignal, useSignalEffect } from "@preact/signals";
import "./index.scss"

import { LocationProvider, useLocation } from "preact-iso";
import { useEffect } from "preact/compat";

import { useAccentColor, useTitle } from "~/util/effects";
import { ENDPOINT_GET_DEVICES } from "~/util/endpoints";
import { Devices } from "~/util/device-types";
import { useFetchSignal } from "~/util/use-fetch";
import DevicePicker from "./components/DevicePicker";
import DevicePage from "./pages/device";

export default function Device({ id }: { id?: string }) {
    useTitle("my device");
    useAccentColor('green');

    const location = useLocation();

    const selectedDevice = useSignal<string | null>(id || null)

    const { data: devices } = useFetchSignal<Devices>(ENDPOINT_GET_DEVICES, (list) => {
        if (selectedDevice.value === null || !list.some(d => d.id === selectedDevice.value)) {
            selectedDevice.value = list[0]?.id ?? null;
        }
    });

    useEffect(() => {
        if (id && id !== selectedDevice.value && devices.value?.some(d => d.id === id)) {
            selectedDevice.value = id;
        }
    }, [id])

    useSignalEffect(() => {
        var selected = selectedDevice.value;
        if (selected && selected !== id)
            location.route('/my/device/' + selected)
    })

    return (
        <LocationProvider>
            <div class="devices">
                {!devices.value ?
                    (<p>fetching your devices...</p>) :
                    (
                        <>
                            <div class="header">
                                <h1>my phone</h1>
                                <DevicePicker deviceId={selectedDevice} devices={devices.value} />
                            </div>
                            {selectedDevice.value && <DevicePage id={selectedDevice.value ?? undefined} />}
                        </>
                    )}
            </div>
        </LocationProvider >
    );
}