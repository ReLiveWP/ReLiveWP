import "./linked-accounts.scss"

import { Signal } from "@preact/signals";

import { AccountTypeGroups, AvailableConnectedService, Connections, LinkedAccountsContext } from "./state/linked-accounts";
import { ENDPOINT_AVAILABLE_LINKS, ENDPOINT_GET_LINKED_ACCOUNTS } from "~/util/endpoints";
import AccountTypeGroup from "./components/AccountTypeGroup";
import { Dialogs } from "./components/Dialogs";
import { useFetchSignal } from "~/util/use-fetch";

export default function LinkedAccounts() {
    const { data: linkedAccounts, refresh: doRefresh } = useFetchSignal<Connections>(ENDPOINT_GET_LINKED_ACCOUNTS);
    const { data: availableLinks } = useFetchSignal<AvailableConnectedService[]>(ENDPOINT_AVAILABLE_LINKS);

    if (!linkedAccounts.value) {
        return <span>Fetching your accounts...</span>;
    }

    return (
        <LinkedAccountsContext.Provider value={{ linkedAccounts: linkedAccounts as Signal<Connections>, availableLinks: availableLinks as Signal<AvailableConnectedService[]>, doRefresh }}>
            <Dialogs>
                <div class="linked-accounts">
                    {Object.entries(AccountTypeGroups)
                        .map(group => <AccountTypeGroup key={group[0]} group={group} />)}
                </div>
            </Dialogs>
        </LinkedAccountsContext.Provider>
    );
}
