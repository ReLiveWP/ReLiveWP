import { AvailableConnectedService, CapabilityGroup } from "../state/linked-accounts";
import { CapabilityServiceEntry } from "./CapabilityServiceEntry";

const CapabilityGroupSection = ({ group, services }: { group: CapabilityGroup; services: AvailableConnectedService[] }) => (
    <>
        <h4>{group.name}</h4>
        <dl>
            {services.map(service => (
                <CapabilityServiceEntry key={service.service} service={service} group={group} />
            ))}
        </dl>
    </>
);

export default CapabilityGroupSection;
