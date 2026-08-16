import type { Contact, Message } from './model.ts';

const SEPARATORS = /[^\p{L}\p{N}]+/gu;
const MIN_LENGTH = 2;
const MAX_TOKENS = 96;

function split(value: string): string[] {
    return value.toLowerCase().split(SEPARATORS).filter((part) => part.length >= MIN_LENGTH);
}

export function searchTerms(query: string): string[] {
    return [...new Set(split(query))];
}

export function messageTokens(message: Message): string[] {
    const sources = [
        message.subject,
        message.preview,
        message.from?.email ?? '',
        message.from?.name ?? '',
        ...message.to.map((address) => address.email),
        ...message.to.map((address) => address.name ?? ''),
    ];

    const tokens = new Set<string>();
    for (const source of sources) {
        for (const token of split(source)) {
            tokens.add(token);
            if (tokens.size >= MAX_TOKENS) return [...tokens];
        }
    }

    return [...tokens];
}

export function contactTokens(contact: Contact): string[] {
    const sources = [
        contact.displayName,
        contact.firstName ?? '',
        contact.lastName ?? '',
        contact.nickname ?? '',
        contact.company ?? '',
        contact.jobTitle ?? '',
        ...contact.emails.map((entry) => entry.value),
        ...contact.phones.map((entry) => entry.value),
    ];

    const tokens = new Set<string>();
    for (const source of sources) {
        for (const token of split(source)) {
            tokens.add(token);
            if (tokens.size >= MAX_TOKENS) return [...tokens];
        }
    }

    return [...tokens];
}

export function matchesTerms(tokens: readonly string[], terms: readonly string[]): boolean {
    return terms.every((term) => tokens.some((token) => token.startsWith(term)));
}
