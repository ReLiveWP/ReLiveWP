import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import type { EasChild, EasNode } from '../src/nodes/node.ts';

const dir = join(dirname(fileURLToPath(import.meta.url)), 'fixtures');

type ByteSpec =
    | { hex: string; note?: string }
    | { ascii: string; note?: string }
    | { fill: number; length: number; note?: string };

interface RawFixture {
    name: string;
    description: string;
    decodeOnly?: boolean;
    wbxml: ByteSpec[];
    tree: RawNode;
    expect?: { unknownTokens?: { page: number; ns: string; token: number; name: string }[] };
}

type RawNode = { ns: string; name: string; children: RawChild[] };
type RawChild = RawNode | { text: string } | { opaque: ByteSpec };

export interface Fixture {
    readonly name: string;
    readonly description: string;
    readonly decodeOnly: boolean;
    readonly bytes: Uint8Array;
    readonly tree: EasNode;
    readonly expect: RawFixture['expect'];
}

function toBytes(spec: ByteSpec): Uint8Array {
    if ('hex' in spec) {
        const cleaned = spec.hex.replace(/\s+/g, '');
        if (cleaned.length % 2 !== 0) throw new Error(`odd-length hex run: "${spec.hex}"`);
        const out = new Uint8Array(cleaned.length / 2);
        for (let i = 0; i < out.length; i++) out[i] = Number.parseInt(cleaned.slice(i * 2, i * 2 + 2), 16);
        return out;
    }
    if ('ascii' in spec) return new TextEncoder().encode(spec.ascii);
    return new Uint8Array(spec.length).fill(spec.fill);
}

function concat(specs: ByteSpec[]): Uint8Array {
    const parts = specs.map(toBytes);
    const out = new Uint8Array(parts.reduce((n, p) => n + p.length, 0));
    let at = 0;
    for (const p of parts) {
        out.set(p, at);
        at += p.length;
    }
    return out;
}

function toTree(raw: RawNode): EasNode {
    return { ns: raw.ns, name: raw.name, children: raw.children.map(toChild) };
}

function toChild(raw: RawChild): EasChild {
    if ('text' in raw) return { text: raw.text };
    if ('opaque' in raw) return { opaque: toBytes(raw.opaque) };
    return toTree(raw);
}

export function loadFixtures(): Fixture[] {
    return readdirSync(dir)
        .filter((f) => f.endsWith('.json'))
        .sort()
        .map((file) => {
            const raw = JSON.parse(readFileSync(join(dir, file), 'utf8')) as RawFixture;
            return {
                name: raw.name,
                description: raw.description,
                decodeOnly: raw.decodeOnly ?? false,
                bytes: concat(raw.wbxml),
                tree: toTree(raw.tree),
                expect: raw.expect,
            };
        });
}

export function hex(bytes: Uint8Array): string {
    return [...bytes].map((b) => b.toString(16).padStart(2, '0')).join(' ');
}
