import { AccountLinkEntry } from "./AccountLinkEntry";
import { AccountType } from "../state/linked-accounts";

const AccountTypeGroup = ({ group }: { group: [string, AccountType[]] }) => (
    <>
        <h4>{group[0]}</h4>
        <dl>
            {group[1].map(type => (<AccountLinkEntry key={type} type={type} />))}
        </dl>
    </>
);

export default AccountTypeGroup;