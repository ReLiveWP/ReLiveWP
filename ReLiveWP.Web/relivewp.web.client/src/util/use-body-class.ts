import { useLayoutEffect } from "preact/hooks";

export function useBodyClass(name: string) {
    useLayoutEffect(() => {
        document.body.classList.add(name);
        return () => document.body.classList.remove(name);
    }, [name]);
}
