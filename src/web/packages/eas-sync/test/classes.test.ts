import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { decode, tags, textOf, type EasNode } from '@relivewp/eas-client/nodes';
import { MemoryStore } from '@relivewp/eas-store';

import {
    CALENDAR_REQUIRED_SUPPORTED,
    CALENDAR_SUPPORTED,
    CONTACT_SUPPORTED,
    LIVE_ANNOTATIONS,
    supportedFor,
} from '../src/index.ts';
import { harness } from './harness.ts';
import { calendarFolder, contactsFolder, tasksFolder } from './server/server.ts';
import { contactItem, eventItem, taskItem } from './server/items.ts';

const { AirSync: A } = tags;

function names(nodes: EasNode[]): string[] {
    return nodes.map((node) => node.name);
}

describe('the Supported set', () => {
    // leave one out and the server can reject the prime outright
    it('carries every element the specification makes mandatory for Calendar', () => {
        const present = new Set(names(CALENDAR_SUPPORTED));

        for (const required of CALENDAR_REQUIRED_SUPPORTED)
            assert.ok(present.has(required), `Supported is missing calendar:${required}`);
    });

    it('names contact properties and nothing from AirSyncBase', () => {
        assert.ok(CONTACT_SUPPORTED.length > 40);
        for (const node of CONTACT_SUPPORTED)
            assert.ok(node.ns === 'Contacts' || node.ns === 'Contacts2',
                `${node.ns}:${node.name} is not a contact property`);
    });

    it('is sent for the two classes that can be ghosted and no others', () => {
        assert.notEqual(supportedFor('Contact'), undefined);
        assert.notEqual(supportedFor('Calendar'), undefined);
        assert.equal(supportedFor('Email'), undefined);
        assert.equal(supportedFor('Task'), undefined);
    });
});

describe('priming a collection', () => {
    async function primedRequest(folder: ReturnType<typeof contactsFolder>) {
        const { engine, server } = harness({ seedInbox: false });
        server.mailbox.addFolder(folder);
        await engine.synchronise();

        const bodies = server.sent.filter((entry) => entry.command === 'Sync');
        return bodies.map((entry) => decode(entry.body!).root);
    }

    it('sends Supported on the SyncKey 0 request and never again', async () => {
        const requests = await primedRequest(contactsFolder());
        const collections = requests.map((root) => {
            const collection = root.children[0] as EasNode;
            return collection.children[0] as EasNode;
        });

        const withSupported = collections.filter((collection) =>
            collection.children.some((child) => 'name' in child && child.name === 'Supported'));

        assert.equal(withSupported.length, 1, 'Supported must be sent exactly once');
        const syncKey = withSupported[0]!.children.find(
            (child): child is EasNode => 'name' in child && child.name === 'SyncKey');
        assert.equal(textOf(syncKey!), '0');
    });

    it('puts CollectionId before Supported, which the server enforces', async () => {
        const requests = await primedRequest(contactsFolder());
        const collection = (requests[0]!.children[0] as EasNode).children[0] as EasNode;
        const order = collection.children
            .filter((child): child is EasNode => 'name' in child)
            .map((child) => child.name);

        assert.ok(order.indexOf('CollectionId') < order.indexOf('Supported'), order.join(', '));
    });

    it('sends no Supported for a class that cannot be ghosted', async () => {
        const requests = await primedRequest(tasksFolder());
        for (const root of requests) {
            const collection = (root.children[0] as EasNode).children[0] as EasNode;
            assert.ok(!collection.children.some(
                (child) => 'name' in child && child.name === 'Supported'));
        }
    });
});

