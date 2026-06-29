import { createContext } from "preact";
import { useContext } from "preact/hooks";
import { Signal } from "@preact/signals";

import { type Stage } from "./service-config";

export type LinkAccountContextValue = {
    handle: Signal<string>;
    redirectUrl: Signal<string>;
    stage: Signal<Stage>;
    error: Signal<string | null>;
    connectionId: Signal<string>;
    service: string;
    id?: string;
    initialCaps?: number;
    currentEnabledCaps?: number;
    onClose: () => void;
};

export const LinkAccountContext = createContext<LinkAccountContextValue | null>(null);

export function useLinkAccount(): LinkAccountContextValue {
    const ctx = useContext(LinkAccountContext);
    if (!ctx) throw new Error("useLinkAccount must be used within LinkAccountDialog");
    return ctx;
}
