import type { Address } from '@relivewp/eas-store';

import { decodeBase64 } from './shared.ts';

const ENCODED_WORD = /=\?([^?]+)\?([bBqQ])\?([^?]*)\?=/g;

function decodeQuotedPrintable(value: string): Uint8Array {
    const source = value.replaceAll('_', ' ');
    const bytes: number[] = [];

    for (let i = 0; i < source.length; i++) {
        if (source[i] === '=' && i + 2 < source.length) {
            const hex = source.slice(i + 1, i + 3);
            if (/^[0-9a-fA-F]{2}$/.test(hex)) {
                bytes.push(parseInt(hex, 16));
                i += 2;
                continue;
            }
        }
        bytes.push(source.charCodeAt(i) & 0xff);
    }

    return new Uint8Array(bytes);
}

export function decodeEncodedWords(value: string): string {
    if (!value.includes('=?')) return value;

    return value.replace(ENCODED_WORD, (whole, charset: string, encoding: string, text: string) => {
        const bytes = encoding.toLowerCase() === 'b'
            ? decodeBase64(text)
            : decodeQuotedPrintable(text);

        try {
            return new TextDecoder(charset.toLowerCase(), { fatal: false }).decode(bytes);
        } catch {
            return whole;
        }
    });
}

function unquote(value: string): string {
    const trimmed = value.trim();
    if (!trimmed.startsWith('"') || !trimmed.endsWith('"') || trimmed.length < 2) return trimmed;
    return trimmed.slice(1, -1).replace(/\\(.)/g, '$1');
}

function one(raw: string): Address | undefined {
    const value = raw.trim();
    if (value.length === 0) return undefined;

    const open = value.lastIndexOf('<');
    const close = value.lastIndexOf('>');

    if (open !== -1 && close > open) {
        const email = value.slice(open + 1, close).trim();
        const name = decodeEncodedWords(unquote(value.slice(0, open))).trim();
        return { name: name.length === 0 ? null : name, email };
    }

    return { name: null, email: value };
}

// display names can hold commas, quotes and angle brackets, so the split has to track them
export function parseAddressList(value: string | undefined): Address[] {
    if (value === undefined || value.trim().length === 0) return [];

    const parts: string[] = [];
    let current = '';
    let quoted = false;
    let escaped = false;
    let depth = 0;

    for (const character of value) {
        if (escaped) {
            current += character;
            escaped = false;
            continue;
        }

        if (character === '\\' && quoted) {
            current += character;
            escaped = true;
            continue;
        }

        if (character === '"') quoted = !quoted;
        else if (!quoted && character === '<') depth++;
        else if (!quoted && character === '>') depth = Math.max(0, depth - 1);
        else if (!quoted && depth === 0 && (character === ',' || character === ';')) {
            parts.push(current);
            current = '';
            continue;
        }

        current += character;
    }

    parts.push(current);

    return parts
        .map(one)
        .filter((address): address is Address => address !== undefined);
}

export function parseAddress(value: string | undefined): Address | null {
    return parseAddressList(value)[0] ?? null;
}
