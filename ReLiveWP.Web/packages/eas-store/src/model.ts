export const ITEM_CLASSES = ['Email', 'Contact', 'Calendar', 'Task', 'Note'] as const;

export type ItemClass = (typeof ITEM_CLASSES)[number];

export type FolderRole =
    | 'inbox'
    | 'drafts'
    | 'sent'
    | 'deleted'
    | 'outbox'
    | 'junk'
    | 'calendar'
    | 'contacts'
    | 'tasks'
    | 'notes'
    | 'user';

export interface Folder {
    id: string;
    parentId: string | null;
    name: string;
    type: number;
    role: FolderRole;
    class: ItemClass | null;
}

export interface Address {
    name: string | null;
    email: string;
}

export type BodyType = 'text' | 'html';

export interface Body {
    type: BodyType;
    content: string;
    truncated: boolean;
    fullSize: number | null;
}

export interface AttachmentRef {
    id: string;
    name: string;
    size: number;
    contentType: string | null;
    inline: boolean;
    contentId: string | null;
}

export type Importance = 'low' | 'normal' | 'high';

export interface Message {
    id: string;
    folderId: string;
    receivedAt: number;
    from: Address | null;
    sender: Address | null;
    to: Address[];
    cc: Address[];
    replyTo: Address[];
    subject: string;
    preview: string;
    read: boolean;
    flagged: boolean;
    importance: Importance;
    messageClass: string;
    conversationId: string | null;
    threadIndex: string | null;
    attachments: AttachmentRef[];
    body: Body | null;
    isMeetingRequest: boolean;
}

export interface LabelledValue {
    label: string;
    value: string;
}

export interface PostalAddress {
    label: string;
    street: string | null;
    city: string | null;
    state: string | null;
    postalCode: string | null;
    country: string | null;
}

export interface ContactAnnotation {
    cid: string | null;
    objectId: string | null;
    wlid: string | null;
    imMri: string | null;
    type: string | null;
    userTileUrl: string | null;
    userTileHash: string | null;
    trustLevel: number | null;
    favouriteOrder: number | null;
}

export interface Contact {
    id: string;
    folderId: string;
    displayName: string;
    sortName: string;
    firstName: string | null;
    middleName: string | null;
    lastName: string | null;
    nickname: string | null;
    company: string | null;
    jobTitle: string | null;
    department: string | null;
    officeLocation: string | null;
    emails: LabelledValue[];
    phones: LabelledValue[];
    imAddresses: LabelledValue[];
    addresses: PostalAddress[];
    webPage: string | null;
    birthday: number | null;
    anniversary: number | null;
    categories: string[];
    notes: Body | null;
    annotation: ContactAnnotation | null;
}

export function isMeContact(contact: Contact): boolean {
    return contact.annotation?.type?.toLowerCase() === 'me';
}

export function isFavourite(contact: Contact): boolean {
    return contact.annotation?.favouriteOrder != null;
}

export type BusyStatus = 'free' | 'tentative' | 'busy' | 'outOfOffice' | 'workingElsewhere';

export type Sensitivity = 'normal' | 'personal' | 'private' | 'confidential';

export type AttendeeStatus = 'unknown' | 'tentative' | 'accepted' | 'declined' | 'notResponded';

export type AttendeeType = 'unknown' | 'required' | 'optional' | 'resource';

export interface Attendee {
    name: string | null;
    email: string;
    status: AttendeeStatus;
    type: AttendeeType;
}

export interface Recurrence {
    type: number;
    interval: number | null;
    until: number | null;
    occurrences: number | null;
    dayOfWeek: number | null;
    dayOfMonth: number | null;
    weekOfMonth: number | null;
    monthOfYear: number | null;
    firstDayOfWeek: number | null;
    calendarType: number | null;
    isLeapMonth: boolean | null;
    // regenerate, deadOccur and startAt only show up on task recurrences
    regenerate: boolean | null;
    deadOccur: boolean | null;
    startAt: number | null;
}

export interface EventException {
    deleted: boolean;
    exceptionStartAt: number | null;
    subject: string | null;
    location: string | null;
    startAt: number | null;
    endAt: number | null;
    allDay: boolean | null;
}

export interface Event {
    id: string;
    folderId: string;
    uid: string | null;
    subject: string;
    location: string | null;
    startAt: number;
    endAt: number;
    allDay: boolean;
    busy: BusyStatus;
    sensitivity: Sensitivity;
    reminderMinutes: number | null;
    timezone: string | null;
    dtStamp: number | null;
    meetingStatus: number | null;
    organizer: Address | null;
    attendees: Attendee[];
    recurrence: Recurrence | null;
    exceptions: EventException[];
    categories: string[];
    body: Body | null;
}

export interface Task {
    id: string;
    folderId: string;
    subject: string;
    complete: boolean;
    completedAt: number | null;
    startAt: number | null;
    dueAt: number | null;
    utcStartAt: number | null;
    utcDueAt: number | null;
    importance: Importance;
    sensitivity: Sensitivity;
    reminderAt: number | null;
    reminderSet: boolean;
    recurrence: Recurrence | null;
    ordinalDate: number | null;
    subOrdinalDate: string | null;
    categories: string[];
    body: Body | null;
}

export type Item = Message | Contact | Event | Task;

export interface CursorOptions {
    windowSize: number;
    bodyType: BodyType;
    truncationSize: number | null;
    filterType: number | null;
    conversationMode: boolean;
    // live annotation names to subscribe to, null on a server that has not opted in
    annotations: string[] | null;
}

export interface Cursor {
    folderId: string;
    class: ItemClass;
    syncKey: string;
    primed: boolean;
    moreAvailable: boolean;
    supportedHash: string | null;
    options: CursorOptions;
    lastSyncedAt: number | null;
    lastError: string | null;
}

export interface PolicySettings {
    allowHtmlEmail: boolean;
    maxEmailBodyTruncationSize: number | null;
    maxEmailHtmlBodyTruncationSize: number | null;
    maxAttachmentSize: number | null;
    maxEmailAgeFilter: number | null;
    maxCalendarAgeFilter: number | null;
}

export interface Account {
    userId: string;
    deviceId: string;
    deviceType: string;
    endpoint: string | null;
    protocolVersion: string | null;
    policyKey: number | null;
    provisionedAt: number | null;
    folderSyncKey: string;
    heartbeatInterval: number | null;
    maxFolders: number | null;
    policy: PolicySettings | null;
}

export interface Lease {
    holderId: string;
    renewedAt: number;
}
