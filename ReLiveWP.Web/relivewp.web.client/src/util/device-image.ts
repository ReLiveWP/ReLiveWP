
import Placeholder from "~/static/devices/RM-801.png"
import HD2 from "~/static/devices/HD2.png"

import { useMemo } from "preact/hooks";

export default function useDeviceImage(manufacturer: string, model: string) {
    return useMemo(() => {
        if (model.includes("HD2") || model == "PD29100") {
            return HD2
        }

        return Placeholder;
    }, [manufacturer, model])
}