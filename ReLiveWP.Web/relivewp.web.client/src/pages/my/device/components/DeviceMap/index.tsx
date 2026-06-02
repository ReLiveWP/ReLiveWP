import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import "./device-map.scss";

import Placeholder from "~/static/devices/RM-801.png"

type DeviceMapProps = {
    latitude: number;
    longitude: number;
    deviceName: string;
}

const DeviceIcon = L.icon({
    iconUrl: Placeholder,
    iconSize: [32, 32],
    iconAnchor: [16, 32],
    popupAnchor: [0, -32]
});

export default function DeviceMap({ latitude, longitude, deviceName }: DeviceMapProps) {
    return (
        <MapContainer center={[latitude, longitude]} zoom={14} scrollWheelZoom={false} style={{ height: "300px", width: "100%" }}>
            <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            <Marker position={[latitude, longitude]} icon={DeviceIcon}>
                <Popup>{deviceName}</Popup>
            </Marker>
        </MapContainer>
    )
}