import { ServiceCaps } from "~/util/service-caps";

import { useCalendarSync, useContactSync } from "../../state/use-sync";

import { CapabilityRow, CapabilityRowProps } from "./CapabilityRow";
import { SyncRow } from "./SyncRow";

type CapabilityRowComponent = (props: CapabilityRowProps) => ReturnType<typeof CapabilityRow>;

const overrides: Partial<Record<number, CapabilityRowComponent>> = {
    [ServiceCaps.contacts]: props => SyncRow({ ...props, useSync: useContactSync }),
    [ServiceCaps.calendar]: props => SyncRow({ ...props, useSync: useCalendarSync }),
};

export const rowFor = (caps: number) => overrides[caps] ?? CapabilityRow;
