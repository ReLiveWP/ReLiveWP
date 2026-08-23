import { ago } from "@relivewp/ui";

import { connectionLabel, isGroupEnabled } from "../../state/linked-accounts";
import { useConnectionCapabilities } from "../../state/use-connection-capabilities";
import { Sync } from "../../state/use-sync";

import { CapabilityAction, CapabilityRowProps, CapabilityShell } from "./CapabilityRow";

export type SyncHook = (connectionId: string) => {
    status: Sync | undefined;
    busy: { value: boolean };
    error: { value: string | null };
    syncNow: () => void;
    setEnabled: (enabled: boolean) => void;
};

const lastRun = (status: Sync) => {
    const parts = [
        status.created && `${status.created} added`,
        status.updated && `${status.updated} updated`,
        status.deleted && `${status.deleted} removed`,
    ].filter(Boolean);

    return parts.length > 0 ? parts.join(", ") : "nothing changed";
};

const note = (status: Sync | undefined, error: string | null) => {
    if (error) return error;
    if (status?.running) return "syncing...";
    if (status?.queued) return "queued...";
    if (status?.last_failure) return `last try failed: ${status.last_failure}`;
    if (!status?.last_synced_at) return "not pulled in yet";

    return `${lastRun(status)}, ${ago(status.last_synced_at)}`;
};

// The connection and what it is doing share one row, because the capability is the sync. Nothing
// about sync is offered while the capability is off, and the backend refuses it too.
export const SyncRow = (props: CapabilityRowProps & { useSync: SyncHook }) => {
    const { account, typeInfo, group, useSync } = props;

    const { setEnabled: setCapability } = useConnectionCapabilities(account);
    const { status, busy, error, syncNow, setEnabled } = useSync(account.id);

    const connected = isGroupEnabled(account, group);
    const connectedTo = connectionLabel(typeInfo, group);

    if (!connected) {
        return (
            <CapabilityShell {...props}
                status={<div class="capability-disconnected">disconnected</div>}
                actions={[{ label: "enable", onClick: () => setCapability(group.caps, true) }]} />
        );
    }

    const syncing = status?.enabled ?? false;
    const running = (status?.running ?? false) || (status?.queued ?? false);

    const actions: CapabilityAction[] = [
        { label: "disable", onClick: () => setCapability(group.caps, false) },
        {
            label: syncing ? "disable sync" : "enable sync",
            onClick: () => setEnabled(!syncing),
            disabled: busy.value,
        },
        {
            label: running ?
                (syncing ? "syncing..." : "importing...") :
                (syncing ? "sync now" : "import now"),
            onClick: syncNow,
            disabled: busy.value || running
        },
    ];

    return (
        <CapabilityShell {...props}
            status={<div class="capability-connection">
                {syncing ? `syncing with ${connectedTo}` : `connected to ${connectedTo}`}
            </div>}
            notes={<div class="capability-caps">{note(status, error.value)}</div>}
            actions={actions} />
    );
};
