
import Placeholder from "~/static/devices/RM-801.png"
import PlaceholderSmall from "~/static/devices/RM-801-small.png"

import HD2 from "~/static/devices/HD2.png"
import HD2Small from "~/static/devices/HD2-small.png"

import RM803 from "~/static/devices/RM-803.png"
import RM803Small from "~/static/devices/RM-803-small.png"

import RM808 from "~/static/devices/RM-808.png"
import RM808Small from "~/static/devices/RM-808-small.png"

import RM835 from "~/static/devices/RM-835.png"
import RM835Small from "~/static/devices/RM-835-small.png"

import PI39110 from "~/static/devices/PI39110.png"
import PI39110Small from "~/static/devices/PI39110-small.png"

import PI06100 from "~/static/devices/PI06100.png"
import PI06100Small from "~/static/devices/PI06100-small.png"

import CETUS from "~/static/devices/CETUS.png"
import CETUSSmall from "~/static/devices/CETUS-small.png"

import { useMemo } from "preact/hooks";

export default function useDeviceImage(_manufacturer: string, _model: string) {
    return useMemo(() => {
        const manufacturer = _manufacturer.toUpperCase();
        const model = _model.toUpperCase();

        if (model.includes("HD2") || model == "PD29100") {
            return [HD2, HD2Small]
        }

        if (model == "PI39110") {
            return [PI39110, PI39110Small]
        }

        if (model == "PI06100") {
            return [PI06100, PI06100Small]
        }

        if (model.includes("CETUS")) {
            return [CETUS, CETUSSmall]
        }

        if (manufacturer === "NOKIA") {
            if (model.includes("803") || model.includes("809")) {
                return [RM803, RM803Small]
            }

            if (model.includes("808") || model.includes("823")) {
                return [RM808, RM808Small]
            }

            if (model.includes("835") || model.includes("836") || model.includes("849")) {
                return [RM835, RM835Small]
            }
        }

        return [Placeholder, PlaceholderSmall]
    }, [_manufacturer, _model])
}