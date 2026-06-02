import { useEffect } from "preact/hooks";
import "./device-info.scss";

import { useComputed, useSignal } from "@preact/signals";
import useDeviceImage from "~/util/device-image";
import { ExtendedDeviceInfo } from "~/util/device-types";
import useVersion from "~/util/version";
import PingDeviceDialog from "./PingDeviceDialog";

function DeviceInfoLeftPanel({ info }: { info: ExtendedDeviceInfo }) {
    const canMask = info.phone_number !== "None";
    const numberMasked = useSignal(canMask);
    const number = useComputed(() => {
        if (numberMasked.value) {
            return "*".repeat(info.phone_number?.length ?? 10);
        }
        else {
            return info.phone_number;
        }
    })

    const imeiMasked = useSignal(true);
    const imei = useComputed(() => {
        if (imeiMasked.value) {
            return "*".repeat(info.imei?.length ?? 10);
        }
        else {
            return info.imei;
        }
    })

    const [versionName, isSad, image] = useVersion(info.os_version);
    const deviceImage = useDeviceImage(info.manufacturer, info.model);
    const backgroundColor = info.colour_theme === 1 ? '#000' : '#fff';

    useEffect(() => {
        numberMasked.value = canMask;
        imeiMasked.value = true;
    }, [info.phone_number, info.imei]);

    return (
        <>
            <div class="device-info">
                <div class="image">
                    <img src={deviceImage} />
                </div>

                <dl class="info">
                    <h2>{info.friendly_name}</h2>
                    <dt>model</dt>
                    <dd>{info.manufacturer} {info.model}</dd>
                    <dt>operating system</dt>
                    <dd>{image && <img class="os-icon" src={image} alt="Windows Phone logo" />} Windows Phone {versionName} {isSad && <span>:(</span>} </dd>
                    <dd><small class="os-version">Version {info.os_version}</small></dd>
                    <dt>theme</dt>
                    <dd>
                        <span class="colour-icon" style={{ backgroundColor: info.accent_colour }} />
                        &nbsp;
                        <span class="colour-icon" style={{ backgroundColor }} />
                    </dd>
                    <dt>language</dt>
                    <dd>{info.locale}</dd>
                    <dt>timezone</dt>
                    <dd>{info.timezone}</dd>
                    {info.operator && <>
                        <dt>carrier</dt>
                        <dd>{info.operator}</dd>
                    </>}
                    {info.phone_number && (
                        <>
                            <dt>phone number</dt>
                            <dd>{number} {canMask && <button onClick={() => numberMasked.value = !numberMasked.value}>{numberMasked.value ? "show" : "hide"}</button>}</dd>
                        </>
                    )}
                    {info.imei && (
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

function DeviceInfoRightPanel({ info }: { info: ExtendedDeviceInfo }) {
    const showPingDialog = useSignal(false);
    return (
        <div class="device-actions">
            <h3>lost your phone?</h3>
            <p><button onClick={() => showPingDialog.value = true}>ping</button></p>

            {
                showPingDialog.value && <PingDeviceDialog deviceId={info.id} onClose={() => showPingDialog.value = false} />
            }
        </div>
    )
}

export default function DeviceInfo({ info }: { info: ExtendedDeviceInfo }) {
    return (
        <div class="device-info-page">
            <DeviceInfoLeftPanel info={info} />
            <DeviceInfoRightPanel info={info} />
        </div>
    )
}
