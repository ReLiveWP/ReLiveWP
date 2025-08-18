import { useEffect } from "preact/hooks";
import { AccentColor, useAppState } from "../state/app-state";

export function useTitle(title: string) {
    return useEffect(() => {
        document.title = !!title ? `${title} - relive for windows phone` : 'relive for windows phone'
    });
}

export function useAccentColor(color: AccentColor) {
    const { accentStack } = useAppState();
    useEffect(() => {
        accentStack.value = [...accentStack.value, color];

        return () => {
            accentStack.value = [...accentStack.value.slice(0, -1)];
        }
    })
}