import { Signal, computed, useSignalEffect, type ReadonlySignal } from "@preact/signals";
import { createContext, type ComponentChildren } from "preact";
import { useContext, useMemo } from "preact/hooks";

export type AccentColor =
    | "red"
    | "purple"
    | "teal"
    | "pink"
    | "green"
    | "yellow"
    | "blue"
    | "magenta"
    | "zune";

export const DEFAULT_TITLE_SUFFIX = "relive for windows phone";

export type ThemeState = {
    accentStack: Signal<AccentColor[]>;
    accent: ReadonlySignal<AccentColor>;
    accentColor: ReadonlySignal<string>;
    titleSuffix: string;
};

const ThemeContext = createContext<ThemeState>(null!);

export function useTheme(): ThemeState {
    return useContext(ThemeContext);
}

function createThemeState(accent: AccentColor, titleSuffix: string): ThemeState {
    const accentStack = new Signal<AccentColor[]>([accent]);
    const current = computed(() => accentStack.value[accentStack.value.length - 1] ?? accent);

    return {
        accentStack,
        accent: current,
        accentColor: computed(() => {
            const name = current.value === "zune" ? "zune1" : current.value;
            return getComputedStyle(document.documentElement)
                .getPropertyValue(`--accent-colour-${name}`)
                .trim();
        }),
        titleSuffix,
    };
}

export function ThemeProvider({
    accent = "red",
    bodyClass,
    titleSuffix = DEFAULT_TITLE_SUFFIX,
    children,
}: {
    accent?: AccentColor;
    bodyClass?: string;
    titleSuffix?: string;
    children: ComponentChildren;
}) {
    const state = useMemo(() => createThemeState(accent, titleSuffix), [accent, titleSuffix]);

    useSignalEffect(() => {
        document.body.className = [bodyClass, `accent-${state.accent.value}`]
            .filter(Boolean)
            .join(" ");

        const themeTag = document.querySelector('meta[name="theme-color"]');
        if (themeTag !== null && state.accentColor.value)
            themeTag.setAttribute("content", state.accentColor.value);
    });

    return <ThemeContext.Provider value={state}>{children}</ThemeContext.Provider>;
}
