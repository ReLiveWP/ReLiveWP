import "./service-picker.scss";

import { AccountType, AccountTypeEntry, AccountTypes, AvailableConnectedService } from "../../state/linked-accounts";

export default function ServicePicker({ services, onSelect, class: className }: {
    services: AvailableConnectedService[];
    onSelect: (service: AccountType) => void;
    class?: string;
}) {
    return (
        <div class={className ? `service-picker ${className}` : "service-picker"}>
            {services.map(service => {
                const type = service.service as AccountType;
                const typeInfo = AccountTypes[type] as AccountTypeEntry | undefined;
                if (!typeInfo) return null;

                const Icon = typeInfo.icon;
                return (
                    <button key={type} type="button" class="service-tile" onClick={() => onSelect(type)}>
                        <Icon class="service-icon" />
                        <span class="service-name">{typeInfo.name}</span>
                    </button>
                );
            })}
        </div>
    );
}
