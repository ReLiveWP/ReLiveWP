import { tags, writeDate, type EasChild } from '@relivewp/eas-client/nodes';

import type { FakeMailbox } from './mailbox.ts';

const {
    AirSyncBase: AB,
    Calendar: C,
    Contacts: CT,
    Contacts2: CT2,
    Email: E,
    Tasks: T,
    WindowsLive: WL,
} = tags;

const extended = (value: Date) => writeDate(value, 'extended')!;
const compact = (value: Date) => writeDate(value, 'compact')!;

export interface MailFields {
    subject?: string;
    from?: string;
    to?: string;
    receivedAt?: Date;
    read?: boolean;
    body?: string;
    preview?: string;
    truncateTo?: number;
}

export function mailItem(fields: MailFields = {}): EasChild[] {
    const body = fields.body ?? '<html><body><p>Hello</p></body></html>';
    const clip = fields.truncateTo;
    const truncated = clip !== undefined && clip < body.length;

    return [
        E.To(fields.to ?? 'Me <me@example.com>'),
        E.From(fields.from ?? 'Sender <sender@example.com>'),
        E.Subject(fields.subject ?? 'Subject'),
        E.DateReceived(extended(fields.receivedAt ?? new Date(Date.UTC(2026, 0, 1)))),
        E.Read(fields.read ?? false),
        E.MessageClass('IPM.Note'),
        E.Importance(1),
        AB.Body(
            AB.Type(2),
            AB.EstimatedDataSize(body.length),
            ...(truncated ? [AB.Truncated(1)] : []),
            AB.Data(truncated ? body.slice(0, clip) : body),
            ...(fields.preview === undefined ? [] : [AB.Preview(fields.preview)])),
    ];
}

export interface ContactFields {
    firstName?: string;
    lastName?: string;
    company?: string;
    jobTitle?: string;
    email?: string;
    mobile?: string;
    homeStreet?: string;
    homeCity?: string;
    nickName?: string;
    birthday?: Date;
    categories?: string[];
    // base64, the form the wire actually carries. an empty string is the explicit clear
    picture?: string;
    annotations?: Record<string, string>;
}

export function contactItem(fields: ContactFields = {}): EasChild[] {
    return [
        ...(fields.firstName === undefined ? [] : [CT.FirstName(fields.firstName)]),
        ...(fields.lastName === undefined ? [] : [CT.LastName(fields.lastName)]),
        ...(fields.company === undefined ? [] : [CT.CompanyName(fields.company)]),
        ...(fields.jobTitle === undefined ? [] : [CT.JobTitle(fields.jobTitle)]),
        ...(fields.email === undefined ? [] : [CT.Email1Address(fields.email)]),
        ...(fields.mobile === undefined ? [] : [CT.MobilePhoneNumber(fields.mobile)]),
        ...(fields.homeStreet === undefined ? [] : [CT.HomeAddressStreet(fields.homeStreet)]),
        ...(fields.homeCity === undefined ? [] : [CT.HomeAddressCity(fields.homeCity)]),
        ...(fields.birthday === undefined ? [] : [CT.Birthday(extended(fields.birthday))]),
        ...(fields.categories === undefined
            ? []
            : [CT.Categories(...fields.categories.map((name) => CT.Category(name)))]),
        ...(fields.nickName === undefined ? [] : [CT2.NickName(fields.nickName)]),
        ...(fields.picture === undefined ? [] : [CT.Picture(fields.picture)]),
        ...(fields.annotations === undefined
            ? []
            : [WL.Annotations(...Object.entries(fields.annotations).map(
                ([name, value]) => WL.Annotation(WL.Name(name), WL.Value(value))))]),
    ];
}

export interface EventFields {
    subject?: string;
    location?: string;
    startAt?: Date;
    endAt?: Date;
    allDay?: boolean;
    busyStatus?: number;
    uid?: string;
    organizer?: { name: string; email: string };
    attendees?: { name: string; email: string; status?: number; type?: number }[];
    recurrence?: { type: number; interval?: number; occurrences?: number; until?: Date };
    reminder?: number;
    timezone?: string;
}

