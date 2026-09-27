import type { BusyStatus, Event, EventException, Recurrence } from '@relivewp/eas-store';

import { addDays, daysBetween, daysInMonth, onDate, startOfDay, startOfWeek } from './dates.ts';

export const DAILY = 0;
export const WEEKLY = 1;
export const MONTHLY = 2;
export const MONTHLY_NTH = 3;
export const YEARLY = 5;
export const YEARLY_NTH = 6;

export interface Occurrence {
    key: string;
    event: Event;
    startAt: number;
    endAt: number;
    allDay: boolean;
    subject: string;
    location: string | null;
    busy: BusyStatus;
    seriesStartAt: number | null;
    exception: boolean;
}

const MAX_STEPS = 4096;

function overlaps(startAt: number, endAt: number, from: number, to: number): boolean {
    if (startAt >= to) return false;

    return endAt > from || (endAt === startAt && startAt >= from);
}

function matches(mask: number, weekday: number): boolean {
    return (mask & (1 << weekday)) !== 0;
}

function nthWeekday(year: number, month: number, mask: number, week: number): number | null {
    const total = daysInMonth(year, month);
    const wanted = week >= 5 ? Infinity : week;
    let seen = 0;
    let last: number | null = null;

    for (let day = 1; day <= total; day++) {
        if (!matches(mask, new Date(year, month, day).getDay())) continue;

        last = day;
        seen++;

        if (seen === wanted) return day;
    }

    return last;
}

function monthsBetween(from: number, to: number): number {
    const a = new Date(from);
    const b = new Date(to);

    return 12 * (b.getFullYear() - a.getFullYear()) + b.getMonth() - a.getMonth();
}

interface Stepper {
    at: (step: number) => number[];
    seek: (at: number) => number;
}

function stepper(base: number, rule: Recurrence): Stepper | null {
    const start = new Date(base);
    const interval = Math.max(1, rule.interval ?? 1);
    const mask = rule.dayOfWeek ?? (1 << start.getDay());
    const dayOfMonth = rule.dayOfMonth ?? start.getDate();
    const month = (rule.monthOfYear ?? start.getMonth() + 1) - 1;
    const week = rule.weekOfMonth ?? 1;

    const clamped = (year: number, target: number): number => Math.min(dayOfMonth, daysInMonth(year, target));

    const monthly = (step: number, day: (year: number, month: number) => number | null): number[] => {
        const shifted = new Date(start.getFullYear(), start.getMonth() + step * interval, 1);
        const chosen = day(shifted.getFullYear(), shifted.getMonth());

        return chosen === null ? [] : [onDate(shifted.getFullYear(), shifted.getMonth(), chosen, base)];
    };

    const yearly = (step: number, day: (year: number, month: number) => number | null): number[] => {
        const year = start.getFullYear() + step * interval;
        const chosen = day(year, month);

        return chosen === null ? [] : [onDate(year, month, chosen, base)];
    };

    switch (rule.type) {
        case DAILY:
            return {
                at: (step) => {
                    const at = addDays(base, step * interval);

                    return rule.dayOfWeek !== null && !matches(mask, new Date(at).getDay()) ? [] : [at];
                },
                seek: (at) => Math.floor(daysBetween(base, at) / interval),
            };

        case WEEKLY: {
            const anchor = startOfWeek(startOfDay(base), rule.firstDayOfWeek ?? 0);

            return {
                at: (step) => {
                    const days: number[] = [];

                    for (let offset = 0; offset < 7; offset++) {
                        const day = new Date(addDays(anchor, step * interval * 7 + offset));

                        if (matches(mask, day.getDay())) {
                            days.push(onDate(day.getFullYear(), day.getMonth(), day.getDate(), base));
                        }
                    }

                    return days;
                },
                seek: (at) => Math.floor(daysBetween(anchor, at) / 7 / interval),
            };
        }

        case MONTHLY:
            return {
                at: (step) => monthly(step, clamped),
                seek: (at) => Math.floor(monthsBetween(base, at) / interval),
            };

        case MONTHLY_NTH:
            return {
                at: (step) => monthly(step, (year, target) => nthWeekday(year, target, mask, week)),
                seek: (at) => Math.floor(monthsBetween(base, at) / interval),
            };

        case YEARLY:
            return {
                at: (step) => yearly(step, clamped),
                seek: (at) => Math.floor((new Date(at).getFullYear() - start.getFullYear()) / interval),
            };

        case YEARLY_NTH:
            return {
                at: (step) => yearly(step, (year) => nthWeekday(year, month, mask, week)),
                seek: (at) => Math.floor((new Date(at).getFullYear() - start.getFullYear()) / interval),
            };

        default:
            return null;
    }
}

function slots(event: Event, rule: Recurrence, from: number, to: number): number[] | null {
    const steps = stepper(event.startAt, rule);
    if (steps === null) return null;

    const limit = rule.occurrences ?? 0;
    const first = limit > 0 ? 0 : Math.max(0, steps.seek(from));
    const found: number[] = [];
    let count = 0;

    for (let step = first; step < first + MAX_STEPS; step++) {
        for (const at of steps.at(step)) {
            if (at < event.startAt) continue;
            if (rule.until !== null && at > rule.until) return found;

            count++;
            if (limit > 0 && count > limit) return found;
            if (at >= to) return found;
            if (at >= from) found.push(at);
        }
    }

    return found;
}

function instance(event: Event, startAt: number, endAt: number, seriesStartAt: number | null): Occurrence {
    return {
        key: seriesStartAt === null ? event.id : `${event.id}\uffff${seriesStartAt}`,
        event,
        startAt,
        endAt,
        allDay: event.allDay,
        subject: event.subject,
        location: event.location,
        busy: event.busy,
        seriesStartAt,
        exception: false,
    };
}

function override(base: Occurrence, exception: EventException, duration: number): Occurrence {
    const startAt = exception.startAt ?? base.startAt;

    return {
        ...base,
        startAt,
        endAt: exception.endAt ?? startAt + duration,
        allDay: exception.allDay ?? base.allDay,
        subject: exception.subject ?? base.subject,
        location: exception.location ?? base.location,
        exception: true,
    };
}

export function expand(event: Event, from: number, to: number): Occurrence[] {
    const duration = Math.max(0, event.endAt - event.startAt);
    const generated = event.recurrence === null ? null : slots(event, event.recurrence, from - duration, to);

    if (generated === null) {
        const single = instance(event, event.startAt, event.endAt, null);

        return overlaps(single.startAt, single.endAt, from, to) ? [single] : [];
    }

    const exceptions = new Map<number, EventException>();
    for (const exception of event.exceptions) {
        if (exception.exceptionStartAt !== null) exceptions.set(exception.exceptionStartAt, exception);
    }

    const found: Occurrence[] = [];
    const applied = new Set<number>();

    for (const at of generated) {
        const exception = exceptions.get(at);

        if (exception !== undefined) {
            applied.add(at);
            if (exception.deleted) continue;
        }

        const base = instance(event, at, at + duration, at);
        found.push(exception === undefined ? base : override(base, exception, duration));
    }
    
    for (const [at, exception] of exceptions) {
        if (applied.has(at) || exception.deleted) continue;

        found.push(override(instance(event, at, at + duration, at), exception, duration));
    }

    return found
        .filter((occurrence) => overlaps(occurrence.startAt, occurrence.endAt, from, to))
        .sort((a, b) => a.startAt - b.startAt);
}

export function expandAll(events: Event[], from: number, to: number): Occurrence[] {
    return events
        .flatMap((event) => expand(event, from, to))
        .sort((a, b) => a.startAt - b.startAt);
}
