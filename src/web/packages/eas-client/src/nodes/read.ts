// nothing here throws, a malformed response still has to be readable enough to report on

import {
    child,
    childrenNamed,
    isNode,
    opaqueOf,
    textOf,
    type EasNode,
} from './node.ts';
import type { Tag } from './tags.ts';

export function pick(node: EasNode | undefined, tag: Tag): EasNode | undefined {
    return node === undefined ? undefined : child(node, tag.ns, tag.name);
}

export function pickAll(node: EasNode | undefined, tag: Tag): EasNode[] {
    return node === undefined ? [] : childrenNamed(node, tag.ns, tag.name);
}

export function path(node: EasNode | undefined, ...tags: Tag[]): EasNode | undefined {
    let found = node;
    for (const tag of tags) {
        if (found === undefined) return undefined;
        found = child(found, tag.ns, tag.name);
    }
    return found;
}

export function text(node: EasNode | undefined, tag: Tag): string | undefined {
    const found = pick(node, tag);
    return found === undefined ? undefined : textOf(found);
}

export function int(node: EasNode | undefined, tag: Tag): number | undefined {
    const value = text(node, tag);
    if (value === undefined) return undefined;

    const parsed = Number(value);
    return Number.isInteger(parsed) ? parsed : undefined;
}

export function bytes(node: EasNode | undefined, tag: Tag): Uint8Array | undefined {
    const found = pick(node, tag);
    return found === undefined ? undefined : opaqueOf(found);
}

export function flag(node: EasNode | undefined, tag: Tag): boolean {
    return pick(node, tag) !== undefined;
}

export function tri(node: EasNode | undefined, tag: Tag): boolean | undefined {
    const found = pick(node, tag);
    if (found === undefined) return undefined;

    const value = textOf(found);
    return value === '' ? true : value !== '0';
}

export function elements(node: EasNode | undefined): EasNode[] {
    return node === undefined ? [] : node.children.filter(isNode);
}
