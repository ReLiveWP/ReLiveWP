import { useContext } from "preact/hooks";
import { useAppState } from "../../../../state/app-state";
import { ENDPOINT_BEGIN_ACCOUNT_LINKING } from "../../../../util/endpoints";
import { AccountType, OpenDialogContext } from "../linked-acounts";

export const AddAnotherButton = ({ service }: { service: AccountType }) => {
    const context = useContext(OpenDialogContext)

    return (
        <dd>
            <a href="#" onClick={() => context(service)}>add another</a>
        </dd>
    );
}