import { getLinkableServices, useLinkedAccounts, useOpenDialog } from "../../state/linked-accounts";
import ServicePicker from "./ServicePicker";

export default function LinkedAccountsEmptyState() {
    const { linkedAccounts, availableLinks } = useLinkedAccounts();
    const openDialog = useOpenDialog();

    const services = getLinkableServices(availableLinks.value, linkedAccounts.value.connections);

    return (
        <div class="linked-accounts-empty">
            <h4>start linking</h4>
            <p>You don't have any accounts linked! Click below to get started.</p>
            <ServicePicker services={services} onSelect={(service) => openDialog({ dialog: 'link', service })} />
        </div>
    );
}
