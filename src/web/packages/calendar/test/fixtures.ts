import type { Event, EventException, Recurrence } from '@relivewp/eas-store';

import type { Occurrence } from '../src/index.ts';

// every algorithm here works in local time, so the suite pins a zone with a well known transition
// rather than inheriting whatever the machine happens to be set to
process.env.TZ = 'Europe/London';

export const ZONED = new Date(2026, 6, 1).getTimezoneOffset() === -60;

export function at(year: number, month: number, day: number, hour = 0, minute = 0): number {
    return new Date(year, month, day, hour, minute).getTime();
}

export function event(patch: Partial<Event> & Pick<Event, 'id' | 'startAt' | 'endAt'>): Event {
    return {
        folderId: 'calendar',
        uid: null,
        subject: 'event',
        location: null,
        allDay: false,
        busy: 'busy',
        sensitivity: 'normal',
        reminderMinutes: null,
        timezone: null,
        dtStamp: null,
        meetingStatus: null,
        organizer: null,
        attendees: [],
        recurrence: null,
        exceptions: [],
        categories: [],
        body: null,
        ...patch,
    };
}

export function rule(patch: Partial<Recurrence> & Pick<Recurrence, 'type'>): Recurrence {
    return {
        interval: null,
        until: null,
        occurrences: null,
        dayOfWeek: null,
        dayOfMonth: null,
        weekOfMonth: null,
        monthOfYear: null,
        firstDayOfWeek: null,
        calendarType: null,
        isLeapMonth: null,
        regenerate: null,
        deadOccur: null,
        startAt: null,
        ...patch,
    };
}

export function exception(patch: Partial<EventException> & Pick<EventException, 'exceptionStartAt'>): EventException {
    return {
        deleted: false,
        subject: null,
        location: null,
        startAt: null,
        endAt: null,
        allDay: null,
        ...patch,
    };
}

export function occurrence(
    key: string,
    startAt: number,
    endAt: number,
    patch: Partial<Occurrence> = {},
): Occurrence {
    return {
        key,
        event: event({ id: key, startAt, endAt }),
        startAt,
        endAt,
        allDay: false,
        subject: key,
        location: null,
        busy: 'busy',
        seriesStartAt: null,
        exception: false,
        ...patch,
    };
}

export function keys(occurrences: { key: string }[]): string[] {
    return occurrences.map((item) => item.key);
}

export function stamps(occurrences: { startAt: number }[]): string[] {
    return occurrences.map((item) => new Date(item.startAt).toString().slice(0, 21));
}
