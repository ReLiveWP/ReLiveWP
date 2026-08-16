import { useEffect, useState } from "preact/hooks";

import { useTheme, type AccentColor } from "./state.tsx";

export type ColourScheme = "light" | "dark";

function resolveColourScheme(): ColourScheme {
    const forced = document.documentElement.dataset.colourScheme;
    if (forced === "light" || forced === "dark")
        return forced;

    return matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function useColourScheme(): ColourScheme {
    const [scheme, setScheme] = useState(resolveColourScheme);

    useEffect(() => {
        const update = () => setScheme(resolveColourScheme());

        const query = matchMedia("(prefers-color-scheme: dark)");
        query.addEventListener("change", update);

        const observer = new MutationObserver(update);
        observer.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ["data-colour-scheme"],
        });

        update();

        return () => {
            query.removeEventListener("change", update);
            observer.disconnect();
        };
    }, []);

    return scheme;
}

export function useTitle(title: string) {
    const { titleSuffix } = useTheme();

    return useEffect(() => {
        document.title = title ? `${title} - ${titleSuffix}` : titleSuffix;
    }, [title, titleSuffix]);
}

export function useAccentColor(color: AccentColor) {
    const { accentStack } = useTheme();

    useEffect(() => {
        accentStack.value = [...accentStack.value, color];

        return () => {
            accentStack.value = accentStack.value.slice(0, -1);
        };
    }, [color]);
}
