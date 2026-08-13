import { getLinkableServices, useLinkedAccounts, useOpenDialog } from "../../state/linked-accounts";
import ServicePicker from "./ServicePicker";

export default function LinkedAccountsEmptyState() {
    const { linkedAccounts, availableLinks } = useLinkedAccounts();
    const openDialog = useOpenDialog();

    const services = getLinkableServices(availableLinks.value, linkedAccounts.value.connections);

    return (
        <div class="linked-accounts-empty">
            <h4>TODO: no accounts linked yet heading</h4>
            <p>TODO: no accounts linked yet description</p>
            <ServicePicker services={services} onSelect={(service) => openDialog({ dialog: 'link', service })} />
        </div>
    );
}
