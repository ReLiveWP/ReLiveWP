import { useContext } from "preact/hooks";
import { AccountType, OpenDialogContext } from "../state/linked-accounts";

export const AddAccountButton = ({ text, service }: { text: string, service: AccountType }) => {
    const context = useContext(OpenDialogContext)

    return (
        <dd>
            <a href="#" onClick={() => context(service)}>{text}</a>
        </dd>
    );
}