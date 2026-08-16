import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { tags } from '@relivewp/eas-client/nodes';

import { opaque } from '@relivewp/eas-client/nodes';

import {
    collectionClassOf,
    decodeEncodedWords,
    folderKind,
    parseAddressList,
    readAnnotations,
    readContact,
    readContactPhoto,
    readFolder,
    readMessage,
} from '../src/index.ts';

const { AirSync: A, AirSyncBase: AB, Contacts: C, Email: E, Email2: E2, WindowsLive: WL } = tags;

const annotation = (name: string, value?: string) =>
    WL.Annotation(WL.Name(name), value === undefined ? WL.Value() : WL.Value(value));

describe('live annotations', () => {
    it('reads every name the server can send', () => {
        const read = readAnnotations(A.ApplicationData(WL.Annotations(
            annotation('CID', '000f00c0deadbeef'),
            annotation('OID', 'oid-1'),
            annotation('WLID', 'ada@live.com'),
            annotation('IMMRI', '1:ada@live.com'),
            annotation('Type', 'Me'),
            annotation('UserTileUrl', 'https://example.invalid/t.jpg'),
            annotation('UserTileHash', 'abc123'),
            annotation('TrustLevel', '3'),
            annotation('FavoriteOrder', '7'))));

        assert.deepEqual(read, {
            cid: '000f00c0deadbeef',
            objectId: 'oid-1',
            wlid: 'ada@live.com',
            imMri: '1:ada@live.com',
            type: 'Me',
            userTileUrl: 'https://example.invalid/t.jpg',
            userTileHash: 'abc123',
            trustLevel: 3,
            favouriteOrder: 7,
        });
    });

    // the CID is 64 bit and arrives as 16 hex characters, which no JS number can hold exactly
    it('keeps the CID as the hex it arrived as', () => {
        const read = readAnnotations(A.ApplicationData(WL.Annotations(
            annotation('CID', 'ffffffffffffffff'))));

        assert.equal(read?.cid, 'ffffffffffffffff');
    });

    it('is null when the element is absent, which is not the same as every value being empty', () => {
        assert.equal(readAnnotations(A.ApplicationData(C.FirstName('Ada'))), null);

        const empty = readAnnotations(A.ApplicationData(WL.Annotations()));
        assert.notEqual(empty, null);
        assert.equal(empty?.type, null);
    });

    it('treats a present but empty Value as absent', () => {
        const read = readAnnotations(A.ApplicationData(WL.Annotations(
            annotation('Type'),
            annotation('WLID', ''),
            annotation('IMMRI', '1:ada@live.com'))));

        assert.equal(read?.type, null);
        assert.equal(read?.wlid, null);
        assert.equal(read?.imMri, '1:ada@live.com');
    });

    it('drops a numeric value that is not an integer rather than storing NaN', () => {
        const read = readAnnotations(A.ApplicationData(WL.Annotations(
            annotation('TrustLevel', 'high'),
            annotation('FavoriteOrder', '2.5'))));

        assert.equal(read?.trustLevel, null);
        assert.equal(read?.favouriteOrder, null);
    });

    it('ignores an Annotation with no Name at all', () => {
        const read = readAnnotations(A.ApplicationData(WL.Annotations(
            WL.Annotation(WL.Value('orphan')),
            annotation('Type', 'Me'))));

        assert.equal(read?.type, 'Me');
    });

    it('hangs the set off the projected contact', () => {
        const contact = readContact(
            A.ApplicationData(C.FirstName('Ada'), WL.Annotations(annotation('Type', 'Me'))),
            'contacts', 'c1');

        assert.equal(contact.annotation?.type, 'Me');
        assert.equal(readContact(A.ApplicationData(C.FirstName('Ada')), 'contacts', 'c1').annotation,
            null);
    });
});

describe('contact pictures', () => {
    const jpeg = new Uint8Array([0xff, 0xd8, 0xff, 0xe0]);

    it('is undefined when absent, because that means unchanged rather than cleared', () => {
        assert.equal(readContactPhoto(A.ApplicationData(C.FirstName('Ada'))), undefined);
    });

    it('is empty when the element is there but carries nothing, which is the clear', () => {
        const read = readContactPhoto(A.ApplicationData(C.Picture()));
        assert.deepEqual(read, new Uint8Array());
    });

    it('decodes the base64 the wire actually carries', () => {
        assert.deepEqual(readContactPhoto(A.ApplicationData(C.Picture('/9j/4A=='))), jpeg);
    });

    it('takes an opaque run as-is rather than decoding it as text', () => {
        assert.deepEqual(readContactPhoto(A.ApplicationData(C.Picture(opaque(jpeg)))), jpeg);
    });
});

describe('address lists', () => {
    it('reads a bare address, a named one and a quoted one', () => {
        assert.deepEqual(parseAddressList('a@b.com'), [{ name: null, email: 'a@b.com' }]);
        assert.deepEqual(parseAddressList('Ada <a@b.com>'), [{ name: 'Ada', email: 'a@b.com' }]);
        assert.deepEqual(
            parseAddressList('"Lovelace, Ada" <a@b.com>'),
            [{ name: 'Lovelace, Ada', email: 'a@b.com' }]);
    });

    it('does not split on a comma inside a quoted display name', () => {
        const parsed = parseAddressList('"Lovelace, Ada" <a@b.com>, Bob <b@c.com>');
        assert.deepEqual(parsed.map((address) => address.email), ['a@b.com', 'b@c.com']);
        assert.equal(parsed[0]?.name, 'Lovelace, Ada');
    });

    it('is empty for nothing at all', () => {
        assert.deepEqual(parseAddressList(undefined), []);
        assert.deepEqual(parseAddressList('   '), []);
    });

    it('decodes encoded words so a display name is not shown raw', () => {
        assert.equal(decodeEncodedWords('=?utf-8?B?QWRhIExvdmVsYWNl?='), 'Ada Lovelace');
        assert.equal(decodeEncodedWords('=?utf-8?Q?Ada_Lovelace?='), 'Ada Lovelace');
        assert.equal(decodeEncodedWords('plain'), 'plain');
        assert.equal(
            parseAddressList('=?utf-8?Q?J=C3=BCrgen?= <j@e.de>')[0]?.name,
            'Jürgen');
    });
});