describe('contacts end to end', () => {
    it('syncs, projects and lists them', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addItem({
            id: 'c1',
            folderId: 'contacts',
            data: contactItem({
                firstName: 'Ada', lastName: 'Lovelace', company: 'Analytical Engines',
                jobTitle: 'Mathematician', email: 'ada@example.com', mobile: '+44 7700 900123',
                homeStreet: '12 Bell Lane', homeCity: 'London', nickName: 'Countess',
                birthday: new Date(Date.UTC(1815, 11, 10)), categories: ['Friends', 'Work'],
            }),
        });
        server.mailbox.addItem({ id: 'c2', folderId: 'contacts', data: contactItem({ company: 'Acme' }) });

        const report = await engine.synchronise();
        assert.equal(report.recovery.kind, 'ok');
        assert.equal(report.folders[0]?.delivered, 2);

        const contacts = await store.listContacts({ folderId: 'contacts', limit: 10 });
        // sorted by surname, so a company-only contact files under its company
        assert.deepEqual(contacts.map((c) => c.id), ['c2', 'c1']);

        const ada = contacts[1]!;
        assert.equal(ada.displayName, 'Ada Lovelace');
        assert.equal(ada.sortName, 'lovelace ada');
        assert.equal(ada.nickname, 'Countess');
        assert.equal(ada.company, 'Analytical Engines');
        assert.deepEqual(ada.emails, [{ label: 'email1', value: 'ada@example.com' }]);
        assert.deepEqual(ada.phones, [{ label: 'mobile', value: '+44 7700 900123' }]);
        assert.deepEqual(ada.addresses, [{
            label: 'home', street: '12 Bell Lane', city: 'London',
            state: null, postalCode: null, country: null,
        }]);
        assert.deepEqual(ada.categories, ['Friends', 'Work']);
        assert.equal(ada.birthday, Date.UTC(1815, 11, 10));

        assert.equal(contacts[0]?.displayName, 'Acme');
        assert.equal(contacts[0]?.sortName, 'acme');
    });

    it('finds them by name and by phone number', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addItem({
            id: 'c1',
            folderId: 'contacts',
            data: contactItem({ firstName: 'Ada', lastName: 'Lovelace', mobile: '+44 7700 900123' }),
        });
        await engine.synchronise();

        assert.equal((await store.searchContacts({ text: 'lovelace', limit: 5 })).length, 1);
        assert.equal((await store.searchContacts({ text: '7700', limit: 5 })).length, 1);
        assert.equal((await store.searchContacts({ text: 'babbage', limit: 5 })).length, 0);
    });
});

describe('live annotations, which a stock server may reject', () => {
    function optionsOf(server: ReturnType<typeof harness>['server']): EasNode[] {
        return server.sent
            .filter((entry) => entry.command === 'Sync')
            .map((entry) => decode(entry.body!).root)
            .flatMap((root) => {
                const collection = (root.children[0] as EasNode).children[0] as EasNode;
                return collection.children.filter(
                    (child): child is EasNode => 'name' in child && child.name === 'Options');
            });
    }

    const asks = (options: EasNode) => options.children.some(
        (child) => 'name' in child && child.ns === 'WindowsLive' && child.name === 'Annotations');

    it('asks for nothing unless the deployment opts in', async () => {
        const { engine, server } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        await engine.synchronise();

        const options = optionsOf(server);
        assert.ok(options.length > 0);
        assert.ok(!options.some(asks), 'a client that did not opt in must not ask for annotations');
    });

    it('asks on every request once opted in, because a bare re-init would clear the cache', async () => {
        const { engine, server } = harness({
            seedInbox: false,
            engine: { annotations: LIVE_ANNOTATIONS },
        });
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addItem({ id: 'c1', folderId: 'contacts', data: contactItem({ firstName: 'Ada' }) });
        await engine.synchronise();

        const options = optionsOf(server);
        assert.ok(options.length > 1);
        assert.ok(options.every(asks), 'every Sync must carry the subscription, not only the prime');
    });

    it('names each annotation without a Value, which is what makes it a subscription', async () => {
        const { engine, server } = harness({
            seedInbox: false,
            engine: { annotations: ['Type', 'FavoriteOrder'] },
        });
        server.mailbox.addFolder(contactsFolder());
        await engine.synchronise();

        const block = optionsOf(server)[0]!.children.find(
            (child): child is EasNode => 'name' in child && child.name === 'Annotations')!;

        assert.deepEqual(
            block.children.map((child) => names([child as EasNode])).flat(),
            ['Annotation', 'Annotation']);

        for (const entry of block.children as EasNode[])
            assert.deepEqual(names(entry.children as EasNode[]), ['Name']);
    });

    it('asks only for the contacts collection', async () => {
        const { engine, server } = harness({ engine: { annotations: LIVE_ANNOTATIONS } });
        server.mailbox.addFolder(tasksFolder());
        await engine.synchronise();

        const perFolder = server.sent
            .filter((entry) => entry.command === 'Sync')
            .map((entry) => decode(entry.body!).root)
            .map((root) => {
                const collection = (root.children[0] as EasNode).children[0] as EasNode;
                const id = collection.children.find(
                    (child): child is EasNode => 'name' in child && child.name === 'CollectionId');
                const options = collection.children.find(
                    (child): child is EasNode => 'name' in child && child.name === 'Options');
                return { folderId: textOf(id!), asked: options !== undefined && asks(options) };
            });

        for (const entry of perFolder)
            assert.equal(entry.asked, entry.folderId === 'contacts', entry.folderId);
    });

    it('re-primes the collection when the requested set changes', async () => {
        const store = new MemoryStore({ userId: 'u', deviceId: 'D1', deviceType: 'Browser' });

        const first = harness({ seedInbox: false, store });
        first.server.mailbox.addFolder(contactsFolder());
        first.server.mailbox.addItem({
            id: 'c1', folderId: 'contacts', data: contactItem({ firstName: 'Ada' }),
        });
        await first.engine.synchronise();
        assert.equal((await store.cursor('contacts'))?.primed, true);

        const second = harness({
            seedInbox: false,
            store,
            engine: { annotations: LIVE_ANNOTATIONS },
        });
        second.server.mailbox.addFolder(contactsFolder());
        second.server.mailbox.addItem({
            id: 'c1',
            folderId: 'contacts',
            data: contactItem({ firstName: 'Ada', annotations: { Type: 'Me' } }),
        });
        await second.engine.synchronise();

        assert.deepEqual((await store.cursor('contacts'))?.options.annotations, [...LIVE_ANNOTATIONS].sort());
        assert.equal((await store.contact('contacts', 'c1'))?.annotation?.type, 'Me');
    });

    it('does not re-prime a client that never asked, on a cursor written before the field existed', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        await engine.synchronise();

        const before = (await store.cursor('contacts'))!;
        const { annotations, ...legacy } = before.options;
        await store.replaceCursor({
            folderId: 'contacts',
            class: 'Contact',
            options: legacy as typeof before.options,
            supportedHash: before.supportedHash,
        });
        await engine.syncFolder('contacts');
        const primedOnce = await store.cursor('contacts');

        await engine.syncFolder('contacts');
        assert.equal((await store.cursor('contacts'))?.syncKey, primedOnce?.syncKey);
    });
});

