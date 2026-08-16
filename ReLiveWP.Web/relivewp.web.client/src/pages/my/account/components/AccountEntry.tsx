import { useSignal } from "@preact/signals";
import { Fragment } from "preact";

import { AccountInfo, AccountType, AccountTypeEntry, AccountTypes, AvailableConnectedService, CapabilityGroup, CapabilityGroups, useLinkedAccounts, useOpenDialog } from "../state/linked-accounts";
import { ServiceCapNames, ServiceCaps } from "~/util/service-caps";
import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_IMPORT_CONTACTS, ENDPOINT_SYNC_STATUS, ENDPOINT_UPDATE_LINK } from "~/util/endpoints";

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
    const { user } = useAppState();
    const fetch = useAuthenticatedFetch();
    const busy = useSignal(false);
    const importStatus = useSignal<string | null>(null);

    const type = service.service as AccountType;
    const typeInfo = AccountTypes[type] as AccountTypeEntry | undefined;
    if (!typeInfo) return null;

    const Icon = typeInfo.icon;
    const groups = CapabilityGroups.filter(group => (service.capabilities & group.caps) !== 0);

    const patch = async (body: Record<string, number>) => {
        if (busy.value) return;
        busy.value = true;

        try {
            await fetch(`${ENDPOINT_UPDATE_LINK}?connectionId=${encodeURIComponent(account.id)}`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
        } finally {
            await doRefresh();
            busy.value = false;
        }
    };

    const toggleGroup = (group: CapabilityGroup, isActive: boolean) => {
        const enabled = account.enabled_capabilities ?? 0;
        return patch({ enabled_capabilities: (isActive ? enabled & ~group.caps : enabled | group.caps) >>> 0 });
    };

    const pollStatus = async (): Promise<boolean> => {
        const params = new URLSearchParams([["connectionId", account.id]]);
        const response = await fetch(`${ENDPOINT_SYNC_STATUS}?${params}`);
        if (!response.ok) return true;

        const { sources } = await response.json();
        if (!sources?.length) return true;

        const active = sources.filter((s: any) => s.running || s.queued);
        if (active.length > 0) {
            importStatus.value = active.some((s: any) => s.running) ? "syncing..." : "queued...";
            return false;
        }

        const total = (key: string) => sources.reduce((sum: number, s: any) => sum + (s[key] ?? 0), 0);
        const failed = sources.find((s: any) => s.last_failure);

        importStatus.value = failed
            ? `failed: ${failed.last_failure}`
            : `${total("created")} added, ${total("updated")} updated, ${total("deleted")} removed`
              + (total("skipped") ? `, ${total("skipped")} skipped` : "");

        return true;
    };

    const importContacts = async (keepInSync: boolean) => {
        if (busy.value) return;
        busy.value = true;
        importStatus.value = "queued...";

        try {
            const response = await fetch(ENDPOINT_IMPORT_CONTACTS, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    connection_id: account.id,
                    keep_in_sync: keepInSync,
                }),
            });

            if (!response.ok) {
                importStatus.value = await response.text() || "import failed";
                return;
            }

            for (let i = 0; i < 120; i++) {
                await new Promise(resolve => setTimeout(resolve, 2000));
                if (await pollStatus()) return;
            }

            importStatus.value = "still syncing, check back shortly";
        } catch (e) {
            importStatus.value = e instanceof Error ? e.message : "import failed";
        } finally {
            busy.value = false;
        }
    };

    const toggleShare = (shareable: number, isShared: boolean) => {
        const shared = account.shared_capabilities ?? 0;
        return patch({
            enabled_capabilities: account.enabled_capabilities ?? 0,
            shared_capabilities: (isShared ? shared & ~shareable : shared | shareable) >>> 0,
        });
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

                const shareable = ((service.shareable_capabilities ?? 0) & group.caps) >>> 0;
                const isShared = ((account.shared_capabilities ?? 0) & shareable) !== 0;
                const isPrivate = user.value?.visibility === "private";

                return (
                    <Fragment key={group.name}>
                        <dd class="capability">
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
                        {isActive && group.caps === ServiceCaps.contacts && (
                            <dd class="capability">
                                <span class="capability-name">contacts</span>
                                <span class="capability-detail">
                                    {importStatus.value
                                        ? <span class="capability-caps">{importStatus.value}</span>
                                        : <span class="capability-disconnected">copy them across, or keep taking changes from {connectedTo}</span>}
                                    <span class="capability-caps">nothing is ever sent back, and a contact you edit here stops following {connectedTo}</span>
                                </span>
                                <span class="capability-action">
                                    <a href="#" class="text-accent" onClick={(e) => {
                                        e.preventDefault();
                                        importContacts(false);
                                    }}>{busy.value ? "working..." : "import contacts >"}</a>
                                    {!busy.value && (
                                        <a href="#" class="text-accent" onClick={(e) => {
                                            e.preventDefault();
                                            importContacts(true);
                                        }}>keep in sync &gt;</a>
                                    )}
                                </span>
                            </dd>
                        )}
                        {isActive && shareable !== 0 && (
                            <dd class="capability capability-sharing">
                                <span class="capability-name">sharing</span>
                                <span class="capability-detail">
                                    {isShared
                                        ? <span class="capability-connection">people linked to you can see this</span>
                                        : <span class="capability-disconnected">only you can see this</span>}
                                    {isShared && isPrivate && (
                                        <span class="capability-caps">
                                            your profile is private, so nobody is linked to you yet. <a href="/my/account/privacy" class="text-accent">discovery settings</a>
                                        </span>
                                    )}
                                </span>
                                <a href="#" class="capability-action text-accent" onClick={(e) => {
                                    e.preventDefault();
                                    toggleShare(shareable, isShared);
                                }}>{isShared ? "stop sharing" : "share"} &gt;</a>
                            </dd>
                        )}
                    </Fragment>
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
