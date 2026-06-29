import { useMemo } from "preact/hooks";
import "./device-info.scss";

import { useSignal } from "@preact/signals";
import useDeviceImage from "~/util/device-image";
import { useMaskedValue } from "~/util/use-masked-value";
import DeviceSpecRows from "~/components/DeviceSpecRows";
import PingDeviceDialog from "./PingDeviceDialog";
import { lazy } from "preact-iso";
import { Suspense } from "preact/compat";
import { useDeviceInfo } from "../data/context";

import Attention from "~/static/attention.png";

const DeviceMap = lazy(() => import("./DeviceMap"));

function DeviceInfoLeftPanel() {
    const info = useDeviceInfo();
    const canMask = info.phone_number !== "None";
    const { display: number, masked: numberMasked, toggle: toggleNumber } = useMaskedValue(info.phone_number, canMask);
    const { display: imei, masked: imeiMasked, toggle: toggleImei } = useMaskedValue(info.imei, true);

    const [deviceImage, _] = useDeviceImage(info.manufacturer, info.model);

    return (
        <>
            <div class="device-info">
                <div class="image">
                    <img src={deviceImage} />
                </div>

                <dl class="info">
                    <h2>{info.friendly_name}</h2>
                    <DeviceSpecRows
                        manufacturer={info.manufacturer}
                        model={info.model}
                        osVersion={info.os_version}
                        colourTheme={info.colour_theme}
                        accentColour={info.accent_colour}
                        locale={info.locale}
                        timezone={info.timezone} />
                    {info.operator && <>
                        <dt>carrier</dt>
                        <dd>{info.operator}</dd>
                    </>}
                    {info.phone_number && (
                        <>
                            <dt>phone number</dt>
                            <dd><span class="text-monospace">{number}</span> {canMask && <button class="text-button" onClick={toggleNumber}>{numberMasked.value ? "show" : "hide"}</button>}</dd>
                        </>
                    )}
                    {info.imei && (
                        <>
                            <dt>imei</dt>
                            <dd><span class="text-monospace">{imei}</span> <button class="text-button" onClick={toggleImei}>{imeiMasked.value ? "show" : "hide"}</button></dd>
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
                            <DeviceMap latitude={info.last_seen_latitude}
                                longitude={info.last_seen_longitude}
                                deviceName={info.friendly_name}
                                model={info.model}
                                manufacturer={info.manufacturer} />
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
                <p>we're here to help, here's a few things you can try</p>
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
