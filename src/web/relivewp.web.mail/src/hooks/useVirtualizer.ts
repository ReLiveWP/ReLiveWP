import {
    Virtualizer,
    elementScroll,
    observeElementOffset,
    observeElementRect,
    type PartialKeys,
    type VirtualizerOptions,
} from "@tanstack/virtual-core";
import { useLayoutEffect, useReducer, useState } from "preact/hooks";

export type VirtualizerHookOptions<TScroll extends Element, TItem extends Element> = PartialKeys<
    VirtualizerOptions<TScroll, TItem>,
    "observeElementRect" | "observeElementOffset" | "scrollToFn"
>;

export function useVirtualizer<TScroll extends Element, TItem extends Element>(
    options: VirtualizerHookOptions<TScroll, TItem>,
): Virtualizer<TScroll, TItem> {
    const rerender = useReducer((count: number) => count + 1, 0)[1];

    const resolved: VirtualizerOptions<TScroll, TItem> = {
        observeElementRect,
        observeElementOffset,
        scrollToFn: elementScroll,
        ...options,
        onChange: (instance, sync) => {
            rerender({});
            options.onChange?.(instance, sync);
        },
    };

    const [virtualizer] = useState(() => new Virtualizer(resolved));

    virtualizer.setOptions(resolved);

    useLayoutEffect(() => virtualizer._didMount(), []);
    useLayoutEffect(() => {
        virtualizer._willUpdate();
    });

    return virtualizer;
}
