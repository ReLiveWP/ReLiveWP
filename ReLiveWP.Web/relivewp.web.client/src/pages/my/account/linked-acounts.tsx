import "./linked-accounts.scss"

import { useSignal } from "@preact/signals";
import { useCallback, useEffect } from "preact/hooks";

import { AccountType, AccountTypes, AvailableConnectedService, CapabilityGroups, Connections, LinkedAccountsContext, useOpenDialog } from "./state/linked-accounts";
import { ENDPOINT_AVAILABLE_LINKS, ENDPOINT_GET_LINKED_ACCOUNTS } from "~/util/endpoints";
import { useAuthenticatedFetch } from "~/state/app-state";
import CapabilityGroupSection from "./components/CapabilityGroupSection";
import { Dialogs } from "./components/Dialogs";

const allKnownCaps = CapabilityGroups.reduce((acc, g) => acc | g.caps, 0);

function OrphanedAccounts({ connections }: { connections: Connections }) {
    const openDialog = useOpenDialog();
    const orphaned: Array<{ type: AccountType; id: string; name: string; url: string }> = [];

    for (const [serviceKey, accounts] of Object.entries(connections.connections ?? {})) {
        const type = serviceKey as AccountType;
        for (const account of accounts ?? []) {
            if (((account.enabled_capabilities ?? 0) & allKnownCaps) === 0) {
                orphaned.push({ type, id: account.id, name: account.name, url: account.url });
            }
        }
    }

    if (orphaned.length === 0) return null;

    return (
        <>
            <h4>Other Connected Accounts</h4>
            <dl>
                {orphaned.map(({ type, id, name, url }) => {
                    const typeInfo = AccountTypes[type];
                    const Icon = typeInfo?.icon;
                    return (
                        <div key={id} class="orphaned-account">
                            <dt>{Icon && <Icon class="link-icon" />} {typeInfo?.name ?? type}</dt>
                            <dd>
                                <a href={url} target="_blank">{name}</a>
                                <span class="inactive-label">(no active features)</span>
                            </dd>
                            <dd>
                                <button onClick={() => openDialog({ dialog: 'link', service: type })}>configure</button>
                                <button onClick={() => openDialog({ dialog: 'unlink', service: type, id })}>unlink account</button>
                            </dd>
                        </div>
                    );
                })}
            </dl>
        </>
    );
}

export default function LinkedAccounts() {
    const fetch = useAuthenticatedFetch()
    const linkedAccounts = useSignal<Connections>(null! as Connections)
    const availableLinks = useSignal<AvailableConnectedService[]>([])

    const doRefresh = useCallback(async () => {
        const response = await fetch(ENDPOINT_GET_LINKED_ACCOUNTS, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) return;
        linkedAccounts.value = await response.json();
    }, [])

    useEffect(() => {
        doRefresh();

        fetch(ENDPOINT_AVAILABLE_LINKS, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        }).then(async (res) => {
            if (!res.ok) return;
            availableLinks.value = await res.json();
        });
    }, []);

    if (!linkedAccounts.value) {
        return <span>Fetching your accounts...</span>;
    }

    return (
        <LinkedAccountsContext.Provider value={{ linkedAccounts: linkedAccounts!, availableLinks, doRefresh }}>
            <Dialogs>
                <div class="linked-accounts">
                    {CapabilityGroups.map(group => {
                        const services = availableLinks.value.filter(s => (s.capabilities & group.caps) !== 0);
                        if (services.length === 0) return null;
                        return <CapabilityGroupSection key={group.name} group={group} services={services} />;
                    })}
                    {/* <OrphanedAccounts connections={linkedAccounts.value} /> */}
                </div>
            </Dialogs>
        </LinkedAccountsContext.Provider>
    );
}
