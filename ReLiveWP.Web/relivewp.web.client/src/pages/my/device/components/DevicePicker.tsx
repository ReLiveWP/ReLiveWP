import "./device-picker.scss";

import { Signal } from "@preact/signals";
import { JSX } from "preact/jsx-runtime";
import useDeviceImage from "~/util/device-image";
import { Device, Devices } from "~/util/device-types";

const SelectedContent = 'selectedcontent' as unknown as keyof JSX.IntrinsicElements;

type DevicePickerProps = {
    deviceId: Signal<string | null>;
    devices: Devices
}

function DeviceOption({ device }: { device: Device }) {
    const deviceImage = useDeviceImage(device.manufacturer, device.model);

    return (
        <option value={device.id}>
            <div class="device-option">
                <div class="device-image"><img src={deviceImage} /></div>
                <span class="device-name">{device.friendly_name}</span>
                <span class="device-color" style={{ backgroundColor: device.accent_colour }}></span>
            </div>
        </option>
    )
}

export default function DevicePicker({ deviceId, devices }: DevicePickerProps) {
    return (
        <select class="device-picker" value={deviceId.value ?? undefined} onChange={e => deviceId.value = e.currentTarget.value}>
            <button>
                <SelectedContent></SelectedContent>
            </button>
            {devices.map(d => <DeviceOption key={d.id} device={d} />)}
        </select>
    )
}