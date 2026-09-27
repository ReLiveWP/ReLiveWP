import type { Message } from "@relivewp/eas-store";

export type Row =
    | { kind: "header", key: string, label: string }
    | { kind: "message", key: string, message: Message, first: boolean };

const DAY = 86_400_000;

const TIME = new Intl.DateTimeFormat(undefined, { hour: "numeric", minute: "2-digit" });
const SHORT = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "short" });
const LONG = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "short", year: "numeric" });
const WEEKDAY = new Intl.DateTimeFormat(undefined, { weekday: "long" });
const MONTH = new Intl.DateTimeFormat(undefined, { month: "long" });
const MONTH_AND_YEAR = new Intl.DateTimeFormat(undefined, { month: "long", year: "numeric" });

export function startOfDay(at: number): number {
    const date = new Date(at);
    date.setHours(0, 0, 0, 0);
    return date.getTime();
}

function daysBefore(midnight: number, today: number): number {
    return Math.round((today - midnight) / DAY);
}

function label(receivedAt: number, today: number): string {
    const delta = daysBefore(startOfDay(receivedAt), today);

    if (delta <= 0) return "today";
    if (delta === 1) return "yesterday";
    if (delta < 7) return WEEKDAY.format(receivedAt);
    if (delta < 14) return "last week";

    return new Date(receivedAt).getFullYear() === new Date(today).getFullYear()
        ? MONTH.format(receivedAt)
        : MONTH_AND_YEAR.format(receivedAt);
}

export function toRows(messages: Message[], now: number): Row[] {
    const today = startOfDay(now);
    const rows: Row[] = [];

    let group: string | null = null;

    for (const message of messages) {
        const next = label(message.receivedAt, today);
        const opens = next !== group;

        if (opens) {
            group = next;
            rows.push({ kind: "header", key: `header\uffff${next}\uffff${rows.length}`, label: next });
        }

        rows.push({
            kind: "message",
            key: `${message.folderId}\uffff${message.id}`,
            message,
            first: opens,
        });
    }

    return rows;
}

export function stamp(receivedAt: number, now: number): string {
    const delta = daysBefore(startOfDay(receivedAt), startOfDay(now));

    if (delta < 7) return TIME.format(receivedAt);
    if (delta < 365) return SHORT.format(receivedAt);

    return LONG.format(receivedAt);
}
