import "./linked-accounts.scss"

import { Signal } from "@preact/signals";
import { AccountType, AvailableConnectedService, Connections, LinkedAccountsContext } from "./state/linked-accounts";
import { ENDPOINT_AVAILABLE_LINKS, ENDPOINT_GET_LINKED_ACCOUNTS } from "~/util/endpoints";
import { Dialogs } from "./components/Dialogs";
import { useFetchSignal } from "~/util/use-fetch";

import { AccountEntry } from "./components/AccountEntry/AccountEntry";
import { CalendarSyncContext, ContactSyncContext, useCalendarSyncList, useContactSyncList } from "./state/use-sync";
import LinkAccountButton from "./components/ServicePicker/LinkAccountButton";
import LinkedAccountsEmptyState from "./components/ServicePicker/LinkedAccountsEmptyState";

export default function LinkedAccounts() {
    const { data: linkedAccounts, refresh: doRefresh } = useFetchSignal<Connections>(ENDPOINT_GET_LINKED_ACCOUNTS);
    const { data: availableLinks } = useFetchSignal<AvailableConnectedService[]>(ENDPOINT_AVAILABLE_LINKS);

    // one call covers every account on the page, so a row knows where it stands before it is touched
    const { data: contactSync } = useContactSyncList();
    const { data: calendarSync } = useCalendarSyncList();

    if (!linkedAccounts.value) {
        return <span>Fetching your accounts...</span>;
    }

    const connections = linkedAccounts.value.connections ?? {};
    const hasAnyConnection = Object.values(connections).some(accounts => (accounts?.length ?? 0) > 0);

    return (
        <LinkedAccountsContext.Provider value={{ linkedAccounts: linkedAccounts as Signal<Connections>, availableLinks: availableLinks as Signal<AvailableConnectedService[]>, doRefresh }}>
            <ContactSyncContext.Provider value={contactSync}>
              <CalendarSyncContext.Provider value={calendarSync}>
                <Dialogs>
                    <div class="linked-accounts">
                        {hasAnyConnection ? (
                            <>
                                <dl>
                                    {(availableLinks.value ?? []).flatMap(service =>
                                        (connections[service.service as AccountType] ?? []).map(account =>
                                            <AccountEntry key={account.id} service={service} account={account} />))}
                                </dl>
                                <LinkAccountButton />
                            </>
                        ) : (
                            <LinkedAccountsEmptyState />
                        )}
                    </div>
                </Dialogs>
              </CalendarSyncContext.Provider>
            </ContactSyncContext.Provider>
        </LinkedAccountsContext.Provider>
    );
}
