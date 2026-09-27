import { CALENDAR_PROPERTIES, readCalendar } from '@relivewp/eas-client';
import { int, pick, pickAll, readDate, tags, text, tri, type EasNode } from '@relivewp/eas-client/nodes';
import type {
    Attendee,
    AttendeeStatus,
    AttendeeType,
    BusyStatus,
    Event,
    EventException,
    Recurrence,
} from '@relivewp/eas-store';

import { epoch, readBody, readCategories, readSensitivity, supportedFrom } from './shared.ts';

const { Calendar: C } = tags;

// fourteen of these are mandatory when Supported is sent for calendar, drop one and the request
// is malformed
export const CALENDAR_SUPPORTED: EasNode[] = supportedFrom(CALENDAR_PROPERTIES, ['Calendar']);

export const CALENDAR_REQUIRED_SUPPORTED = [
    'DtStamp', 'Categories', 'Sensitivity', 'BusyStatus', 'UID', 'Timezone', 'StartTime',
    'Subject', 'Location', 'EndTime', 'Recurrence', 'AllDayEvent', 'Reminder', 'Exceptions',
] as const;

const BUSY: Readonly<Record<number, BusyStatus>> = {
    0: 'free', 1: 'tentative', 2: 'busy', 3: 'outOfOffice', 4: 'workingElsewhere',
};

const ATTENDEE_STATUS: Readonly<Record<number, AttendeeStatus>> = {
    0: 'notResponded', 2: 'tentative', 3: 'accepted', 4: 'declined', 5: 'notResponded',
};

const ATTENDEE_TYPE: Readonly<Record<number, AttendeeType>> = {
    1: 'required', 2: 'optional', 3: 'resource',
};

function when(value: string | undefined): number | null {
    return epoch(readDate(value));
}

function readAttendees(node: EasNode | undefined): Attendee[] {
    return pickAll(node, C.Attendee)
        .map((attendee): Attendee | undefined => {
            const email = text(attendee, C.Email);
            if (email === undefined) return undefined;

            return {
                name: text(attendee, C.Name) ?? null,
                email,
                status: ATTENDEE_STATUS[int(attendee, C.AttendeeStatus) ?? 0] ?? 'unknown',
                type: ATTENDEE_TYPE[int(attendee, C.AttendeeType) ?? 0] ?? 'unknown',
            };
        })
        .filter((attendee): attendee is Attendee => attendee !== undefined);
}

function readRecurrence(node: EasNode | undefined): Recurrence | null {
    if (node === undefined) return null;

    return {
        type: int(node, C.Type) ?? 0,
        interval: int(node, C.Interval) ?? null,
        until: when(text(node, C.Until)),
        occurrences: int(node, C.Occurrences) ?? null,
        dayOfWeek: int(node, C.DayOfWeek) ?? null,
        dayOfMonth: int(node, C.DayOfMonth) ?? null,
        weekOfMonth: int(node, C.WeekOfMonth) ?? null,
        monthOfYear: int(node, C.MonthOfYear) ?? null,
        firstDayOfWeek: int(node, C.FirstDayOfWeek) ?? null,
        calendarType: int(node, C.CalendarType) ?? null,
        isLeapMonth: tri(node, C.IsLeapMonth) ?? null,
        regenerate: null,
        deadOccur: null,
        startAt: null,
    };
}

function readExceptions(node: EasNode | undefined): EventException[] {
    return pickAll(node, C.Exception).map((exception) => ({
        deleted: int(exception, C.Deleted) === 1,
        exceptionStartAt: when(text(exception, C.ExceptionStartTime)),
        subject: text(exception, C.Subject) ?? null,
        location: text(exception, C.Location) ?? null,
        startAt: when(text(exception, C.StartTime)),
        endAt: when(text(exception, C.EndTime)),
        allDay: tri(exception, C.AllDayEvent) ?? null,
    }));
}

export function readEvent(data: EasNode, folderId: string, id: string): Event {
    const item = readCalendar(data);
    const startAt = when(item.startTime) ?? 0;

    return {
        id,
        folderId,
        uid: item.uid ?? null,
        subject: item.subject ?? '',
        location: item.location ?? null,
        startAt,
        endAt: when(item.endTime) ?? startAt,
        allDay: item.allDayEvent === 1,
        busy: BUSY[item.busyStatus ?? 2] ?? 'busy',
        sensitivity: readSensitivity(item.sensitivity),
        reminderMinutes: item.reminder ?? null,
        timezone: item.timezone ?? null,
        dtStamp: when(item.dtStamp),
        meetingStatus: item.meetingStatus ?? null,
        organizer: item.organizerEmail === undefined
            ? null
            : { name: item.organizerName ?? null, email: item.organizerEmail },
        attendees: readAttendees(pick(data, C.Attendees)),
        recurrence: readRecurrence(pick(data, C.Recurrence)),
        exceptions: readExceptions(pick(data, C.Exceptions)),
        categories: readCategories(item.categories),
        body: readBody(item.body),
    };
}
