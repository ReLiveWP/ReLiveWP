import { useMemo } from "preact/hooks";
import { populateEndpoint, ENDPOINT_DEVICE_IMAGE } from "~/util/endpoints";

export default function useDeviceImage(_manufacturer: string, model: string) {
    return useMemo(() => [
        populateEndpoint(ENDPOINT_DEVICE_IMAGE, { size: "large", device: model }),
        populateEndpoint(ENDPOINT_DEVICE_IMAGE, { size: "small", device: model }),
    ], [model]);
}
