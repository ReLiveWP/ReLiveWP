import { int, tags, text, textOf, type EasNode, type PropertySpec } from '@relivewp/eas-client/nodes';
import type { Body, BodyType, Sensitivity } from '@relivewp/eas-store';

const { AirSyncBase: AB } = tags;

const SENSITIVITY: Readonly<Record<number, Sensitivity>> = {
    0: 'normal', 1: 'personal', 2: 'private', 3: 'confidential',
};

export function readSensitivity(value: number | undefined): Sensitivity {
    return SENSITIVITY[value ?? 0] ?? 'normal';
}

export function readBody(node: EasNode | undefined): Body | null {
    if (node === undefined) return null;

    const type: BodyType = int(node, AB.Type) === 2 ? 'html' : 'text';
    const truncated = int(node, AB.Truncated) === 1;
    const size = int(node, AB.EstimatedDataSize);

    return {
        type,
        content: text(node, AB.Data) ?? '',
        truncated,
        fullSize: truncated ? size ?? null : null,
    };
}

export function readCategories(node: EasNode | undefined): string[] {
    if (node === undefined) return [];
    return node.children.filter((child): child is EasNode => 'name' in child).map(textOf);
}

export function decodeBase64(value: string): Uint8Array {
    const binary = atob(value.replace(/\s+/g, ''));
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}

export function epoch(value: Date | undefined): number | null {
    return value === undefined ? null : value.getTime();
}

export function labelled(entries: [string, string | undefined][]): { label: string; value: string }[] {
    return entries
        .filter((entry): entry is [string, string] =>
            entry[1] !== undefined && entry[1].trim().length > 0)
        .map(([label, value]) => ({ label, value }));
}

// same table the projection reads, so the two can't drift. what's declared isn't ghosted, so
// dropping it from a later Change deletes it on the server.
export function supportedFrom(
    properties: readonly PropertySpec[], namespaces: readonly string[]): EasNode[] {
    return properties
        .filter((property) => namespaces.includes(property.tag.ns))
        .map((property) => property.tag());
}
