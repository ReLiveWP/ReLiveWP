import { AccountInfo, AccountType, AccountTypes, useLinkedAccounts, useOpenDialog } from "../state/linked-accounts";

import { AddAccountButton } from "./AddAnotherButton";

import Attention from "~/static/attention.png"

export const AccountLinkInfo = ({ type, accountInfo }: { type: AccountType, accountInfo: AccountInfo }) => {
    const openDialog = useOpenDialog();

    return (
        <>
            <dd><a href={accountInfo.url} target="_blank">{accountInfo.name}</a> {accountInfo.needs_relink && <img class="attention" alt={"Account requires attention!"} src={Attention} />}</dd>
            <dd>
                <button>options</button>
                {accountInfo.needs_relink && <button onClick={() => openDialog({ dialog: 'relink', id: accountInfo.id })}>fix account</button>}
                <button onClick={() => openDialog({ dialog: 'unlink', id: accountInfo.id, service: type })}>unlink account</button>
            </dd>
        </>
    );
}


export const AccountLinkEntry = ({ type }: { type: AccountType; }) => {
    const { linkedAccounts } = useLinkedAccounts();
    console.log(linkedAccounts.value);
    const { name, icon: Icon, allowsMany } = AccountTypes[type];
    const accountInfo = linkedAccounts.value.connections?.[type];
    const accounts = accountInfo
        ?.map(a => <AccountLinkInfo key={name + '_' + a.name} type={type} accountInfo={a} />);

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
