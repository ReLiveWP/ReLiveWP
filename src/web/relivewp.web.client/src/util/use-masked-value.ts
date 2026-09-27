import { useComputed, useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";

export function useMaskedValue(value: string | null | undefined, initiallyMasked: boolean) {
    const masked = useSignal(initiallyMasked);

    useEffect(() => {
        masked.value = initiallyMasked;
    }, [value, initiallyMasked]);

    const display = useComputed(() => masked.value ? "*".repeat(value?.length ?? 10) : value ?? "");

    return { masked, display, toggle: () => { masked.value = !masked.value; } };
}