// calendar dates go out in the compact form, tasks and contacts don't
export function eventItem(fields: EventFields = {}): EasChild[] {
    const start = fields.startAt ?? new Date(Date.UTC(2026, 2, 3, 9, 0, 0));
    const end = fields.endAt ?? new Date(Date.UTC(2026, 2, 3, 10, 0, 0));
    const recurrence = fields.recurrence;

    return [
        ...(fields.timezone === undefined ? [] : [C.Timezone(fields.timezone)]),
        C.DtStamp(compact(new Date(Date.UTC(2026, 2, 1)))),
        C.StartTime(compact(start)),
        C.EndTime(compact(end)),
        C.Subject(fields.subject ?? 'Meeting'),
        ...(fields.location === undefined ? [] : [C.Location(fields.location)]),
        C.UID(fields.uid ?? 'uid-1'),
        C.AllDayEvent(fields.allDay === true ? 1 : 0),
        C.BusyStatus(fields.busyStatus ?? 2),
        C.Sensitivity(0),
        ...(fields.reminder === undefined ? [] : [C.Reminder(fields.reminder)]),
        ...(fields.organizer === undefined
            ? []
            : [C.OrganizerName(fields.organizer.name), C.OrganizerEmail(fields.organizer.email)]),
        ...(fields.attendees === undefined ? [] : [C.Attendees(...fields.attendees.map((a) =>
            C.Attendee(
                C.Name(a.name),
                C.Email(a.email),
                C.AttendeeStatus(a.status ?? 0),
                C.AttendeeType(a.type ?? 1))))]),
        ...(recurrence === undefined ? [] : [C.Recurrence(
            C.Type(recurrence.type),
            ...(recurrence.interval === undefined ? [] : [C.Interval(recurrence.interval)]),
            ...(recurrence.occurrences === undefined ? [] : [C.Occurrences(recurrence.occurrences)]),
            ...(recurrence.until === undefined ? [] : [C.Until(compact(recurrence.until))]))]),
    ];
}

export interface TaskFields {
    subject?: string;
    complete?: boolean;
    dueAt?: Date;
    startAt?: Date;
    importance?: number;
    reminderAt?: Date;
    recurrence?: { type: number; interval?: number; regenerate?: boolean };
}

export function taskItem(fields: TaskFields = {}): EasChild[] {
    const recurrence = fields.recurrence;

    return [
        T.Subject(fields.subject ?? 'Task'),
        T.Importance(fields.importance ?? 1),
        T.Complete(fields.complete === true ? 1 : 0),
        ...(fields.complete === true
            ? [T.DateCompleted(extended(new Date(Date.UTC(2026, 3, 1))))]
            : []),
        ...(fields.startAt === undefined ? [] : [T.StartDate(extended(fields.startAt))]),
        ...(fields.dueAt === undefined ? [] : [T.DueDate(extended(fields.dueAt))]),
        ...(fields.reminderAt === undefined
            ? []
            : [T.ReminderSet(1), T.ReminderTime(extended(fields.reminderAt))]),
        T.Sensitivity(0),
        ...(recurrence === undefined ? [] : [T.Recurrence(
            T.Type(recurrence.type),
            ...(recurrence.interval === undefined ? [] : [T.Interval(recurrence.interval)]),
            ...(recurrence.regenerate === undefined
                ? []
                : [T.Regenerate(recurrence.regenerate)]))]),
    ];
}

export function messages(mailbox: FakeMailbox, folderId: string, count: number): void {
    for (let i = 1; i <= count; i++)
        mailbox.addItem({
            id: `m${i}`,
            folderId,
            data: mailItem({
                subject: `Message ${i}`,
                from: `Sender ${i} <sender${i}@example.com>`,
                receivedAt: new Date(Date.UTC(2026, 0, i)),
                body: `<html><body><p>Body of message ${i}</p></body></html>`,
            }),
        });
}
