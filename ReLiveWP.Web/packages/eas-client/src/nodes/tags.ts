import type { EasChild, EasNode } from './node.ts';

export type Kid =
    | EasChild
    | string
    | number
    | boolean
    | Uint8Array
    | readonly Kid[]
    | undefined
    | null;

export interface Tag {
    (...children: Kid[]): EasNode;
    readonly ns: string;
    readonly name: string;
}

export type TagSet<N extends string> = { readonly [K in N]: Tag };

function push(out: EasChild[], kid: Kid): void {
    if (kid === undefined || kid === null) return;

    switch (typeof kid) {
        case 'string': out.push({ text: kid }); return;
        case 'number': out.push({ text: String(kid) }); return;
        case 'boolean': out.push({ text: kid ? '1' : '0' }); return;
    }

    if (kid instanceof Uint8Array) { out.push({ opaque: kid }); return; }
    if (Array.isArray(kid)) { for (const k of kid as readonly Kid[]) push(out, k); return; }

    out.push(kid as EasChild);
}

// fn.name isn't writable, it has to be redefined
function makeTag(ns: string, name: string): Tag {
    const tag = (...children: Kid[]): EasNode => {
        const out: EasChild[] = [];
        for (const kid of children) push(out, kid);
        return { ns, name, children: out };
    };

    return Object.defineProperties(tag, {
        ns: { value: ns },
        name: { value: name },
    }) as Tag;
}

export function makeTags<N extends readonly string[]>(ns: string, names: N): TagSet<N[number]> {
    const set: Record<string, Tag> = {};
    for (const name of names) set[name] = makeTag(ns, name);
    return set as TagSet<N[number]>;
}

export function opt(value: string | number | boolean | undefined | null, tag: Tag): EasNode | undefined {
    return value === undefined || value === null ? undefined : tag(value);
}
