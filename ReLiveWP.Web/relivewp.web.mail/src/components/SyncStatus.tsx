import "./sync-status.scss";

import type { EngineState } from "@relivewp/eas-sync/host";

import { useAppState } from "~/state/app-state";
import { useSync } from "~/state/sync";
import { ago } from "@relivewp/ui";

function since(engine: EngineState | null): string {
    return engine === null || engine.lastSyncAt === null ? "" : `, synced ${ago(engine.lastSyncAt)}`;
}

function ahead(at: number): string {
    const remaining = at - Date.now();

    for (const [unit, size] of UNITS)
        if (remaining >= size) return RELATIVE.format(Math.round(remaining / size), unit);

    return "shortly";
}

function synced(engine: EngineState): string {
    return engine.lastSyncAt === null ? "not synced" : `synced ${ago(engine.lastSyncAt)}`;
}

function label(
    engine: EngineState | null,
    authenticated: boolean,
    identified: boolean,
    online: boolean): string {
    if (!identified) return "not signed in";
    if (!online) return `offline${since(engine)}`;
    if (!authenticated) return `session expired${since(engine)}`;

    if (engine === null) return "connecting…";

    switch (engine.status) {
        case "configuring":
            return "connecting…";
        case "syncing":
            return "syncing…";
        case "error":
            return "sync failed";
        case "backoff":
            return engine.nextAttemptAt === null
                ? "sync failed"
                : `retrying ${ahead(engine.nextAttemptAt)}`;
        case "paused":
        case "waiting":
        case "idle":
            return synced(engine);
    }
}

function detailOf(engine: EngineState | null): string | null {
    if (engine === null) return null;
    if (engine.status === "waiting") return "listening for changes";
    if (engine.status === "backoff" && engine.nextAttemptAt !== null)
        return `${engine.lastError ?? "sync failed"}, retrying ${ahead(engine.nextAttemptAt)}`;

    return engine.lastError;
}

const IDLE: EngineState["status"][] = ["idle", "error", "waiting", "backoff", "paused"];

export default function SyncStatus() {
    const { isAuthenticated, hasIdentity, online } = useAppState();
    const { engine, error, autoSync, synchronise, setAutoSync } = useSync();

    const state = engine.value;
    const connected = online.value;
    const authenticated = isAuthenticated.value;

    const ready = state !== null && IDLE.includes(state.status);
    const detail = !connected
        ? "no network connection"
        : !authenticated && hasIdentity.value
            ? "sign in again to resume syncing"
            : error.value ?? detailOf(state);

    return (
        <div class="sync-status">
            <button
                type="button"
                class="sync-status-label"
                disabled={!ready || !connected || !authenticated}
                title={detail ?? undefined}
                onClick={() => void synchronise()}
            >
                {label(state, authenticated, hasIdentity.value, connected)}
            </button>

            <button
                type="button"
                class="sync-status-auto"
                aria-pressed={autoSync.value}
                title={autoSync.value
                    ? "checking for new mail as it arrives"
                    : "only syncing when you ask"}
                onClick={() => { setAutoSync(!autoSync.value); }}
            >
                {autoSync.value ? "auto" : "manual"}
            </button>
        </div>
    );
}
