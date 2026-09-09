import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import type { Contact, ContactAnnotation, LabelledValue } from '@relivewp/eas-store';

import {
    aggregateContacts, aggregateIndex, aggregatesFor, autoLinkingName, canLink, liveMemberOf,
    normalizeName, normalizePhone, pairKey, phonesMatch, photoOf, searchLinks, suggestLinks,
    withLinked, withoutMember,
    type Aggregate,
} from '../src/views/people/aggregate.ts';

type Patch = Partial<Omit<Contact, 'emails' | 'phones' | 'annotation'>> & {
    emails?: string[],
    phones?: string[],
    cid?: string | null,
    me?: boolean,
    origin?: string,
};

function labelled(values: string[] | undefined, label: string): LabelledValue[] {
    return (values ?? []).map((value) => ({ label, value }));
}

function contact(id: string, patch: Patch = {}): Contact {
    const { emails, phones, cid, me, origin, ...rest } = patch;

    const annotation: ContactAnnotation | null = cid === undefined && me !== true && origin === undefined ? null : {
        cid: cid ?? null, objectId: null, wlid: null, imMri: null, type: me === true ? 'Me' : null,
        userTileUrl: null, userTileHash: null, trustLevel: null, favouriteOrder: null,
        originService: origin ?? null, originCollection: origin === undefined ? null : 'default',
    };

    const first = rest.firstName ?? null;
    const last = rest.lastName ?? null;
    const displayName = rest.displayName ?? [first, last].filter((part) => part !== null).join(' ');

    return {
        id,
        folderId: 'contacts',
        displayName: displayName.length > 0 ? displayName : id,
        sortName: rest.sortName ?? [last, first].filter((part) => part !== null).join(' ').toLowerCase(),
        firstName: first, middleName: null, lastName: last, nickname: null,
        company: null, jobTitle: null, department: null, officeLocation: null,
        emails: labelled(emails, 'email1'),
        phones: labelled(phones, 'mobile'),
        imAddresses: [], addresses: [],
        webPage: null, birthday: null, anniversary: null,
        categories: [], notes: null, annotation,
        ...rest,
    };
}

function partition(aggregates: Aggregate[]): string[][] {
    return aggregates
        .map((aggregate) => aggregate.members.map((member) => member.id).sort())
        .sort((a, b) => a[0]!.localeCompare(b[0]!));
}

describe('normalising for comparison', () => {
    it('folds case, diacritics, punctuation and runs of whitespace', () => {
        assert.equal(normalizeName("  José  O'Brien-Smith "), 'jose obriensmith');
        assert.equal(normalizeName('JOSÉ'), 'jose');
    });

    it('keeps only digits of a phone number and matches on the tail', () => {
        assert.equal(normalizePhone('+44 (0)7700 900123'), '4407700900123');
        assert.ok(phonesMatch(normalizePhone('+44 7700 900123'), normalizePhone('07700 900123')));
        assert.ok(!phonesMatch('900123', '07700900123'), 'six digits is too short to trust');
        assert.ok(!phonesMatch('07700900124', '07700900123'));
    });
});

describe('the auto-linking name', () => {
    it('is the full name when any name part exists', () => {
        assert.deepEqual(autoLinkingName(contact('a', { firstName: 'Ada', middleName: 'K', lastName: 'Lovelace' })),
            { text: 'Ada K Lovelace', source: 'name' });
        assert.deepEqual(autoLinkingName(contact('b', { firstName: 'Ada' })), { text: 'Ada', source: 'name' });
    });

    it('falls through nickname, company, email, phone, display name in that order', () => {
        assert.deepEqual(autoLinkingName(contact('a', { nickname: 'ada', company: 'Analytical' })),
            { text: 'ada', source: 'nickname' });
        assert.deepEqual(autoLinkingName(contact('b', { company: 'Analytical', emails: ['a@x.org'] })),
            { text: 'Analytical', source: 'company' });
        assert.deepEqual(autoLinkingName(contact('c', { emails: ['a@x.org'], phones: ['1'] })),
            { text: 'a@x.org', source: 'email' });
        assert.deepEqual(autoLinkingName(contact('d', { phones: ['07700 900123'] })),
            { text: '07700 900123', source: 'phone' });
        assert.deepEqual(autoLinkingName(contact('e', { displayName: 'Unknown' })),
            { text: 'Unknown', source: 'display' });
    });

    it('prefers a mobile number over a work one regardless of list order', () => {
        const mixed = contact('a');
        mixed.phones = [{ label: 'work', value: '111 1111' }, { label: 'mobile', value: '222 2222' }];

        assert.deepEqual(autoLinkingName(mixed), { text: '222 2222', source: 'phone' });
    });
});

