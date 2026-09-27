import type { Contact, CursorInit, Event, Folder, Message, Task } from '../src/index.ts';

export function folder(id: string, patch: Partial<Folder> = {}): Folder {
    return { id, parentId: null, name: id, type: 2, role: 'inbox', class: 'Email', ...patch };
}

export function message(id: string, patch: Partial<Message> = {}): Message {
    return {
        id,
        folderId: 'inbox',
        receivedAt: 0,
        from: null,
        sender: null,
        to: [],
        cc: [],
        replyTo: [],
        subject: '',
        preview: '',
        read: false,
        flagged: false,
        importance: 'normal',
        messageClass: 'IPM.Note',
        conversationId: null,
        threadIndex: null,
        attachments: [],
        body: null,
        isMeetingRequest: false,
        ...patch,
    };
}

export function contact(id: string, patch: Partial<Contact> = {}): Contact {
    const base: Contact = {
        id,
        folderId: 'contacts',
        displayName: id,
        sortName: id.toLowerCase(),
        firstName: null,
        middleName: null,
        lastName: null,
        nickname: null,
        company: null,
        jobTitle: null,
        department: null,
        officeLocation: null,
        emails: [],
        phones: [],
        imAddresses: [],
        addresses: [],
        webPage: null,
        birthday: null,
        anniversary: null,
        categories: [],
        notes: null,
        annotation: null,
    };

    return { ...base, ...patch };
}

export function event(id: string, patch: Partial<Event> = {}): Event {
    const base: Event = {
        id,
        folderId: 'calendar',
        uid: null,
        subject: id,
        location: null,
        startAt: 0,
        endAt: 0,
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
    };

    return { ...base, ...patch };
}

export function task(id: string, patch: Partial<Task> = {}): Task {
    const base: Task = {
        id,
        folderId: 'tasks',
        subject: id,
        complete: false,
        completedAt: null,
        startAt: null,
        dueAt: null,
        utcStartAt: null,
        utcDueAt: null,
        importance: 'normal',
        sensitivity: 'normal',
        reminderAt: null,
        reminderSet: false,
        recurrence: null,
        ordinalDate: null,
        subOrdinalDate: null,
        categories: [],
        body: null,
    };

    return { ...base, ...patch };
}

export function cursorInit(folderId: string, patch: Partial<CursorInit> = {}): CursorInit {
    return {
        folderId,
        class: 'Email',
        supportedHash: null,
        options: {
            windowSize: 100,
            bodyType: 'html',
            truncationSize: 4096,
            filterType: null,
            conversationMode: false,
            annotations: null,
        },
        ...patch,
    };
}
