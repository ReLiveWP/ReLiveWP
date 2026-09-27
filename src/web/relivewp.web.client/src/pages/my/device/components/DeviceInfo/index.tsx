import "./device-info.scss";

import DeviceInfoLeftPanel from "./DeviceInfoLeftPanel";
import DeviceInfoRightPanel from "./DeviceInfoRightPanel";

export default function DeviceInfo() {
    return (
        <div class="device-info-page">
            <DeviceInfoLeftPanel />
            <DeviceInfoRightPanel />
        </div>
    )
}
