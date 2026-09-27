import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import "./device-map.scss";

import { useMemo } from "preact/hooks";
import useDeviceImage from "~/util/device-image";
// import { useColourScheme } from "@relivewp/ui";

// const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>';
// const TILE_URL = {
//     light: "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png",
//     dark: "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
// };

type DeviceMapProps = {
    latitude: number;
    longitude: number;
    deviceName: string;
    model: string;
    manufacturer: string;
}

export default function DeviceMap({ latitude, longitude, deviceName, model, manufacturer }: DeviceMapProps) {
    const [_, iconSmall] = useDeviceImage(manufacturer, model);
    const icon = useMemo(() => {
        return L.icon({
            iconUrl: iconSmall,
            iconSize: [32, 32],
            iconAnchor: [16, 32],
            popupAnchor: [0, -32]
        })
    }, [iconSmall])

    // const scheme = useColourScheme();

    return (
        <MapContainer center={[latitude, longitude]} zoom={14} scrollWheelZoom={false} style={{ height: "300px", width: "100%" }}>
            <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            <Marker position={[latitude, longitude]} icon={icon}>
                <Popup>{deviceName}</Popup>
            </Marker>
        </MapContainer>
    )
}