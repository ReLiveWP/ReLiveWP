import { Device } from "~/util/device-types";
import useVersion from "~/util/version";
import useDeviceImage from "~/util/device-image";
import Link from "~/components/Link";


export default function DeviceView({ device }: { device: Device }) {
    const [versionName, isSad, image] = useVersion(device.os_version);
    const deviceImage = useDeviceImage(device.manufacturer, device.model);

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
                    {/* {device.operator && <>
                        <dt>carrier</dt>
                        <dd>{device.operator}</dd>
                    </>}
                    {device.phone_number && (
                        <>
                            <dt>phone number</dt>
                            <dd>{number} {canMask && <button onClick={() => numberMasked.value = !numberMasked.value}>{numberMasked.value ? "show" : "hide"}</button>}</dd>
                        </>
                    )}
                    {device.imei && (
                        <>
                            <dt>imei</dt>
                            <dd>{imei} <button onClick={() => imeiMasked.value = !imeiMasked.value}>{imeiMasked.value ? "show" : "hide"}</button></dd>
                        </>
                    )} */}
                    <dt>
                        <Link href={`/my/device/${device.id}`} class="view-more text-accent">view more in my phone &gt;</Link>
                    </dt>
                </dl>
            </div>
        </>
    )
}