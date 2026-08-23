import { Signal, effect } from "@preact/signals";
import {
    connectWorker,
    LIVE_ANNOTATIONS,
    type EasClient,
    type EngineState,
} from "@relivewp/eas-sync/host";
import { createContext } from "preact";
import { useContext } from "preact/hooks";

import { DEVICE_TYPE, loadDeviceId, requestPersistence, saveDeviceId } from "~/util/device";
import { ENDPOINT_EAS, SERVICE_TARGET_EAS } from "~/util/endpoints";
import type { AppState } from "./app-state";

export type SyncState = {
    engine: Signal<EngineState | null>,
    error: Signal<string | null>,
    client: Signal<EasClient | null>,
    autoSync: Signal<boolean>,
    synchronise: () => Promise<void>,
    setAutoSync: (enabled: boolean) => void,
}

const PROTOCOL_VERSION = "14.1";
const STALE_MS = 5 * 60_000;
const AUTO_SYNC_KEY = "relivewp.mail.autosync";

function readAutoSync(): boolean {
    try {
        return localStorage.getItem(AUTO_SYNC_KEY) !== "off";
    } catch {
        return true;
    }
}

function writeAutoSync(enabled: boolean): void {
    try {
        localStorage.setItem(AUTO_SYNC_KEY, enabled ? "on" : "off");
    } catch {
        // private windows and full quotas, not worth a dialog
    }
}

const SyncContext = createContext<SyncState>(null!);
const SyncProvider = SyncContext.Provider;

function useSync() {
    return useContext(SyncContext);
}

function createSyncState(appState: AppState): SyncState {
    const engine = new Signal<EngineState | null>(null);
    const error = new Signal<string | null>(null);
    const ready = new Signal<EasClient | null>(null);
    const autoSync = new Signal(readAutoSync());

    let client: EasClient | undefined;
    let configured: string | null = null;

    const open = (): EasClient => {
        if (client !== undefined) return client;

        const worker = new Worker(new URL("../eas-worker.ts", import.meta.url), { type: "module" });

        client = connectWorker(worker);
        client.on((event) => {
            if (event.kind === "state") engine.value = event.state;
        });

        return client;
    };

    const teardown = () => {
        configured = null;
        engine.value = null;
        error.value = null;
        ready.value = null;

        client?.close();
        client = undefined;
    };

    const configure = async (userId: string, token: string) => {
        const deviceId = loadDeviceId(userId);
        requestPersistence();

        try {
            const opened = open();
            const account = await opened.configure({
                userId,
                endpoint: ENDPOINT_EAS,
                deviceId,
                deviceType: DEVICE_TYPE,
                protocolVersion: PROTOCOL_VERSION,
                credentials: { kind: "bearer", token },
                // ReLive's exchange server will respond to these!
                annotations: LIVE_ANNOTATIONS,
            });

            saveDeviceId(userId, account.deviceId);
            if (!autoSync.value) await opened.setAutoSync(false);
            if (client === opened) ready.value = opened;
        } catch (thrown) {
            configured = null;
            error.value = thrown instanceof Error ? thrown.message : String(thrown);
        }
    };

    effect(() => {
        const token = appState.storedEasToken.value;
        const userId = appState.user.value?.id;

        if (token === null || userId === undefined) {
            if (configured !== null) teardown();
            return;
        }

        if (configured === userId) {
            void open().credentials({ kind: "bearer", token });
            return;
        }

        configured = userId;
        error.value = null;
        void configure(userId, token);
    });

    const synchronise = async (): Promise<void> => {
        error.value = null;

        try {
            const summary = await open().sync();
            if (!summary.ok && summary.reason !== null) error.value = summary.reason;
        } catch (thrown) {
            error.value = thrown instanceof Error ? thrown.message : String(thrown);
        }
    };

    const renewEasToken = (): Promise<string | null> =>
        appState.refresher.accessToken(SERVICE_TARGET_EAS).catch(() => null);

    const auto = async (): Promise<void> => {
        if (!appState.online.value) return;
        if (appState.easToken.value === null && await renewEasToken() === null) return;
        if (ready.value === null) return;

        void open().wake();
    };

    window.addEventListener("online", () => void auto());

    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState !== "visible") return;
        if (Date.now() - (engine.value?.lastSyncAt ?? 0) < STALE_MS) return;

        void auto();
    });

    const setAutoSync = (enabled: boolean): void => {
        autoSync.value = enabled;
        writeAutoSync(enabled);

        if (ready.value !== null) void open().setAutoSync(enabled);
    };

    return {
        engine,
        error,
        client: ready,
        autoSync,
        synchronise,
        setAutoSync,
    };
}

export { createSyncState, SyncProvider, useSync }
