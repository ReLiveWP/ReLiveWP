import { createContext } from "preact";
import { useContext } from "preact/compat";
import { ExtendedDeviceInfo } from "~/util/device-types";

export const DeviceInfoContext = createContext<ExtendedDeviceInfo | null>(null);

export const useDeviceInfo = () => {
    const context = useContext(DeviceInfoContext);
    if (context === null) {
        throw new Error("useDeviceInfo must be used within a DeviceInfoContext.Provider");
    }
    return context;
}