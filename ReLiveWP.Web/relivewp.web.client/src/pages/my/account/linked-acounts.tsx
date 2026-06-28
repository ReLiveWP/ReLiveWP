import "./linked-accounts.scss"

import { Signal, useSignal } from "@preact/signals";
import { useCallback, useEffect } from "preact/hooks";

import { AccountTypeGroups, AvailableConnectedService, Connections, LinkedAccountsContext } from "./state/linked-accounts";
import { ENDPOINT_AVAILABLE_LINKS, ENDPOINT_GET_LINKED_ACCOUNTS } from "~/util/endpoints";
import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import AccountTypeGroup from "./components/AccountTypeGroup";
import { Dialogs } from "./components/Dialogs";

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
                    {Object.entries(AccountTypeGroups)
                        .map(group => <AccountTypeGroup key={group[0]} group={group} />)}
                </div>
            </Dialogs>
        </LinkedAccountsContext.Provider>
    );
}
