import { Signal } from "@preact/signals";
import { createContext } from "preact";
import { useContext, useEffect } from "preact/hooks";

export type SearchRequest = {
    placeholder?: string,
    onSubmit?: (value: string) => void,
}

export type ShellState = {
    search: Signal<SearchRequest | null>,
    query: Signal<string>,
}

const ShellContext = createContext<ShellState>(null!);
const ShellProvider = ShellContext.Provider;

function useShell() {
    return useContext(ShellContext);
}

function createShellState(): ShellState {
    return {
        search: new Signal<SearchRequest | null>(null),
        query: new Signal(""),
    };
}

function useSearch(request: SearchRequest): void {
    const shell = useShell();

    useEffect(() => {
        shell.search.value = request;
        shell.query.value = "";

        return () => {
            if (shell.search.value !== request) return;

            shell.search.value = null;
            shell.query.value = "";
        };
    }, [request]);
}

export { createShellState, ShellProvider, useShell, useSearch }
