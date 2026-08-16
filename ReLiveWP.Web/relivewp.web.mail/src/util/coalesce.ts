export type Coalesced = {
    fire: () => void,
    cancel: () => void,
};

// a drain reports every round, and a plain debounce would keep pushing its deadline back until the
// whole sync finished. `maxWaitMs` is what makes the list fill in as the data lands.
export function coalescing(run: () => void, settleMs: number, maxWaitMs: number): Coalesced {
    let timer: ReturnType<typeof setTimeout> | undefined;
    let waitingSince = 0;

    const invoke = () => {
        timer = undefined;
        waitingSince = 0;
        run();
    };

    return {
        fire: () => {
            const now = Date.now();
            if (waitingSince === 0) waitingSince = now;

            clearTimeout(timer);
            timer = setTimeout(invoke, Math.max(0, Math.min(settleMs, waitingSince + maxWaitMs - now)));
        },
        cancel: () => {
            clearTimeout(timer);
            timer = undefined;
            waitingSince = 0;
        },
    };
}