describe('contact pictures end to end', () => {
    // /9j/4A== is the four byte JPEG opening
    it('stores the bytes as a blob and clears them on an empty Picture', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addItem({
            id: 'c1',
            folderId: 'contacts',
            data: contactItem({ firstName: 'Ada', picture: '/9j/4A==' }),
        });
        await engine.synchronise();

        const photo = await store.contactPhoto('contacts', 'c1');
        assert.equal(photo?.size, 4);
        assert.equal(photo?.type, 'image/jpeg');

        server.mailbox.changeItem('contacts', 'c1', contactItem({ firstName: 'Ada', picture: '' }));
        await engine.syncFolder('contacts');

        assert.equal(await store.contactPhoto('contacts', 'c1'), undefined);
    });

    it('leaves a stored picture alone when a later change omits it', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addItem({
            id: 'c1',
            folderId: 'contacts',
            data: contactItem({ firstName: 'Ada', picture: '/9j/4A==' }),
        });
        await engine.synchronise();

        server.mailbox.changeItem('contacts', 'c1', contactItem({ firstName: 'Adelaide' }));
        await engine.syncFolder('contacts');

        assert.equal((await store.contact('contacts', 'c1'))?.firstName, 'Adelaide');
        assert.equal((await store.contactPhoto('contacts', 'c1'))?.size, 4);
    });
});

describe('calendar end to end', () => {
    it('reads compact dates, attendees and a recurrence', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(calendarFolder());
        server.mailbox.addItem({
            id: 'e1',
            folderId: 'calendar',
            data: eventItem({
                subject: 'Weekly sync',
                location: 'Room 3',
                uid: 'uid-weekly',
                startAt: new Date(Date.UTC(2026, 2, 3, 9, 0, 0)),
                endAt: new Date(Date.UTC(2026, 2, 3, 10, 0, 0)),
                reminder: 15,
                organizer: { name: 'Ada', email: 'ada@example.com' },
                attendees: [{ name: 'Bob', email: 'bob@example.com', status: 3, type: 1 }],
                recurrence: { type: 1, interval: 1, occurrences: 10 },
            }),
        });

        const report = await engine.synchronise();
        assert.equal(report.recovery.kind, 'ok');

        const event = await store.event('calendar', 'e1');
        assert.equal(event?.subject, 'Weekly sync');
        assert.equal(event?.location, 'Room 3');
        assert.equal(event?.uid, 'uid-weekly');
        assert.equal(event?.startAt, Date.UTC(2026, 2, 3, 9, 0, 0));
        assert.equal(event?.endAt, Date.UTC(2026, 2, 3, 10, 0, 0));
        assert.equal(event?.allDay, false);
        assert.equal(event?.busy, 'busy');
        assert.equal(event?.reminderMinutes, 15);
        assert.deepEqual(event?.organizer, { name: 'Ada', email: 'ada@example.com' });
        assert.deepEqual(event?.attendees, [
            { name: 'Bob', email: 'bob@example.com', status: 'accepted', type: 'required' },
        ]);
        assert.equal(event?.recurrence?.type, 1);
        assert.equal(event?.recurrence?.interval, 1);
        assert.equal(event?.recurrence?.occurrences, 10);
    });

    it('returns only the events overlapping the requested window', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(calendarFolder());

        for (const [id, day] of [['e1', 3], ['e2', 10], ['e3', 20]] as const)
            server.mailbox.addItem({
                id,
                folderId: 'calendar',
                data: eventItem({
                    subject: id,
                    uid: id,
                    startAt: new Date(Date.UTC(2026, 2, day, 9, 0, 0)),
                    endAt: new Date(Date.UTC(2026, 2, day, 10, 0, 0)),
                }),
            });

        await engine.synchronise();

        const week = await store.listEvents({
            folderId: 'calendar',
            limit: 50,
            from: Date.UTC(2026, 2, 8),
            to: Date.UTC(2026, 2, 15),
        });

        assert.deepEqual(week.map((e) => e.id), ['e2']);
        assert.equal((await store.listEvents({ folderId: 'calendar', limit: 50 })).length, 3);
    });
});

