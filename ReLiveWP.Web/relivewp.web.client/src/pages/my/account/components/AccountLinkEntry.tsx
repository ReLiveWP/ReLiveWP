import { AccountInfo, AccountType, AccountTypes, LinkedAccountsContext } from "../state/linked-accounts";

import { AddAccountButton } from "./AddAnotherButton";
import { useContext } from "preact/hooks";

export const AccountLinkInfo = ({ accountInfo }: { accountInfo: AccountInfo }) => (
    <>
        <dd><a href={accountInfo.url} target="_blank">{accountInfo.name}</a></dd>
        <dd><button>options</button> <button>unlink account</button></dd>
    </>
);


export const AccountLinkEntry = ({ type }: { type: AccountType; }) => {
    const { linkedAccounts } = useContext(LinkedAccountsContext);
    const { name, icon: Icon, allowsMany } = AccountTypes[type];
    const accountInfo = linkedAccounts.value.connections[type];
    const accounts = accountInfo
        ?.map(a => <AccountLinkInfo key={name + '_' + a.name} accountInfo={a} />);

    return (
        <>
            <dt><Icon class="link-icon" /> {name}</dt>
            {accounts?.length ? (
                <>
                    {accounts}
                    {allowsMany ? (<AddAccountButton service={type} text="add another" />) : undefined}
                </>
            ) : (
                <AddAccountButton service={type} text="link account" />
            )}
        </>
    );
};
