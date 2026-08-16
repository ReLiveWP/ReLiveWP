import { CODE_PAGES } from '../generated/codepages.g.ts';

export interface CodePage {
    readonly page: number;
    readonly ns: string;
    readonly xmlns: string;
    readonly tags: readonly (string | undefined)[];
}

const byPage = new Map<number, CodePage>();
const byNamespace = new Map<string, CodePage>();

for (const generated of CODE_PAGES) {
    const tags: (string | undefined)[] = new Array(0x40);
    for (const [tag, token] of Object.entries(generated.tokens)) tags[token] = tag;

    const page: CodePage = { page: generated.page, ns: generated.ns, xmlns: generated.xmlns, tags };
    byPage.set(page.page, page);
    byNamespace.set(page.ns.toLowerCase(), page);
}

// built on first encode, decoding is the common case
let encodeTables: Map<string, ReadonlyMap<string, number>> | undefined;

function encodeTable(): Map<string, ReadonlyMap<string, number>> {
    if (encodeTables) return encodeTables;

    encodeTables = new Map();
    for (const generated of CODE_PAGES)
        encodeTables.set(generated.ns.toLowerCase(), new Map(Object.entries(generated.tokens)));

    return encodeTables;
}

export function codePageByNumber(page: number): CodePage | undefined {
    return byPage.get(page);
}

export function codePageByNamespace(ns: string): CodePage | undefined {
    return byNamespace.get(ns.toLowerCase());
}

export function tokenFor(ns: string, tag: string): number | undefined {
    return encodeTable().get(ns.toLowerCase())?.get(tag);
}

export function allCodePages(): readonly CodePage[] {
    return [...byPage.values()];
}
