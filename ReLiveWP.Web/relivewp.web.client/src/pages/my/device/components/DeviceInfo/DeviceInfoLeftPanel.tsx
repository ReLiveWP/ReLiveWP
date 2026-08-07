import useDeviceImage from "~/util/device-image";
import { useMaskedValue } from "~/util/use-masked-value";
import DeviceSpecRows from "~/components/DeviceSpecRows";
import { useDeviceInfo } from "../../state/device-info";

export default function DeviceInfoLeftPanel() {
    const { data: info } = useDeviceInfo();
    const canMask = info.value.phone_number !== "None";
    const { display: number, masked: numberMasked, toggle: toggleNumber } = useMaskedValue(info.value.phone_number, canMask);
    const { display: imei, masked: imeiMasked, toggle: toggleImei } = useMaskedValue(info.value.imei, true);

    const [deviceImage, _] = useDeviceImage(info.value.manufacturer, info.value.model);

    return (
        <>
            <div class="device-info">
                <div class="image">
                    <img src={deviceImage} />
                </div>

                <dl class="info">
                    <h2>{info.value.friendly_name}</h2>
                    <DeviceSpecRows
                        manufacturer={info.value.manufacturer}
                        model={info.value.model}
                        osVersion={info.value.os_version}
                        colourTheme={info.value.colour_theme}
                        accentColour={info.value.accent_colour}
                        locale={info.value.locale}
                        timezone={info.value.timezone} />
                    {info.value.operator && <>
                        <dt>carrier</dt>
                        <dd>{info.value.operator}</dd>
                    </>}
                    {info.value.phone_number && (
                        <>
                            <dt>phone number</dt>
                            <dd><span class="text-monospace">{number}</span> {canMask && <button class="text-button" onClick={toggleNumber}>{numberMasked.value ? "show" : "hide"}</button>}</dd>
                        </>
                    )}
                    {info.value.imei && (
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