describe('automatic linking', () => {
    it('merges an exact first and last name match', () => {
        const aggregates = aggregateContacts([
            contact('google', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('carddav', { firstName: 'ada', lastName: 'LOVELACE' }),
            contact('other', { firstName: 'Charles', lastName: 'Babbage' }),
        ]);

        assert.deepEqual(partition(aggregates), [['carddav', 'google'], ['other']]);
    });

    it('refuses to pick between two existing candidates', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'John', lastName: 'Smith', emails: ['john@a.org'], cid: 'aaaa' }),
            contact('b', { firstName: 'John', lastName: 'Smith', emails: ['john@b.org'], cid: 'bbbb' }),
            contact('c', { firstName: 'John', lastName: 'Smith', emails: ['john@a.org'] }),
        ]);

        // the cids keep a and b apart, so c sees two candidates and links nothing, even though it
        // shares an address with one of them
        assert.deepEqual(partition(aggregates), [['a'], ['b'], ['c']]);
    });

    it('needs a shared address when middle names disagree', () => {
        const apart = aggregateContacts([
            contact('a', { firstName: 'John', middleName: 'Quincy', lastName: 'Adams' }),
            contact('b', { firstName: 'John', middleName: 'Paul', lastName: 'Adams' }),
        ]);
        assert.deepEqual(partition(apart), [['a'], ['b']]);

        const together = aggregateContacts([
            contact('a', { firstName: 'John', middleName: 'Quincy', lastName: 'Adams', emails: ['JQA@example.org'] }),
            contact('b', { firstName: 'John', middleName: 'Paul', lastName: 'Adams', emails: ['jqa@example.org'] }),
        ]);
        assert.deepEqual(partition(together), [['a', 'b']]);
    });

    it('treats a missing middle name as no conflict', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'John', middleName: 'Quincy', lastName: 'Adams' }),
            contact('b', { firstName: 'John', lastName: 'Adams' }),
        ]);

        assert.deepEqual(partition(aggregates), [['a', 'b']]);
    });

    it('needs corroboration for a single-token name and takes a phone number for it', () => {
        const apart = aggregateContacts([
            contact('a', { firstName: 'Cher' }),
            contact('b', { firstName: 'Cher' }),
        ]);
        assert.deepEqual(partition(apart), [['a'], ['b']]);

        const together = aggregateContacts([
            contact('a', { firstName: 'Cher', phones: ['+1 555 010 0199'] }),
            contact('b', { firstName: 'Cher', phones: ['(555) 010-0199'] }),
        ]);
        assert.deepEqual(partition(together), [['a', 'b']]);
    });

    it('links nameless contacts on a shared email address', () => {
        const aggregates = aggregateContacts([
            contact('a', { emails: ['Someone@Example.org'] }),
            contact('b', { emails: ['someone@example.org'] }),
            contact('c', { emails: ['else@example.org'] }),
        ]);

        assert.deepEqual(partition(aggregates), [['a', 'b'], ['c']]);
    });

    it('does not merge colleagues on company alone unless asked to', () => {
        const contacts = [
            contact('a', { company: 'Analytical Engines Ltd' }),
            contact('b', { company: 'Analytical Engines Ltd' }),
        ];

        assert.deepEqual(partition(aggregateContacts(contacts)), [['a'], ['b']]);
        assert.deepEqual(partition(aggregateContacts(contacts, { linkOnCompany: true })), [['a', 'b']]);
    });

    it('falls back to the auto-linking name when first and last are ambiguous', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'John', middleName: 'Quincy', lastName: 'Adams' }),
            contact('b', { firstName: 'John', middleName: 'Paul', lastName: 'Adams' }),
            contact('c', { firstName: 'John', middleName: 'Paul', lastName: 'Adams' }),
        ]);

        assert.deepEqual(partition(aggregates), [['a'], ['b', 'c']]);
    });

    it('never merges two contacts carrying different cids', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'Ada', lastName: 'Lovelace', emails: ['ada@x.org'], cid: 'aaaa' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace', emails: ['ada@x.org'], cid: 'bbbb' }),
        ]);

        assert.deepEqual(partition(aggregates), [['a'], ['b']]);
    });

    it('always merges two contacts carrying the same cid, whatever they are called', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'Ada', lastName: 'Lovelace', cid: 'ABCD' }),
            contact('b', { firstName: 'Augusta', lastName: 'King', cid: 'abcd' }),
        ]);

        assert.deepEqual(partition(aggregates), [['a', 'b']]);
    });

    it('does not let a contact with a cid join an aggregate that already holds another', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'Ada', lastName: 'Lovelace', cid: 'aaaa' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('c', { firstName: 'Ada', lastName: 'Lovelace', cid: 'cccc' }),
        ]);

        assert.deepEqual(partition(aggregates), [['a', 'b'], ['c']]);
    });

    it('leaves the me contact alone in both directions', () => {
        const aggregates = aggregateContacts([
            contact('me', { firstName: 'Ada', lastName: 'Lovelace', emails: ['ada@x.org'], me: true }),
            contact('twin', { firstName: 'Ada', lastName: 'Lovelace', emails: ['ada@x.org'] }),
        ]);

        assert.deepEqual(partition(aggregates), [['me'], ['twin']]);
    });

    it('needs corroboration for two contacts from the same account', () => {
        const contacts = [
            contact('carddav', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('google1', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('google2', { firstName: 'Ada', lastName: 'Lovelace' }),
        ];
        const sourceOf = (entry: Contact): string | null => (entry.id.startsWith('google') ? 'google' : 'carddav');

        assert.deepEqual(partition(aggregateContacts(contacts, { sourceOf })), [['carddav', 'google1'], ['google2']]);
    });

    it('reads the source off the origin annotation by default', () => {
        const aggregates = aggregateContacts([
            contact('a', { firstName: 'Ada', lastName: 'Lovelace', origin: 'google' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('c', { firstName: 'Ada', lastName: 'Lovelace', origin: 'google' }),
        ]);

        assert.deepEqual(partition(aggregates), [['a', 'b'], ['c']]);
    });

    it('treats an unknown source as no restriction', () => {
        const contacts = [
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace' }),
        ];

        assert.deepEqual(partition(aggregateContacts(contacts, { sourceOf: () => null })), [['a', 'b']]);
    });

    it('gives the same answer whatever order the contacts arrive in', () => {
        const contacts = [
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('c', { firstName: 'Cher', phones: ['555 010 0199'] }),
            contact('d', { firstName: 'Cher', phones: ['555 010 0199'] }),
            contact('e', { emails: ['e@x.org'] }),
            contact('f', { emails: ['E@X.ORG'] }),
            contact('g', { firstName: 'Grace', lastName: 'Hopper', cid: '1' }),
            contact('h', { firstName: 'Grace', lastName: 'Hopper', cid: '2' }),
        ];

        const forwards = partition(aggregateContacts(contacts));
        const backwards = partition(aggregateContacts([...contacts].reverse()));

        assert.deepEqual(forwards, backwards);
        assert.deepEqual(forwards, [['a', 'b'], ['c', 'd'], ['e', 'f'], ['g'], ['h']]);
    });

    it('honours a split over every rule, and a merge over every rule but a split', () => {
        const twins = [
            contact('a', { firstName: 'Ada', lastName: 'Lovelace', emails: ['ada@x.org'], cid: 'same' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace', emails: ['ada@x.org'], cid: 'same' }),
        ];
        assert.deepEqual(partition(aggregateContacts(twins)), [['a', 'b']]);
        assert.deepEqual(partition(aggregateContacts(twins, { overrides: new Map([[pairKey('b', 'a'), 'split']]) })), [['a'], ['b']]);

        const strangers = [
            contact('a', { firstName: 'Ada', lastName: 'Lovelace', cid: 'one' }),
            contact('b', { firstName: 'Charles', lastName: 'Babbage', cid: 'two' }),
            contact('c', { firstName: 'Charles', lastName: 'Babbage' }),
        ];
        assert.deepEqual(partition(aggregateContacts(strangers)), [['a'], ['b', 'c']]);
        assert.deepEqual(
            partition(aggregateContacts(strangers, { overrides: new Map([[pairKey('a', 'b'), 'merge']]) })),
            [['a', 'b', 'c']]);
    });

    it('keeps an unlinked profile out of the row it left', () => {
        const members = [
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('c', { firstName: 'Ada', lastName: 'Lovelace' }),
        ];
        const [row] = aggregateContacts(members);
        assert.deepEqual(row!.members.map((m) => m.id), ['a', 'b', 'c']);

        const overrides = withoutMember(new Map(), members[2]!, row!.members);
        assert.deepEqual(partition(aggregateContacts(members, { overrides })), [['a', 'b'], ['c']]);
    });

    it('collapses search hits and favourites onto their rows without losing strangers', () => {
        const a = contact('a', { firstName: 'Ada', lastName: 'Lovelace' });
        const b = contact('b', { firstName: 'Ada', lastName: 'Lovelace' });
        const stranger = contact('z', { firstName: 'Zed' });

        const aggregates = aggregateContacts([a, b]);
        const index = aggregateIndex(aggregates);

        assert.equal(index.get('b'), aggregates[0]);
        assert.deepEqual(aggregatesFor([b, a, stranger], index).map((row) => row.key), ['a', 'z']);
    });

    it('takes the first photo any member has and hangs the feed off whoever has the cid', () => {
        const [aggregate] = aggregateContacts([
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace', cid: 'abcd' }),
        ]);

        assert.equal(photoOf(aggregate!, new Map([['b', 'blob:b']])), 'blob:b');
        assert.equal(photoOf(aggregate!, new Map()), undefined);
        assert.equal(liveMemberOf(aggregate!).id, 'b');
    });

    it('suggests links on a shared address, phone, name or hyphen half, never itself or a clashing cid', () => {
        const all = aggregateContacts([
            contact('me', { firstName: 'Ada', lastName: 'Lovelace-King', emails: ['ada@x.org'], phones: ['+44 7700 900123'], cid: 'mine' }),
            contact('email', { firstName: 'A', lastName: 'L', emails: ['ADA@x.org'] }),
            contact('phone', { firstName: 'Countess', lastName: 'Lovelace', phones: ['07700 900123'] }),
            contact('half', { firstName: 'Ada', lastName: 'King' }),
            contact('clash', { firstName: 'Ada', lastName: 'Lovelace-King', cid: 'theirs' }),
            contact('nobody', { firstName: 'Charles', lastName: 'Babbage' }),
            contact('self', { firstName: 'Ada', lastName: 'Lovelace-King', me: true }),
        ]);
        const target = all.find((row) => row.key === 'me')!;

        assert.deepEqual(suggestLinks(target, all).map((row) => row.key), ['email', 'half', 'phone']);
        assert.ok(!canLink(target, all.find((row) => row.key === 'clash')!));
        assert.ok(!canLink(target, all.find((row) => row.key === 'self')!));
        assert.ok(!canLink(target, target));
    });

    it('searches everyone else by name once two characters are typed', () => {
        const all = aggregateContacts([
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('b', { firstName: 'Charles', lastName: 'Babbage' }),
            contact('c', { firstName: 'Charlotte', lastName: 'Bronte' }),
        ]);
        const target = all.find((row) => row.key === 'a')!;

        assert.deepEqual(searchLinks(target, all, 'c').map((row) => row.key), []);
        assert.deepEqual(searchLinks(target, all, 'CHAR').map((row) => row.key), ['b', 'c']);
        assert.deepEqual(searchLinks(target, all, 'babb').map((row) => row.key), ['b']);
    });

    it('linking two rows clears the splits between them and holds them with one merge', () => {
        const contacts = [
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
            contact('b', { firstName: 'Ada', lastName: 'Lovelace' }),
        ];
        const [row] = aggregateContacts(contacts);
        const apart = withoutMember(new Map(), contacts[1]!, row!.members);
        const [left, right] = aggregateContacts(contacts, { overrides: apart });
        assert.deepEqual(partition([left!, right!]), [['a'], ['b']]);

        const together = withLinked(apart, left!, right!);
        assert.deepEqual(partition(aggregateContacts(contacts, { overrides: together })), [['a', 'b']]);
        assert.equal(together.get(pairKey('a', 'b')), 'merge');
    });

    it('carries the primary member name onto the row and keeps the rest', () => {
        const [aggregate] = aggregateContacts([
            contact('b', { firstName: 'Ada', lastName: 'Lovelace', displayName: 'Ada Lovelace (work)' }),
            contact('a', { firstName: 'Ada', lastName: 'Lovelace' }),
        ]);

        assert.equal(aggregate!.key, 'a');
        assert.equal(aggregate!.displayName, 'Ada Lovelace');
        assert.deepEqual(aggregate!.members.map((member) => member.id), ['a', 'b']);
    });
});
