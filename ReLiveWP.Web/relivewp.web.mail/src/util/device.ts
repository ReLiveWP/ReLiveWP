const STORAGE_PREFIX = "relivewp.mail.device.";

export const DEVICE_TYPE = "Browser";

// DeviceId is capped at 32 alphanumeric characters on the wire
function mint(): string {
    return crypto.randomUUID().replaceAll("-", "").toUpperCase();
}

function key(userId: string): string {
    return `${STORAGE_PREFIX}${userId}`;
}

function usable(value: string | null): value is string {
    return value !== null && value.length % 2 === 0 && /^[0-9a-fA-F]+$/.test(value);
}

export function loadDeviceId(userId: string): string {
    let stored: string | null = null;
    try {
        stored = localStorage.getItem(key(userId));
    } catch {
        return mint();
    }

    if (usable(stored)) return stored;

    const minted = mint();
    saveDeviceId(userId, minted);
    return minted;
}

export function saveDeviceId(userId: string, deviceId: string): void {
    try {
        localStorage.setItem(key(userId), deviceId);
    } catch {
        // private windows and full quotas, not worth a dialog
    }
}

export function requestPersistence(): void {
    void navigator.storage?.persist?.().catch(() => undefined);
}
