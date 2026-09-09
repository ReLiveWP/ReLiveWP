import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import type { Contact, ContactAnnotation } from '@relivewp/eas-store';

import { cidIndex, contactFor } from '../src/views/people/authors.ts';

function contact(id: string, cid: string | null): Contact {
    const annotation: ContactAnnotation | null = cid === null ? null : {
        cid, objectId: null, wlid: null, imMri: null, type: null,
        userTileUrl: null, userTileHash: null, trustLevel: null, favouriteOrder: null,
        originService: null, originCollection: null,
    };

    return {
        id,
        folderId: 'contacts',
        displayName: id,
        sortName: id,
        firstName: null, middleName: null, lastName: null, nickname: null,
        company: null, jobTitle: null, department: null, officeLocation: null,
        emails: [], phones: [], imAddresses: [], addresses: [],
        webPage: null, birthday: null, anniversary: null,
        categories: [], notes: null, annotation,
    };
}

describe('joining feed authors to contacts by cid', () => {
    it('matches the x16 cid eas wrote regardless of case', () => {
        const amy = contact('amy', '15FE5D7A6D8D65FF');
        const index = cidIndex([amy, contact('me', null)]);

        assert.equal(contactFor(index, '15fe5d7a6d8d65ff'), amy);
        assert.equal(index.size, 1);
    });

    it('misses an author with no cid or one nobody carries', () => {
        const index = cidIndex([contact('amy', '15fe5d7a6d8d65ff')]);

        assert.equal(contactFor(index, null), undefined);
        assert.equal(contactFor(index, '0e0d99585f1f6823'), undefined);
    });

    it('keeps the first contact when two carry the same cid', () => {
        const first = contact('first', 'abc');
        const index = cidIndex([first, contact('second', 'abc')]);

        assert.equal(contactFor(index, 'abc'), first);
    });
});
