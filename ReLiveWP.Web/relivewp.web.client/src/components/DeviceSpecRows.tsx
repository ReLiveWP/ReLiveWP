import useVersion from "~/util/version";

type DeviceSpecRowsProps = {
    manufacturer: string;
    model: string;
    osVersion: string;
    colourTheme: number;
    accentColour?: string | null;
    locale: string;
    timezone: string;
};

export default function DeviceSpecRows({ manufacturer, model, osVersion, colourTheme, accentColour, locale, timezone }: DeviceSpecRowsProps) {
    const [versionName, isSad, image] = useVersion(osVersion);
    const backgroundColor = colourTheme === 1 ? '#000' : '#fff';

    return (
        <>
            <dt>model</dt>
            <dd>{manufacturer} {model}</dd>
            <dt>operating system</dt>
            <dd>{image && <img class="os-icon" src={image} alt="Windows Phone logo" />} Windows Phone {versionName} {isSad && <span>:(</span>} </dd>
            <dd><small class="os-version">Version {osVersion}</small></dd>
            <dt>theme</dt>
            <dd>
                <span class="colour-icon" style={{ backgroundColor: accentColour }} />
                &nbsp;
                <span class="colour-icon" style={{ backgroundColor }} />
            </dd>
            <dt>language</dt>
            <dd>{locale}</dd>
            <dt>timezone</dt>
            <dd>{timezone}</dd>
        </>
    );
}
