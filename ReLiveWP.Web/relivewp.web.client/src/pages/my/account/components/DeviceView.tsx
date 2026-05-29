import { Device } from "../devices";

import Placeholder from "~/static/devices/RM-801.png"

export default function DeviceView({ device }: { device: Device }) {
    return (
        <>
            <h2>{device.friendly_name}</h2>
            <div class="device-view">
                <div class="image">
                    <img src={Placeholder} />
                </div>

                <div class="info">
                    <dl>
                        <dt>Model</dt>
                        <dd>{device.manufacturer} {device.model}</dd>
                        <dt>OS Version</dt>
                        <dd>{device.os_version}</dd>
                        {device.operator && <>
                            <dt>Carrier</dt>
                            <dd>{device.operator}</dd>
                        </>}
                        {device.phone_number && <>
                            <dt>Phone Number</dt>
                            <dd>{device.phone_number}</dd>
                        </>}
                    </dl>
                </div>
            </div>
        </>
    )
}