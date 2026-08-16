import { ServiceCaps } from "~/util/service-caps";

import { CapabilityRow, CapabilityRowProps } from "./CapabilityRow";
import { ContactsRow } from "./ContactsRow";

type CapabilityRowComponent = (props: CapabilityRowProps) => ReturnType<typeof CapabilityRow>;

const overrides: Partial<Record<number, CapabilityRowComponent>> = {
    [ServiceCaps.contacts]: ContactsRow,
};

export const rowFor = (caps: number) => overrides[caps] ?? CapabilityRow;
