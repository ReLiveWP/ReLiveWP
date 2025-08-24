import "./linked-accounts.scss"

import { AccountType, AccountTypeGroups, Connections, LinkedAccountsContext, OpenDialogContext } from "./state/linked-accounts";
import { useSignal, useSignalEffect } from "@preact/signals";

import AccountTypeGroup from "./components/AccountTypeGroup";
import { ENDPOINT_GET_LINKED_ACCOUNTS } from "~/util/endpoints";
import LinkAccountDialog from "./components/LinkAccountDialog";
import { useAppState } from "~/state/app-state";
import { useState } from "preact/hooks";

export default function LinkedAccounts() {
    const { authenticatedFetch } = useAppState();
    const linkedAccounts = useSignal<Connections>()
    const [isOpen, setIsOpen] = useState<boolean>(false);
    const [service, setService] = useState<AccountType>()

    useSignalEffect(() => {
        (async () => {
            if (linkedAccounts.value)
                return;

            const _fetch = authenticatedFetch.value;
            const response = await _fetch(ENDPOINT_GET_LINKED_ACCOUNTS, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                }
            });

            if (!response.ok) {
                // todo: errored
                return;
            }

            linkedAccounts.value = await response.json();
        })();
    });


    const openDialog = (service: AccountType) => {
        setService(service);
        setIsOpen(true);
    }

    const onClose = () => {
        setIsOpen(false);
    }

    return (
        <OpenDialogContext.Provider value={openDialog}>
            {!!linkedAccounts.value ? (
                <LinkedAccountsContext.Provider value={{ linkedAccounts }}>
                    <LinkAccountDialog isShown={isOpen} onClose={onClose} service={service} />
                    <div class="linked-accounts">
                        {Object.entries(AccountTypeGroups)
                            .map(group => (<AccountTypeGroup key={group[0]} group={group} />))}
                    </div>
                </LinkedAccountsContext.Provider>
            ) : (
                <span>Fetching your accounts...</span>
            )}
        </OpenDialogContext.Provider>
    )
}