import { useComputed, useSignal } from "@preact/signals";
import { Device } from "../devices";

import Placeholder from "~/static/devices/RM-801.png"
import HD2 from "~/static/devices/HD2.png"
import { useMemo } from "preact/hooks";

import WP70 from "~/static/os/wp70.png"
import WP75 from "~/static/os/wp75.png"
import WP78 from "~/static/os/wp78.png"

type Version = [major: number, minor: number, build: number, revision: number];

const VERSION_MIN_7_8: Version = [7, 10, 8800, 0];
const VERSION_MIN_7_5: Version = [7, 10, 7510, 0];
const VERSION_MIN_7_0_RTM: Version = [7, 0, 7000, 0];
const VERSION_MIN_7_0_SERIES: Version = [7, 0, 6077, 0];

const versionGreaterThan = (
    [major_a, minor_a, build_a, revision_a]: Version,
    [major_b, minor_b, build_b, revision_b]: Version) => {
    return (
        major_a !== major_b ? major_a > major_b :
            minor_a !== minor_b ? minor_a > minor_b :
                build_a !== build_b ? build_a > build_b :
                    revision_a > revision_b
    );
}

export default function DeviceView({ device }: { device: Device }) {
    const canMask = device.phone_number !== "None";
    const numberMasked = useSignal(canMask);
    const number = useComputed(() => {
        if (numberMasked.value) {
            return "*".repeat(device.phone_number?.length ?? 10);
        }
        else {
            return device.phone_number;
        }
    })

    const imeiMasked = useSignal(true);
    const imei = useComputed(() => {
        if (imeiMasked.value) {
            return "*".repeat(device.imei?.length ?? 10);
        }
        else {
            return device.imei;
        }
    })

    const [versionName, isSad, image] = useMemo(() => {
        let version = device.os_version.split('.', 4).map(v => parseInt(v)) as Version;
        if (versionGreaterThan(version, VERSION_MIN_7_8)) {
            return ["7.8", true, WP78]
        }

        if (versionGreaterThan(version, VERSION_MIN_7_5)) {
            return ["7.5", false, WP75]
        }

        if (versionGreaterThan(version, VERSION_MIN_7_0_RTM)) {
            return ["7", false, WP70]
        }

        if (versionGreaterThan(version, VERSION_MIN_7_0_SERIES)) {
            return ["7 Series", false, WP70]
        }

        return ["Unknown", true, null];
    }, [device.os_version])

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
                    {device.operator && <>
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
                    )}
                </dl>
            </div>
        </>
    )
}