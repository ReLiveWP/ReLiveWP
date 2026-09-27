import type { Tag } from './tags.ts';
import { isNode, type EasChild, type EasNode } from './node.ts';
import { writeDate, type DateFormat } from './dates.ts';

export interface PropertySpec {
    property: string;
    tag: Tag;
    kind: string;
    dateFormat?: DateFormat;
}

function encodeValue(value: unknown, spec: PropertySpec): EasChild | undefined {
    if (value === undefined) return undefined;
    if (value instanceof Date) return spec.tag(writeDate(value, spec.dateFormat ?? 'extended'));
    if (value !== null && typeof value === 'object' && 'name' in value) return value as EasNode;
    return spec.tag(value as string | number | boolean);
}

export function mergeProperties(
    original: EasNode, changes: Record<string, unknown>, specs: readonly PropertySpec[]): EasNode {
    const touched = new Map<string, PropertySpec>();
    for (const spec of specs)
        if (Object.prototype.hasOwnProperty.call(changes, spec.property))
            touched.set(`${spec.tag.ns}:${spec.tag.name}`, spec);

    const children: EasChild[] = [];
    const written = new Set<string>();

    for (const child of original.children) {
        if (!isNode(child)) { children.push(child); continue; }

        const key = `${child.ns}:${child.name}`;
        const spec = touched.get(key);
        if (spec === undefined) { children.push(child); continue; }

        written.add(key);
        const replacement = encodeValue(changes[spec.property], spec);
        if (replacement !== undefined) children.push(replacement);
    }

    for (const [key, spec] of touched) {
        if (written.has(key)) continue;
        const added = encodeValue(changes[spec.property], spec);
        if (added !== undefined) children.push(added);
    }

    return { ns: original.ns, name: original.name, children };
}
