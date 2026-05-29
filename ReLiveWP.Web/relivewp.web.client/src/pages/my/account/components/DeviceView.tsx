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
                        <dt>model</dt>
                        <dd>{device.manufacturer} {device.model}</dd>
                        <dt>os version</dt>
                        <dd>{device.os_version}</dd>
                        {device.operator && <>
                            <dt>carrier</dt>
                            <dd>{device.operator}</dd>
                        </>}
                        {device.phone_number && <>
                            <dt>phone number</dt>
                            <dd>{device.phone_number}</dd>
                        </>}
                    </dl>
                </div>
            </div>
        </>
    )
}