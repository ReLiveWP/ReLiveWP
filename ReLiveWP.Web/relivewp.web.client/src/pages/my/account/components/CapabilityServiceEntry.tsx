import { AccountInfo, AccountType, AccountTypeEntry, AccountTypes, AvailableConnectedService, CapabilityGroup, useLinkedAccounts, useOpenDialog } from "../state/linked-accounts";
import { ServiceCapNames } from "~/util/service-caps";

import Attention from "~/static/attention.png"

const CapBadges = ({ account, group }: { account: AccountInfo; group: CapabilityGroup }) => {
    const activeCaps = Object.entries(ServiceCapNames)
        .filter(([cap]) => {
            const c = Number(cap);
            return (c & group.caps) !== 0 && ((account.enabled_capabilities ?? 0) & c) !== 0;
        })
        .map(([, name]) => name);

    if (activeCaps.length === 0) return null;
    return (
        <span class="cap-badges">
            {activeCaps.map(cap => <span key={cap} class="cap-badge">{cap}</span>)}
        </span>
    );
};

const AccountEntry = ({ type, account, group }: { type: AccountType; account: AccountInfo; group: CapabilityGroup }) => {
    const openDialog = useOpenDialog();
    const isActive = ((account.enabled_capabilities ?? 0) & group.caps) !== 0;

    return (
        <>
            <dd class={isActive ? undefined : "inactive-account"}>
                <a href={account.url} target="_blank">{account.name}</a>
                {isActive && <CapBadges account={account} group={group} />}
                {account.needs_relink && <img class="attention" alt="Account requires attention!" src={Attention} />}
                {!isActive && <span class="inactive-label">(not configured for {group.name.toLowerCase()})</span>}
            </dd>
            <dd>
                <button onClick={() => openDialog({ dialog: 'link', service: type, existingConnectionId: account.id, currentEnabledCaps: account.enabled_capabilities ?? 0 })}>options</button>
                {account.needs_relink && <button onClick={() => openDialog({ dialog: 'relink', id: account.id })}>fix account</button>}
                <button onClick={() => openDialog({ dialog: 'unlink', id: account.id, service: type })}>unlink account</button>
            </dd>
        </>
    );
};

export const CapabilityServiceEntry = ({ service, group }: { service: AvailableConnectedService; group: CapabilityGroup }) => {
    const { linkedAccounts } = useLinkedAccounts();
    const openDialog = useOpenDialog();

    const type = service.service as AccountType;
    const typeInfo = AccountTypes[type] as AccountTypeEntry | undefined;
    if (!typeInfo) return null;

    const { allowsMany } = typeInfo;
    const name = typeInfo.capOptions?.[group.caps]?.name ?? typeInfo.name;
    const Icon = typeInfo.capOptions?.[group.caps]?.icon ?? typeInfo.icon;
    const allAccounts = linkedAccounts.value.connections?.[type] ?? [];
    const canLink = allAccounts.length === 0 || allowsMany;
    const linkText = allAccounts.length === 0 ? "link account" : "add another";

    return (
        <>
            <dt><Icon class="link-icon" /> {name}</dt>
            {allAccounts.map(account => (
                <AccountEntry key={account.id} type={type} account={account} group={group} />
            ))}
            {canLink && (
                <dd>
                    <a href="#" onClick={(e) => { e.preventDefault(); openDialog({ dialog: 'link', service: type, initialCaps: group.caps }); }}>
                        {linkText}
                    </a>
                </dd>
            )}
        </>
    );
};