describe('tasks end to end', () => {
    it('syncs, orders by due date and hides completed ones by default', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(tasksFolder());

        server.mailbox.addItem({
            id: 't1',
            folderId: 'tasks',
            data: taskItem({ subject: 'Later', dueAt: new Date(Date.UTC(2026, 4, 20)) }),
        });
        server.mailbox.addItem({
            id: 't2',
            folderId: 'tasks',
            data: taskItem({
                subject: 'Sooner',
                dueAt: new Date(Date.UTC(2026, 4, 1)),
                importance: 2,
                reminderAt: new Date(Date.UTC(2026, 3, 30, 9, 0, 0)),
                recurrence: { type: 1, interval: 2, regenerate: true },
            }),
        });
        server.mailbox.addItem({
            id: 't3',
            folderId: 'tasks',
            data: taskItem({ subject: 'Done', complete: true }),
        });

        const report = await engine.synchronise();
        assert.equal(report.recovery.kind, 'ok');
        assert.equal(report.folders[0]?.delivered, 3);

        const open = await store.listTasks({ folderId: 'tasks', limit: 10 });
        assert.deepEqual(open.map((t) => t.id), ['t2', 't1']);

        const sooner = open[0]!;
        assert.equal(sooner.subject, 'Sooner');
        assert.equal(sooner.importance, 'high');
        assert.equal(sooner.dueAt, Date.UTC(2026, 4, 1));
        assert.equal(sooner.reminderSet, true);
        assert.equal(sooner.reminderAt, Date.UTC(2026, 3, 30, 9, 0, 0));
        assert.equal(sooner.recurrence?.regenerate, true);
        assert.equal(sooner.recurrence?.interval, 2);

        const all = await store.listTasks({ folderId: 'tasks', limit: 10, includeComplete: true });
        assert.equal(all.length, 3);
        assert.equal((await store.task('tasks', 't3'))?.complete, true);
    });
});

describe('all four classes together', () => {
    it('primes each folder with its own class and keeps the items apart', async () => {
        const { engine, server, store } = harness();
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addFolder(calendarFolder());
        server.mailbox.addFolder(tasksFolder());

        server.mailbox.addItem({ id: 'x', folderId: 'contacts', data: contactItem({ firstName: 'Ada' }) });
        server.mailbox.addItem({ id: 'x', folderId: 'calendar', data: eventItem({ uid: 'u' }) });
        server.mailbox.addItem({ id: 'x', folderId: 'tasks', data: taskItem({}) });

        const report = await engine.synchronise();

        assert.equal(report.recovery.kind, 'ok');
        assert.deepEqual(
            report.folders.map((outcome) => outcome.folderId).sort(),
            ['calendar', 'contacts', 'inbox', 'tasks']);

        const cursors = await store.cursors();
        assert.deepEqual(
            Object.fromEntries(cursors.map((cursor) => [cursor.folderId, cursor.class])),
            { inbox: 'Email', contacts: 'Contact', calendar: 'Calendar', tasks: 'Task' });

        // same server id in three collections, three different things
        assert.equal((await store.contact('contacts', 'x'))?.firstName, 'Ada');
        assert.equal((await store.event('calendar', 'x'))?.uid, 'u');
        assert.equal((await store.task('tasks', 'x'))?.subject, 'Task');
        assert.equal(await store.message('inbox', 'x'), undefined);
    });

    it('re-primes a collection whose Supported set no longer matches', async () => {
        const { engine, server, store } = harness({ seedInbox: false });
        server.mailbox.addFolder(contactsFolder());
        server.mailbox.addItem({ id: 'c1', folderId: 'contacts', data: contactItem({ firstName: 'Ada' }) });
        await engine.synchronise();

        const before = await store.cursor('contacts');
        assert.notEqual(before?.supportedHash, null);
        assert.equal(before?.primed, true);

        await store.replaceCursor({
            folderId: 'contacts',
            class: 'Contact',
            options: before!.options,
            supportedHash: 'from-an-older-model',
        });

        await engine.syncFolder('contacts');

        const after = await store.cursor('contacts');
        assert.equal(after?.supportedHash, before?.supportedHash);
        assert.equal(after?.primed, true);
        assert.equal((await store.contact('contacts', 'c1'))?.firstName, 'Ada');
    });
});
