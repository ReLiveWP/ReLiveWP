import { ago } from "@relivewp/ui";

import { connectionLabel, isGroupEnabled } from "../../state/linked-accounts";
import { useConnectionCapabilities } from "../../state/use-connection-capabilities";
import { ContactSync, useContactSync } from "../../state/use-contact-sync";

import { CapabilityAction, CapabilityRowProps, CapabilityShell } from "./CapabilityRow";

const lastRun = (status: ContactSync) => {
    const parts = [
        status.created && `${status.created} added`,
        status.updated && `${status.updated} updated`,
        status.deleted && `${status.deleted} removed`,
    ].filter(Boolean);

    return parts.length > 0 ? parts.join(", ") : "nothing changed";
};

const note = (status: ContactSync | undefined, error: string | null) => {
    if (error) return error;
    if (status?.running) return "syncing...";
    if (status?.queued) return "queued...";
    if (status?.last_failure) return `last try failed: ${status.last_failure}`;
    if (!status?.last_synced_at) return "not pulled in yet";

    return `${lastRun(status)}, ${ago(status.last_synced_at)}`;
};

// People is contacts, so the connection and what it is doing share one row. Nothing about sync is
// offered while the capability is off, and the backend refuses it too.
export const ContactsRow = (props: CapabilityRowProps) => {
    const { account, typeInfo, group } = props;

    const { setEnabled: setCapability } = useConnectionCapabilities(account);
    const { status, busy, error, syncNow, setEnabled } = useContactSync(account.id);

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
