import { useSignal } from "@preact/signals";

import { AccountInfo, AccountType, AccountTypeEntry, AccountTypes, AvailableConnectedService, CapabilityGroup, CapabilityGroups, useLinkedAccounts, useOpenDialog } from "../state/linked-accounts";
import { ServiceCapNames } from "~/util/service-caps";
import { useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_UPDATE_LINK } from "~/util/endpoints";

import Attention from "~/static/attention.png"

const stripGroupPrefix = (name: string) => {
    const separator = name.indexOf(" - ");
    return separator === -1 ? name : name.slice(separator + 3);
};

const activeCapNames = (account: AccountInfo, group: CapabilityGroup) =>
    Object.entries(ServiceCapNames)
        .filter(([cap]) => {
            const c = Number(cap);
            return (c & group.caps) !== 0 && ((account.enabled_capabilities ?? 0) & c) !== 0;
        })
        .map(([, name]) => stripGroupPrefix(name));

export const AccountEntry = ({ service, account }: { service: AvailableConnectedService; account: AccountInfo }) => {
    const openDialog = useOpenDialog();
    const { doRefresh } = useLinkedAccounts();
    const fetch = useAuthenticatedFetch();
    const busy = useSignal(false);

    const type = service.service as AccountType;
    const typeInfo = AccountTypes[type] as AccountTypeEntry | undefined;
    if (!typeInfo) return null;

    const Icon = typeInfo.icon;
    const groups = CapabilityGroups.filter(group => (service.capabilities & group.caps) !== 0);

    const toggleGroup = async (group: CapabilityGroup, isActive: boolean) => {
        if (busy.value) return;
        busy.value = true;

        const enabled = account.enabled_capabilities ?? 0;
        const next = (isActive ? enabled & ~group.caps : enabled | group.caps) >>> 0;

        try {
            await fetch(`${ENDPOINT_UPDATE_LINK}?connectionId=${encodeURIComponent(account.id)}`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled_capabilities: next }),
            });
        } finally {
            await doRefresh();
            busy.value = false;
        }
    };

    return (
        <>
            <dt class="header">
                <Icon class="link-icon" />
                <div>
                    <h4><a href={account.url} target="_blank">{account.name}</a></h4>
                    <p>{typeInfo.name}</p>
                </div>
                {/* {account.needs_relink && <img class="attention" alt="Account requires attention!" src={Attention} />} */}
            </dt>
            {groups.map(group => {
                const isActive = ((account.enabled_capabilities ?? 0) & group.caps) !== 0;
                const isMultiCap = (group.caps & (group.caps - 1)) !== 0;
                const connectedTo = typeInfo.capOptions?.[group.caps]?.name ?? typeInfo.name;
                const caps = isMultiCap ? activeCapNames(account, group) : [];

                return (
                    <dd key={group.name} class="capability">
                        <span class="capability-name">{group.name}</span>
                        <span class="capability-detail">
                            {isActive
                                ? <span class="capability-connection">connected to {connectedTo}</span>
                                : <span class="capability-disconnected">disconnected</span>}
                            {isActive && caps.length > 0 && <span class="capability-caps">{caps.join(", ")}</span>}
                        </span>
                        {isMultiCap ? (
                            <a href="#" class="capability-action text-accent" onClick={(e) => {
                                e.preventDefault();
                                openDialog({ dialog: 'link', service: type, existingConnectionId: account.id, currentEnabledCaps: account.enabled_capabilities ?? 0 });
                            }}>options &gt;</a>
                        ) : (
                            <a href="#" class="capability-action text-accent" onClick={(e) => {
                                e.preventDefault();
                                toggleGroup(group, isActive);
                            }}>{isActive ? "disable" : "enable"} &gt;</a>
                        )}
                    </dd>
                );
            })}
            {account.needs_relink && (
                <dd class="capability">
                    <a href="#" class="capability-action text-accent" onClick={(e) => {
                        e.preventDefault();
                        openDialog({ dialog: 'relink', service: type, id: account.id });
                    }}>fix account &gt;</a>
                </dd>
            )}
            <dd class="capability">
                <a href="#" class="capability-action error" onClick={(e) => {
                    e.preventDefault();
                    openDialog({ dialog: 'unlink', id: account.id, service: type });
                }}>unlink account &gt;</a>
            </dd>
        </>
    );
};
