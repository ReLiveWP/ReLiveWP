import { Device } from "~/util/device-types";
import useDeviceImage from "~/util/device-image";
import Link from "~/components/Link";
import DeviceSpecRows from "~/components/DeviceSpecRows";


export default function DeviceView({ device, onRemove }: { device: Device; onRemove: (id: string) => void }) {
    const [deviceImage, _] = useDeviceImage(device.manufacturer, device.model);

    return (
        <>
            <h2>{device.friendly_name}</h2>
            <div class="device-view">
                <div class="image">
                    <img src={deviceImage} />
                </div>

                <dl class="info">
                    <DeviceSpecRows
                        manufacturer={device.manufacturer}
                        model={device.model}
                        osVersion={device.os_version}
                        colourTheme={device.colour_theme}
                        accentColour={device.accent_colour}
                        locale={device.locale}
                        timezone={device.timezone} />
                    <dt>
                        <Link href={`/my/device/${device.id}`} class="view-more text-accent">view more in my phone &gt;</Link>
                    </dt>
                    <dt>
                        <a href="#" class="view-more error" onClick={() => onRemove(device.id)}>remove device</a>
                    </dt>
                </dl>
            </div>
        </>
    )
}