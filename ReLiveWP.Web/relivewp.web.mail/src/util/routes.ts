import type { Folder } from "@relivewp/eas-store";

export const MODES = ["day", "week", "month"] as const;
export type Mode = (typeof MODES)[number];

export const DEFAULT_MODE: Mode = "month";
export const DEFAULT_FOLDER = "inbox";

const NAMED: ReadonlySet<string> = new Set([
    "inbox", "drafts", "sent", "outbox", "deleted", "junk",
]);

const ISO = /^\d{4}-\d{2}-\d{2}$/;

export function folderSlug(folder: Folder): string {
    return NAMED.has(folder.role) ? folder.role : folder.id;
}

export function isNamedFolder(slug: string): boolean {
    return NAMED.has(slug);
}

export function findFolder(folders: Folder[], slug: string | undefined): Folder | undefined {
    if (slug === undefined) return undefined;

    return folders.find((folder) => NAMED.has(folder.role) && folder.role === slug)
        ?? folders.find((folder) => folder.id === slug);
}

export function formatDate(at: number): string {
    const date = new Date(at);
    const pad = (value: number): string => String(value).padStart(2, "0");

    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function parseDate(text: string | undefined): number | null {
    if (text === undefined || !ISO.test(text)) return null;

    const [year, month, day] = text.split("-").map(Number) as [number, number, number];
    const at = new Date(year, month - 1, day).getTime();

    return Number.isNaN(at) || formatDate(at) !== text ? null : at;
}

export function isMode(value: string | undefined): value is Mode {
    return value !== undefined && (MODES as readonly string[]).includes(value);
}

export function mailPath(slug: string, messageId?: string | null | undefined): string {
    const folder = `/mail/${encodeURIComponent(slug)}`;

    return messageId === null || messageId === undefined
        ? folder
        : `${folder}/${encodeURIComponent(messageId)}`;
}

export function calendarPath(mode: Mode, anchor: number): string {
    return `/calendar/${mode}/${formatDate(anchor)}`;
}
