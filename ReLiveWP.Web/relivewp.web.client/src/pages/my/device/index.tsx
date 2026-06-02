import { useSignal } from "@preact/signals";
import "./index.scss"

import { ErrorBoundary, LocationProvider, Route, Router } from "preact-iso";
import { useCallback, useEffect } from "preact/compat";
import { useAuthenticatedFetch } from "~/state/app-state";

import { useAccentColor, useTitle } from "~/util/effects";
import { ENDPOINT_GET_DEVICES } from "~/util/endpoints";
import { Devices } from "~/util/device-types";
import DevicePicker from "./components/DevicePicker";
import DevicePage from "./pages/device";

export default function Device({ id }: { id?: string }) {
    useTitle("my device");
    useAccentColor('green');

    const fetch = useAuthenticatedFetch()
    const devices = useSignal<Devices>(null!)
    const selectedDevice = useSignal<string | null>(id || null)

    const doRefresh = useCallback(async () => {
        const response = await fetch(ENDPOINT_GET_DEVICES, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) return;
        devices.value = await response.json();

        if (selectedDevice.value === null || !devices.value.some(d => d.id === selectedDevice.value)) {
            selectedDevice.value = devices.value[0]?.id ?? null;
        }
    }, [])

    useEffect(() => {
        doRefresh();
    }, []);

    return (
        <LocationProvider>
            <div class="devices">
                {!devices.value ?
                    <p>fetching your devices...</p> :
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