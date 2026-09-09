import type { Contact } from "@relivewp/eas-store";

export type Row =
    | { kind: "header", key: string, letter: string }
    | { kind: "contact", key: string, contact: Contact, first: boolean };

export type IndexEntry = {
    letter: string,
    row: number | null,
};

export const OTHER = "#";

const LETTERS = "abcdefghijklmnopqrstuvwxyz".split("");

export const ALPHABET: readonly string[] = [...LETTERS, OTHER];

export function letterOf(contact: Contact): string {
    const first = contact.sortName.trim().charAt(0).toLowerCase();
    return LETTERS.includes(first) ? first : OTHER;
}

export function toRows(contacts: Contact[]): Row[] {
    const rows: Row[] = [];
    let group: string | null = null;

    for (const contact of contacts) {
        const next = letterOf(contact);
        const opens = next !== group;

        if (opens) {
            group = next;
            rows.push({ kind: "header", key: `header\uffff${next}`, letter: next });
        }

        rows.push({
            kind: "contact",
            key: `${contact.folderId}\uffff${contact.id}`,
            contact,
            first: opens,
        });
    }

    return rows;
}

export function letterIndex(rows: Row[]): IndexEntry[] {
    const at = new Map<string, number>();
    rows.forEach((row, index) => {
        if (row.kind === "header" && !at.has(row.letter)) at.set(row.letter, index);
    });

    return ALPHABET.map((letter) => ({ letter, row: at.get(letter) ?? null }));
}

export type NameParts = {
    lead: string,
    surname: string,
};

export function nameOf(contact: Contact): NameParts {
    const surname = contact.lastName?.trim() ?? "";
    const lead = [contact.firstName, contact.middleName]
        .map((part) => part?.trim() ?? "")
        .filter((part) => part.length > 0)
        .join(" ");

    if (lead.length === 0 && surname.length === 0) return { lead: contact.displayName.trim(), surname: "" };

    return { lead, surname };
}

export function initialFor(name: string): string {
    const first = name.trim().charAt(0).toUpperCase();
    return first === "" ? "?" : first;
}

export function initialOf(contact: Contact): string {
    const { lead, surname } = nameOf(contact);
    return initialFor(lead.length > 0 ? lead : surname);
}
