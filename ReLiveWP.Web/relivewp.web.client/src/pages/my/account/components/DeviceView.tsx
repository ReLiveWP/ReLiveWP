import { Device } from "~/util/device-types";
import useVersion from "~/util/version";
import useDeviceImage from "~/util/device-image";
import Link from "~/components/Link";


export default function DeviceView({ device }: { device: Device }) {
    const [versionName, isSad, image] = useVersion(device.os_version);
    const [deviceImage, _] = useDeviceImage(device.manufacturer, device.model);

    const deviceImage = useMemo(() => {
        if (device.model.includes("HD2") || device.model == "PD29100") {
            return HD2
        }

        return Placeholder;
    }, [device.model])

    const backgroundColor = device.colour_theme === 1 ? '#000' : '#fff';

    return (
        <>
            <h2>{device.friendly_name}</h2>
            <div class="device-view">
                <div class="image">
                    <img src={deviceImage} />
                </div>

                <dl class="info">
                    <dt>model</dt>
                    <dd>{device.manufacturer} {device.model}</dd>
                    <dt>operating system</dt>
                    <dd>{image && <img class="os-icon" src={image} alt="Windows Phone logo" />} Windows Phone {versionName} {isSad && <span>:(</span>} </dd>
                    <dd><small class="os-version">Version {device.os_version}</small></dd>
                    <dt>theme</dt>
                    <dd>
                        <span class="colour-icon" style={{ backgroundColor: device.accent_colour }} />
                        &nbsp;
                        <span class="colour-icon" style={{ backgroundColor }} />
                    </dd>
                    <dt>language</dt>
                    <dd>{device.locale}</dd>
                    <dt>timezone</dt>
                    <dd>{device.timezone}</dd>
                    <dt>
                        <Link href={`/my/device/${device.id}`} class="view-more text-accent">view more in my phone &gt;</Link>
                    </dt>
                </dl>
            </div>
        </>
    )
}