const MONTH = new Intl.DateTimeFormat(undefined, { month: "long", year: "numeric" });
const WEEKDAY = new Intl.DateTimeFormat(undefined, { weekday: "short" });
const FULL_DAY = new Intl.DateTimeFormat(undefined, { dateStyle: "full" });
const LONG_DAY = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "long", year: "numeric" });
const DAY_AND_MONTH = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "long" });
const MINUTES = new Intl.DateTimeFormat(undefined, { hour: "numeric", minute: "2-digit" });
const HOURS = new Intl.DateTimeFormat(undefined, { hour: "numeric" });

export function monthLabel(at: number): string {
    return MONTH.format(at);
}

export function dayLabel(at: number): string {
    return FULL_DAY.format(at);
}

// the first of january 2023 was a sunday, so a day offset from it is its own getDay index
export function weekdayLabels(firstDayOfWeek: number): string[] {
    return Array.from({ length: 7 }, (_, column) =>
        WEEKDAY.format(new Date(2023, 0, 1 + (firstDayOfWeek + column) % 7)));
}

export function weekdayOf(at: number): string {
    return WEEKDAY.format(at);
}

export function hourLabels(): string[] {
    return Array.from({ length: 24 }, (_, hour) => HOURS.format(new Date(2023, 0, 1, hour)));
}

// the first of a month names it. six weeks of bare numbers give the eye nothing to anchor on, and
// the leading and trailing days are exactly where that hurts. the formatter decides the order, so
// this reads "1 Sep" or "Sep 1" as the locale prefers
export function cellDate(at: number): string {
    const date = new Date(at);

    return date.getDate() === 1 ? DAY_AND_MONTH.format(at) : String(date.getDate());
}

export function clockTime(at: number): string {
    return (new Date(at).getMinutes() === 0 ? HOURS : MINUTES).format(at);
}

export function exactTime(at: number): string {
    return MINUTES.format(at);
}

export function rangeLabel(days: number[]): string {
    const first = days[0];
    const last = days[days.length - 1];

    if (first === undefined || last === undefined) return "";
    if (first === last) return FULL_DAY.format(first);

    return LONG_DAY.formatRange(first, last);
}
