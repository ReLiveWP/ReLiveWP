declare global {
    interface Array<T> {
        flatMap<U>(callback: (value: T, index: number, array: T[]) => U | U[], thisArg?: unknown): U[];
    }

    interface ObjectConstructor {
        fromEntries<T>(entries: Iterable<readonly [PropertyKey, T]>): { [key: string]: T };
    }

    interface Promise<T> {
        finally(onFinally?: (() => void) | null): Promise<T>;
    }
}

type FlatMapCallback = (value: unknown, index: number, array: unknown[]) => unknown;

if (!Array.prototype.flatMap) {
    (Array.prototype as { flatMap?: unknown }).flatMap = function (this: unknown[], callback: FlatMapCallback, thisArg?: unknown) {
        return this.reduce<unknown[]>((out, value, index) => out.concat(callback.call(thisArg, value, index, this)), []);
    };
}

if (!Promise.prototype.finally) {
    (Promise.prototype as { finally?: unknown }).finally = function (this: Promise<unknown>, onFinally?: () => void) {
        return this.then(
            (value) => Promise.resolve(onFinally?.()).then(() => value),
            (reason) => Promise.resolve(onFinally?.()).then(() => { throw reason; }));
    };
}

if (!Object.fromEntries) {
    (Object as { fromEntries?: unknown }).fromEntries = function (entries: Iterable<readonly [PropertyKey, unknown]>) {
        const out: { [key: string]: unknown } = {};
        for (const [key, value] of entries) out[key as string] = value;

        return out;
    };
}

export { };
