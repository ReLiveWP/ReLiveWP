import { Fragment } from "preact";

import { AccountInfo, AccountType, AccountTypeEntry, AccountTypes, AvailableConnectedService, groupsFor, useOpenDialog } from "../../state/linked-accounts";

import { ActionLink } from "./ActionLink";
import { rowFor } from "./rows";

// the right edge carries account-level actions and nothing else, so they live up here rather than
// competing with the capability actions below
const AccountHeader = ({ account, type, typeInfo }: {
    account: AccountInfo;
    type: AccountType;
    typeInfo: AccountTypeEntry;
}) => {
    const openDialog = useOpenDialog();
    const Icon = typeInfo.icon;

    return (
        <dt class="header">
            <Icon class="link-icon" />
            <div>
                <h4><a href={account.url} target="_blank">{account.name}</a></h4>
                <p>{typeInfo.name}</p>
            </div>
            <div class="account-actions">
                {account.needs_relink && (
                    <ActionLink onClick={() => openDialog({ dialog: 'relink', service: type, id: account.id })}>
                        fix account
                    </ActionLink>
                )}
                <ActionLink class="error"
                    onClick={() => openDialog({ dialog: 'unlink', id: account.id, service: type })}>
                    unlink account
                </ActionLink>
            </div>
        </dt>
    );
};

export const AccountEntry = ({ service, account }: { service: AvailableConnectedService; account: AccountInfo }) => {
    const type = service.service as AccountType;
    const typeInfo = AccountTypes[type] as AccountTypeEntry | undefined;
    if (!typeInfo) return null;

    return (
        <>
            <AccountHeader account={account} type={type} typeInfo={typeInfo} />
            {groupsFor(service).map(group => {
                const Row = rowFor(group.caps);

                return (
                    <Fragment key={group.name}>
                        <Row account={account} service={service} type={type} typeInfo={typeInfo} group={group} />
                    </Fragment>
                );
            })}
        </>
    );
};
