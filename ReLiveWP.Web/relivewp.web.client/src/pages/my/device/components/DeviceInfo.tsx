import { useEffect, useMemo } from "preact/hooks";
import "./device-info.scss";

import { useComputed, useSignal } from "@preact/signals";
import useDeviceImage from "~/util/device-image";
import { ExtendedDeviceInfo } from "~/util/device-types";
import useVersion from "~/util/version";
import PingDeviceDialog from "./PingDeviceDialog";
import { lazy } from "preact-iso";
import { Suspense } from "preact/compat";
import { useDeviceInfo } from "../data/context";

import Attention from "~/static/attention.png";

const DeviceMap = lazy(() => import("./DeviceMap"));

function DeviceInfoLeftPanel() {
    const info = useDeviceInfo();
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
                            <dd>{number} {canMask && <button class="text-button" onClick={() => numberMasked.value = !numberMasked.value}>{numberMasked.value ? "show" : "hide"}</button>}</dd>
                        </>
                    )}
                    {info.imei && (
                        <>
                            <dt>imei</dt>
                            <dd>{imei} <button class="text-button" onClick={() => imeiMasked.value = !imeiMasked.value}>{imeiMasked.value ? "show" : "hide"}</button></dd>
                        </>
                    )}
                </dl>
            </div>

        </>
    )
}

function DeviceInfoRightPanel() {
    const showPingDialog = useSignal(false);
    const info = useDeviceInfo();

    const lastSeen = useMemo(() => {
        if (!info.last_seen) return "unknown";
        const date = new Date(info.last_seen);
        return date.toLocaleString();
    }, [info.last_seen]);

    return (
        <div class="device-actions">
            <div class="map-container">
                {info.last_seen_latitude && info.last_seen_longitude ?
                    (
                        <Suspense fallback={<p>fetching device location...</p>}>
                            <DeviceMap latitude={info.last_seen_latitude} longitude={info.last_seen_longitude} deviceName={info.friendly_name} />
                        </Suspense>
                    ) :
                    (
                        <div class="device-map-placeholder">
                            <p><img src={Attention} /> no location data</p>
                        </div>
                    )
                }
            </div>

            {lastSeen && <p><small><strong>last seen at {lastSeen}</strong></small></p>}

            <div class="find">
                <h3>lost your phone?</h3>
                <p>that's okay, here's a few things you can try</p>
                <ul>
                    <li><button class="text-button icon" onClick={() => showPingDialog.value = true}>ring it</button></li>
                    <li><button class="text-button icon">lock it</button></li>
                    <li><button class="text-button icon">wipe it</button></li>
                </ul>
            </div>

            {
                showPingDialog.value && <PingDeviceDialog deviceId={info.id} onClose={() => showPingDialog.value = false} />
            }
        </div>
    )
}

export default function DeviceInfo() {
    return (
        <div class="device-info-page">
            <DeviceInfoLeftPanel />
            <DeviceInfoRightPanel />
        </div>
    )
}
