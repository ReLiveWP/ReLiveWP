import type { Contact } from "@relivewp/eas-store";

export function cidIndex(contacts: Contact[]): ReadonlyMap<string, Contact> {
    const index = new Map<string, Contact>();

    for (const contact of contacts) {
        const cid = contact.annotation?.cid?.toLowerCase();
        if (cid !== undefined && cid.length > 0 && !index.has(cid)) index.set(cid, contact);
    }

    return index;
}

export function contactFor(index: ReadonlyMap<string, Contact>, cid: string | null): Contact | undefined {
    return cid === null ? undefined : index.get(cid.toLowerCase());
}
