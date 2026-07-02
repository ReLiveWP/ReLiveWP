import { Signal, useSignal, useSignalEffect } from "@preact/signals";
import { createContext } from "preact";
import { useCallback, useContext, useEffect } from "preact/hooks";
import { fetchEventSource } from "@microsoft/fetch-event-source";

import { ExtendedDeviceInfo } from "~/util/device-types";
import { ENDPOINT_DEVICE_EVENTS, ENDPOINT_DEVICE_LOCATE, ENDPOINT_GET_EXTENDED_DEVICE_INFO, populateEndpoint } from "~/util/endpoints";
import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import { useFetchSignal } from "~/util/use-fetch";

type DeviceInfoContextType = {
    data: Signal<ExtendedDeviceInfo>;
    error: Signal<string | null>;
    locateDevice: () => Promise<void>;
};

export const DeviceInfoContext = createContext<DeviceInfoContextType>(null!);

export const useDeviceInfo = () => {
    const context = useContext(DeviceInfoContext);
    if (context === null) {
        throw new Error("useDeviceInfo must be used within a DeviceInfoContext.Provider");
    }
    return context;
}

export function useDeviceInfoState(id?: string) {
    const url = id ? populateEndpoint(ENDPOINT_GET_EXTENDED_DEVICE_INFO, { deviceId: id }) : "";

    const fetch = useAuthenticatedFetch();
    const { data, error } = useFetchSignal<ExtendedDeviceInfo>(url);

    const { token } = useAppState();

    const deviceInfo = useSignal(data.value);
    useSignalEffect(() => {
        deviceInfo.value = data.value;
    })

    useEffect(() => {
        if (!deviceInfo.value?.id) return;

        const url = populateEndpoint(ENDPOINT_DEVICE_EVENTS, { deviceId: deviceInfo.value.id });
        const abortController = new AbortController();

        fetchEventSource(url, {
            headers: { "Authorization": `Bearer ${token.value}` },
            signal: abortController.signal,
            openWhenHidden: true,
            onmessage: (event) => {
                const data = JSON.parse(event.data);

                if (data.action === "locate" && data.result === 0 && data.latitude != null && data.longitude != null) {
                    const { latitude, longitude, reported }: { latitude: number, longitude: number, reported: string } = data;
                    if (Date.parse(reported) > Date.parse(deviceInfo.value?.last_seen ?? "")) {

                        deviceInfo.value = {
                            ...deviceInfo.value,
                            last_seen_latitude: latitude,
                            last_seen_longitude: longitude,
                            last_seen: reported
                        } as ExtendedDeviceInfo;
                    }
                }
            }
        });

        return () => {
            abortController.abort();
        }
    }, [deviceInfo.value?.id])

    const locateDevice = useCallback(async () => {
        if (!deviceInfo.value?.id) return;

        try {
            const url = populateEndpoint(ENDPOINT_DEVICE_LOCATE, { deviceId: deviceInfo.value.id });
            const resp = await fetch(url, { method: "PUT" });
            if (!resp.ok) {
                throw new Error(`failed to locate device: ${resp.status} ${resp.statusText}`);
            }
        }
        catch (e) {
            error.value = (e as Error).message;
        }
    }, [deviceInfo.value?.id]);

    return {
        data: deviceInfo as Signal<ExtendedDeviceInfo>,
        error: error,
        locateDevice: locateDevice
    };
}
