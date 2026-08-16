import { ComponentChildren } from "preact";

import { useAppState } from "~/state/app-state";

import { AccountInfo, AccountType, AccountTypeEntry, AvailableConnectedService, CapabilityGroup, connectionLabel, enabledCapNames, isGroupEnabled, isMultiCapGroup, sharedCaps, shareableCaps, useOpenDialog } from "../../state/linked-accounts";
import { useConnectionCapabilities } from "../../state/use-connection-capabilities";

export type CapabilityRowProps = {
    account: AccountInfo;
    service: AvailableConnectedService;
    type: AccountType;
    typeInfo: AccountTypeEntry;
    group: CapabilityGroup;
};

export type CapabilityAction = {
    label: string;
    onClick: () => void;
    disabled?: boolean;
};

// The right edge belongs to the account, so a capability's actions sit under the status they change.
// Sharing is one of them wherever the group offers it, rather than a row of its own.
export const CapabilityShell = ({ account, service, group, status, notes, actions }: {
    account: AccountInfo;
    service: AvailableConnectedService;
    group: CapabilityGroup;
    status: ComponentChildren;
    notes?: ComponentChildren;
    actions: CapabilityAction[];
}) => {
    const { user } = useAppState();
    const { setShared } = useConnectionCapabilities(account);

    const shareable = shareableCaps(service, group);
    const sharing = isGroupEnabled(account, group) && shareable !== 0;
    const shared = (sharedCaps(account) & shareable) !== 0;

    const all = sharing
        ? [...actions, { label: shared ? "stop sharing" : "share", onClick: () => setShared(shareable, !shared) }]
        : actions;

    return (
        <dd class="capability">
            <span class="capability-name">{group.name}</span>
            <div class="capability-detail">
                {status}
                {notes}
                {sharing && (
                    <div class="capability-sharing">
                        {shared ? "people linked to you can see this" : "only you can see this"}
                        {shared && user.value?.visibility === "private" && (
                            <> your profile is private, so nobody is linked to you yet.{" "}
                                <a href="/my/account/privacy" class="text-accent">discovery settings</a></>
                        )}
                    </div>
                )}
                <div class="capability-actions">
                    {all.map(action => (
                        <button key={action.label} type="button" class="text-button icon"
                            disabled={action.disabled} onClick={action.onClick}>
                            {action.label}
                        </button>
                    ))}
                </div>
            </div>
        </dd>
    );
};

export const CapabilityRow = (props: CapabilityRowProps) => {
    const { account, type, typeInfo, group } = props;

    const openDialog = useOpenDialog();
    const { setEnabled } = useConnectionCapabilities(account);

    const isEnabled = isGroupEnabled(account, group);
    const caps = isMultiCapGroup(group) ? enabledCapNames(account, group) : [];

    const showOptions = () => openDialog({
        dialog: 'link',
        service: type,
        existingConnectionId: account.id,
        currentEnabledCaps: account.enabled_capabilities ?? 0,
    });

    return (
        <CapabilityShell {...props}
            status={isEnabled
                ? <div class="capability-connection">connected to {connectionLabel(typeInfo, group)}</div>
                : <div class="capability-disconnected">disconnected</div>}
            notes={isEnabled && caps.length > 0 && <div class="capability-caps">{caps.join(", ")}</div>}
            actions={isMultiCapGroup(group)
                ? [{ label: "options", onClick: showOptions }]
                : [{
                    label: isEnabled ? "disable" : "enable",
                    onClick: () => setEnabled(group.caps, !isEnabled),
                }]} />
    );
};