describe('folder types', () => {
    it('maps the default folders onto roles and classes', () => {
        assert.deepEqual(folderKind(2), { role: 'inbox', class: 'Email' });
        assert.deepEqual(folderKind(5), { role: 'sent', class: 'Email' });
        assert.deepEqual(folderKind(8), { role: 'calendar', class: 'Calendar' });
        assert.deepEqual(folderKind(9), { role: 'contacts', class: 'Contact' });
    });

    it('refuses to give Drafts a class, because it must not be synchronised', () => {
        assert.deepEqual(folderKind(3), { role: 'drafts', class: null });
    });

    it('gives the recipient information cache and unknown types no class', () => {
        assert.equal(folderKind(18).class, null);
        assert.equal(folderKind(19).class, null);
        assert.equal(folderKind(undefined).class, null);
        assert.equal(folderKind(999).class, null);
    });

    it('translates a stored class back into the wire vocabulary', () => {
        assert.equal(collectionClassOf('Contact'), 'Contacts');
        assert.equal(collectionClassOf('Task'), 'Tasks');
        assert.equal(collectionClassOf('Email'), 'Email');
    });

    it('treats a parent of 0 as the root', () => {
        const folder = readFolder({
            kind: 'Add', serverId: '2', parentId: '0', displayName: 'Inbox', type: 2,
        });
        assert.equal(folder?.parentId, null);
    });
});

describe('projecting a message', () => {
    const data = (...children: Parameters<typeof A.ApplicationData>) =>
        A.ApplicationData(...children);

    it('derives a snippet from an HTML body when the server sends no preview', () => {
        const message = readMessage(
            data(AB.Body(AB.Type(2), AB.Data('<p>Hello&nbsp;<b>there</b></p>'))),
            'inbox', '1');

        assert.equal(message.preview, 'Hello there');
        assert.equal(message.body?.type, 'html');
    });

    it('prefers the preview the server sent', () => {
        const message = readMessage(
            data(AB.Body(AB.Type(1), AB.Data('the whole body'), AB.Preview('the snippet'))),
            'inbox', '1');

        assert.equal(message.preview, 'the snippet');
    });

    it('flattens a preview that arrives full of markup anyway', () => {
        const message = readMessage(
            data(AB.Body(
                AB.Type(2),
                AB.Data('<html><body>A new device was added!<br>Named &quot;Lumia&quot;</body></html>'),
                AB.Preview('<html><body>A new device was added!<br>Named &quot;Lumia'))),
            'inbox', '1');

        assert.equal(message.preview, 'A new device was added! Named "Lumia');
    });

    it('records truncation, because it is the only sign a refetch is needed', () => {
        const message = readMessage(
            data(AB.Body(
                AB.Type(2), AB.Data('start'), AB.Truncated(1), AB.EstimatedDataSize(9000))),
            'inbox', '1');

        assert.equal(message.body?.truncated, true);
        assert.equal(message.body?.fullSize, 9000);
    });

    it('leaves fullSize empty when nothing was truncated', () => {
        const message = readMessage(
            data(AB.Body(AB.Type(2), AB.Data('all of it'), AB.EstimatedDataSize(9))),
            'inbox', '1');

        assert.equal(message.body?.truncated, false);
        assert.equal(message.body?.fullSize, null);
    });

    it('hexes a conversation id that arrived as bytes', () => {
        const message = readMessage(
            data(E2.ConversationId(new Uint8Array([0xbb, 0xa4, 0x72]))),
            'inbox', '1');

        assert.equal(message.conversationId, 'bba472');
    });

    it('keeps a conversation id that arrived as text', () => {
        const message = readMessage(data(E2.ConversationId('abc123')), 'inbox', '1');
        assert.equal(message.conversationId, 'abc123');
    });

    it('reads attachments a reading pane needs to fetch and inline', () => {
        const message = readMessage(
            data(AB.Attachments(AB.Attachment(
                AB.DisplayName('logo.png'),
                AB.FileReference('ref-1'),
                AB.Method(1),
                AB.EstimatedDataSize(2048),
                AB.ContentId('logo@cid'),
                AB.IsInline(1)))),
            'inbox', '1');

        assert.equal(message.attachments.length, 1);
        assert.deepEqual(message.attachments[0], {
            id: 'ref-1',
            name: 'logo.png',
            size: 2048,
            contentType: null,
            inline: true,
            contentId: 'logo@cid',
        });
    });

    it('treats a cleared flag as not flagged', () => {
        assert.equal(readMessage(data(E.Flag(E.Status(0))), 'inbox', '1').flagged, false);
        assert.equal(readMessage(data(E.Flag(E.Status(2))), 'inbox', '1').flagged, true);
        assert.equal(readMessage(data(), 'inbox', '1').flagged, false);
    });

    it('falls back to sane defaults for a nearly empty item', () => {
        const message = readMessage(data(), 'inbox', '1');

        assert.equal(message.subject, '');
        assert.equal(message.receivedAt, 0);
        assert.equal(message.from, null);
        assert.deepEqual(message.to, []);
        assert.equal(message.body, null);
        assert.equal(message.importance, 'normal');
        assert.equal(message.messageClass, 'IPM.Note');
    });
});
