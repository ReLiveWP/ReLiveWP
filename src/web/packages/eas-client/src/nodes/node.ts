const utf8 = new TextDecoder('utf-8', { fatal: false });

export interface EasNode {
    readonly ns: string;
    readonly name: string;
    readonly children: EasChild[];
}

export interface EasText {
    readonly text: string;
}

export interface EasOpaque {
    readonly opaque: Uint8Array;
}

export type EasChild = EasNode | EasText | EasOpaque;

export function isNode(child: EasChild): child is EasNode {
    return 'name' in child;
}

export function isText(child: EasChild): child is EasText {
    return 'text' in child;
}

export function isOpaque(child: EasChild): child is EasOpaque {
    return 'opaque' in child;
}

export function el(ns: string, name: string, ...children: (EasChild | string)[]): EasNode {
    return { ns, name, children: children.map((c) => (typeof c === 'string' ? { text: c } : c)) };
}

export function opaque(bytes: Uint8Array): EasOpaque {
    return { opaque: bytes };
}

export function child(node: EasNode, ns: string, name: string): EasNode | undefined {
    for (const c of node.children) if (isNode(c) && c.ns === ns && c.name === name) return c;
    return undefined;
}

export function childrenNamed(node: EasNode, ns: string, name: string): EasNode[] {
    return node.children.filter((c): c is EasNode => isNode(c) && c.ns === ns && c.name === name);
}

// an opaque text element is UTF-8, same as STR_I
export function textOf(node: EasNode): string {
    let out = '';
    for (const c of node.children) {
        if (isText(c)) out += c.text;
        else if (isOpaque(c)) out += utf8.decode(c.opaque);
    }
    return out;
}

export function opaqueOf(node: EasNode): Uint8Array | undefined {
    for (const c of node.children) if (isOpaque(c)) return c.opaque;
    return undefined;
}
