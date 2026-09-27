type Envelope<T> = { id: string, message: T };

const STORAGE_PREFIX = "broadcast:";

export function postBroadcast<T>(name: string, message: T): void {
    if (typeof BroadcastChannel !== "undefined") {
        const channel = new BroadcastChannel(name);
        channel.postMessage(message);
        channel.close();
        return;
    }

    const envelope: Envelope<T> = { id: `${Date.now()}.${Math.random()}`, message };
    localStorage.setItem(STORAGE_PREFIX + name, JSON.stringify(envelope));
}

export function subscribeBroadcast<T>(name: string, onMessage: (message: T) => void): () => void {
    if (typeof BroadcastChannel !== "undefined") {
        const channel = new BroadcastChannel(name);
        const onChannelMessage = (e: MessageEvent<T>) => onMessage(e.data);

        channel.addEventListener("message", onChannelMessage);
        return () => {
            channel.removeEventListener("message", onChannelMessage);
            channel.close();
        };
    }

    const key = STORAGE_PREFIX + name;
    const onStorage = (e: StorageEvent) => {
        if (e.key !== key || !e.newValue) return;
        onMessage((JSON.parse(e.newValue) as Envelope<T>).message);
    };

    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
}
