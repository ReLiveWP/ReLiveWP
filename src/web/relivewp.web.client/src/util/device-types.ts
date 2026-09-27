export type Device = {
    id: string,
    friendly_name: string,
    manufacturer: string,
    model: string,
    operator: string | undefined,
    phone_number: string | undefined,
    imei: string | undefined;
    os_version: string,
    locale: string
    timezone: string
    accent_colour: string
    colour_theme: number
};

export type Devices = Device[];

export type ExtendedDeviceInfo = {
    id: string;
    manufacturer: string;
    model: string;
    operator: string;
    phone_number: string;
    os_version: string;
    friendly_name: string;
    colour_theme: number;
    accent_colour: string | null;
    locale: string;
    timezone: string;
    battery_level: number | null;
    storage_remaining: number | null;
    is_pin_locked: boolean | null;
    is_sim_locked: boolean | null;
    working_set: number | null;
    last_seen: string | null;
    last_seen_latitude: number | null;
    last_seen_longitude: number | null;
    imei: string | null;
}
