import { useCallback, useEffect, useState } from "preact/hooks";

import { reason } from "~/util/reason";

export type Async<T> = {
    value: T | undefined,
    loading: boolean,
    error: string | null,
};

export type AsyncOptions = {
    keep?: boolean,
    debounceMs?: number,
    describe?: (thrown: unknown) => string,
};

export type AsyncHandle<T> = Async<T> & {
    reload: () => void,
    set: (update: (prev: T | undefined) => T | undefined) => void,
};

const IDLE: Async<never> = { value: undefined, loading: false, error: null };

// a null load parks the hook idle, which is how callers say the inputs are not there yet. `keep`
// holds the last value across a reload or a failure instead of dropping it
export function useAsync<T>(
    load: (() => Promise<T>) | null,
    deps: readonly unknown[],
    { keep = false, debounceMs = 0, describe = reason }: AsyncOptions = {},
): AsyncHandle<T> {
    const [state, setState] = useState<Async<T>>(IDLE);
    const [attempt, setAttempt] = useState(0);

    useEffect(() => {
        if (load === null) {
            setState(IDLE);
            return;
        }

        let live = true;
        setState((prev) => ({ value: keep ? prev.value : undefined, loading: true, error: null }));

        const start = () => {
            load().then((value) => {
                if (live) setState({ value, loading: false, error: null });
            }, (thrown: unknown) => {
                if (live) setState((prev) => ({ value: keep ? prev.value : undefined, loading: false, error: describe(thrown) }));
            });
        };

        if (debounceMs === 0) {
            start();
            return () => { live = false; };
        }

        const timer = setTimeout(start, debounceMs);

        return () => {
            live = false;
            clearTimeout(timer);
        };
    }, [...deps, attempt, load === null]);

    const reload = useCallback(() => { setAttempt((count) => count + 1); }, []);

    const set = useCallback((update: (prev: T | undefined) => T | undefined) => {
        setState((prev) => ({ ...prev, value: update(prev.value) }));
    }, []);

    return { ...state, reload, set };
}
