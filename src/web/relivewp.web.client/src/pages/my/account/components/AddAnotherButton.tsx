import { AccountType, useOpenDialog } from "../state/linked-accounts";

export const AddAccountButton = ({ text, service }: { text: string, service: AccountType }) => {
    const openDialog = useOpenDialog();

    return (
        <dd>
            <a href="#" onClick={() => openDialog({ dialog: 'link', service })}>{text}</a>
        </dd>
    );
}
