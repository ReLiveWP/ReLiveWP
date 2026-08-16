import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import type { Contact } from '@relivewp/eas-store';

import { ALPHABET, initialOf, letterIndex, letterOf, nameOf, OTHER, toRows } from '../src/views/people/groups.ts';

function named(id: string, patch: Partial<Contact>): Contact {
    return { ...contact(id, id.toLowerCase()), ...patch };
}

function contact(id: string, sortName: string, displayName = id): Contact {
    return {
        id,
        folderId: 'contacts',
        displayName,
        sortName,
        firstName: null, middleName: null, lastName: null, nickname: null,
        company: null, jobTitle: null, department: null, officeLocation: null,
        emails: [], phones: [], imAddresses: [], addresses: [],
        webPage: null, birthday: null, anniversary: null,
        categories: [], notes: null, annotation: null,
    };
}

describe('bucketing contacts by letter', () => {
    it('opens a group on the first letter of the sort name', () => {
        const rows = toRows([
            contact('a1', 'anderson ada'),
            contact('a2', 'archer bob'),
            contact('b1', 'barker carl'),
        ]);

        assert.deepEqual(rows.map((row) => (row.kind === 'header' ? `[${row.letter}]` : row.contact.id)),
            ['[a]', 'a1', 'a2', '[b]', 'b1']);
    });

    it('marks only the first contact of a group, which is what draws the top rule', () => {
        const rows = toRows([contact('a1', 'anderson'), contact('a2', 'archer')]);
        const contacts = rows.filter((row) => row.kind === 'contact');

        assert.deepEqual(contacts.map((row) => row.kind === 'contact' && row.first), [true, false]);
    });

    it('files anything that is not a latin letter under one bucket', () => {
        assert.equal(letterOf(contact('n', '3 mobile')), OTHER);
        assert.equal(letterOf(contact('s', '+44 7700 900123')), OTHER);
        assert.equal(letterOf(contact('c', 'Ålesund')), OTHER);
        assert.equal(letterOf(contact('e', '')), OTHER);
        assert.equal(letterOf(contact('u', 'Anderson')), 'a');
    });

    it('is empty for no contacts at all', () => {
        assert.deepEqual(toRows([]), []);
    });

    it('gives every row a key unique across the list', () => {
        const rows = toRows([contact('a1', 'anderson'), contact('b1', 'barker')]);
        assert.equal(new Set(rows.map((row) => row.key)).size, rows.length);
    });
});

describe('the a-z index', () => {
    it('lists every letter whether or not anyone is filed under it', () => {
        const index = letterIndex(toRows([contact('a1', 'anderson')]));
        assert.deepEqual(index.map((entry) => entry.letter), [...ALPHABET]);
    });

    // a letter with nobody in it has no row to jump to, which is what makes it unclickable
    it('points at the header row for a populated letter and nowhere for an empty one', () => {
        const rows = toRows([contact('a1', 'anderson'), contact('c1', 'carter')]);
        const index = letterIndex(rows);
        const at = (letter: string) => index.find((entry) => entry.letter === letter)?.row;

        assert.equal(at('a'), 0);
        assert.equal(at('b'), null);
        assert.equal(at('c'), 2);
        assert.equal(rows[at('c')!]?.kind, 'header');
    });

    it('puts the other bucket last, after z', () => {
        assert.equal(ALPHABET[ALPHABET.length - 1], OTHER);

        const index = letterIndex(toRows([contact('n', '3 mobile'), contact('a1', 'anderson')]));
        assert.notEqual(index.find((entry) => entry.letter === OTHER)?.row, null);
    });
});

// the server stores whatever FileAs the phone wrote, which is "Last, First" for anyone with a
// surname. the parts are rebuilt from firstName/lastName rather than by unpicking that comma
describe('rebuilding a name for display', () => {
    it('turns a filed name back into given-name-first, keeping the surname apart', () => {
        const parts = nameOf(named('c', {
            displayName: 'Ballmer, Steve', sortName: 'ballmer steve',
            firstName: 'Steve', lastName: 'Ballmer',
        }));

        assert.deepEqual(parts, { lead: 'Steve', surname: 'Ballmer' });
    });

    it('keeps a middle name with the given name rather than with the surname', () => {
        const parts = nameOf(named('c', {
            firstName: 'Ada', middleName: 'Byron', lastName: 'Lovelace',
        }));

        assert.deepEqual(parts, { lead: 'Ada Byron', surname: 'Lovelace' });
    });

    // most contacts on a phone are a first name and nothing else, and emphasising all of them
    // would say nothing at all
    it('leaves someone with no surname with nothing to emphasise', () => {
        assert.deepEqual(nameOf(named('c', { firstName: 'Amy' })), { lead: 'Amy', surname: '' });
    });

    it('copes with a surname and no given name', () => {
        assert.deepEqual(nameOf(named('c', { firstName: null, lastName: 'Sharples' })),
            { lead: '', surname: 'Sharples' });
    });

    it('falls back to what the server filed when there are no parts at all', () => {
        assert.deepEqual(
            nameOf(named('c', { displayName: 'Acme', firstName: null, company: 'Acme' })),
            { lead: 'Acme', surname: '' });
    });

    it('ignores parts that are only whitespace', () => {
        assert.deepEqual(nameOf(named('c', { firstName: '  ', lastName: '  ', displayName: 'Nokia' })),
            { lead: 'Nokia', surname: '' });
    });
});

describe('the initial on a tile', () => {
    // it labels what is rendered, so a filed name has to be rebuilt before it is read
    it('comes from the given name, not from what the contact is filed under', () => {
        assert.equal(initialOf(named('c', {
            displayName: 'Ballmer, Steve', firstName: 'Steve', lastName: 'Ballmer',
        })), 'S');
    });

    it('uses the surname when that is all there is', () => {
        assert.equal(initialOf(named('c', { firstName: null, lastName: 'Sharples' })), 'S');
    });

    it('falls back rather than rendering nothing', () => {
        assert.equal(initialOf(contact('c', 'x', '   ')), '?');
    });
});
