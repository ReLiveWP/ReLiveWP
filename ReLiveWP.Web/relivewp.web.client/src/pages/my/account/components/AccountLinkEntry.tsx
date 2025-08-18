import { AccountInfo, AccountType, AccountTypes, linkInfo } from "../linked-acounts";
import { AddAnotherButton } from "./AddAnotherButton";


export const AccountLinkInfo = ({ accountInfo }: { accountInfo: AccountInfo }) => (
    <>
        <dd><a href={accountInfo.url} target="_blank">{accountInfo.name}</a></dd>
        <dd><button>options</button> <button>unlink account</button></dd>
    </>
);


export const AccountLinkEntry = ({ type }: { type: AccountType; }) => {
    const { name, icon: Icon, allowsMany } = AccountTypes[type];
    const accountInfo = linkInfo[type];
    const accounts = accountInfo
        ?.map(a => <AccountLinkInfo key={name + '_' + a.name} accountInfo={a} />);

    return (
        <>
            <dt><Icon class="link-icon" /> {name}</dt>
            {accounts?.length ? (
                <>
                    {accounts}
                    {allowsMany ? (<AddAnotherButton service={type} />) : undefined}
                </>
            ) : (
                <dd><a>link account</a></dd>
            )}
        </>
    );
};
