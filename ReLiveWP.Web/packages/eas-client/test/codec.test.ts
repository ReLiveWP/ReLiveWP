import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { decode } from '../src/wbxml/decode.ts';
import { encode } from '../src/wbxml/encode.ts';
import { WbxmlError } from '../src/wbxml/errors.ts';
import { el, opaque } from '../src/nodes/node.ts';
import { toXml } from '../src/wbxml/toxml.ts';

const HEADER = [0x03, 0x01, 0x6a, 0x00];

function doc(...body: number[]): Uint8Array {
    return new Uint8Array([...HEADER, ...body]);
}

describe('decode', () => {
    it('accepts charset 0 as UTF-8, per [WBXML1.3] 5.6', () => {
        const bytes = new Uint8Array([0x03, 0x01, 0x00, 0x00, 0x45, 0x03, 0x78, 0x00, 0x01]);
        assert.equal(decode(bytes).root.name, 'Sync');
    });

    it('rejects a charset it would silently mis-decode', () => {
        // MIBEnum 4 is iso-8859-1
        assert.throws(() => decode(new Uint8Array([0x03, 0x01, 0x04, 0x00, 0x05])), WbxmlError);
    });

    it('treats an empty element as empty rather than opening a scope', () => {
        // Sync with content, Status (0x0e) without
        const { root } = decode(doc(0x45, 0x0e, 0x01));
        assert.deepEqual(root, el('AirSync', 'Sync', el('AirSync', 'Status')));
    });

    it('keeps consecutive text runs separate', () => {
        const { root } = decode(doc(0x45, 0x03, 0x61, 0x00, 0x03, 0x62, 0x00, 0x01));
        assert.deepEqual(root.children, [{ text: 'a' }, { text: 'b' }]);
    });

    it('decodes ENTITY as a code point', () => {
        // ENTITY 0x02 then mb_u_int32 160 (0x81 0x20)
        const { root } = decode(doc(0x45, 0x02, 0x81, 0x20, 0x01));
        assert.deepEqual(root.children, [{ text: ' ' }]);
    });

    it('skips extension tokens without losing sync', () => {
        // EXT_I_0 with a string, EXT_T_0 with an integer, then real content
        const { root } = decode(doc(0x45, 0x40, 0x7a, 0x00, 0x80, 0x05, 0x03, 0x71, 0x00, 0x01));
        assert.deepEqual(root.children, [{ text: 'q' }]);
    });

    it('rejects a tag that declares attributes', () => {
        assert.throws(() => decode(doc(0xc5, 0x01, 0x01)), /attributes/);
    });

    it('rejects a processing instruction', () => {
        assert.throws(() => decode(doc(0x43, 0x05, 0x01)), /processing instruction/);
    });

    it('rejects an unbalanced document', () => {
        assert.throws(() => decode(doc(0x45)), /still open/);
        assert.throws(() => decode(doc(0x45, 0x01, 0x01)), /END with no open element/);
    });

    it('rejects a second root element', () => {
        assert.throws(() => decode(doc(0x05, 0x05)), /root elements/);
    });

    it('reports an unknown code page but keeps decoding', () => {
        const { root, unknownPages } = decode(doc(0x00, 0x63, 0x45, 0x03, 0x78, 0x00, 0x01));
        assert.deepEqual(unknownPages, [0x63]);
        assert.equal(root.ns, 'CodePage99');
    });
});

describe('encode', () => {
    it('writes the EAS document header', () => {
        assert.deepEqual([...encode(el('AirSync', 'Sync')).subarray(0, 4)], HEADER);
    });

    it('refuses an unknown namespace instead of guessing the current page', () => {
        assert.throws(() => encode(el('Nonsense', 'Sync')), /no code page for namespace/);
    });

    it('refuses a tag with no token in its page', () => {
        assert.throws(() => encode(el('AirSync', 'Nonexistent')), /no WBXML token/);
    });

    it('does not carry the code page across calls', () => {
        // AirSyncBase leaves the writer on page 17, a fresh AirSync doc can't assume that
        encode(el('AirSyncBase', 'Body', 'x'));
        assert.deepEqual([...encode(el('AirSync', 'Sync', 'x'))], [...HEADER, 0x45, 0x03, 0x78, 0x00, 0x01]);
    });

    it('writes an empty opaque child as OPAQUE plus a zero length', () => {
        const bytes = encode(el('ComposeMail', 'Mime', opaque(new Uint8Array(0))));
        assert.deepEqual([...bytes], [...HEADER, 0x00, 0x15, 0x50, 0xc3, 0x00, 0x01]);
    });
});

describe('toXml', () => {
    it('renders the shape the server logs', () => {
        const tree = el('FolderHierarchy', 'FolderSync', el('FolderHierarchy', 'SyncKey', '0'));

        assert.equal(
            toXml(tree),
            '<?xml version="1.0" encoding="utf-8"?>' +
            '<folderhierarchy:FolderSync xmlns:folderhierarchy="FolderHierarchy">' +
            '<folderhierarchy:SyncKey>0</folderhierarchy:SyncKey>' +
            '</folderhierarchy:FolderSync>');
    });

    it('declares a prefix once per scope', () => {
        const tree = el('AirSync', 'Options', el('AirSyncBase', 'BodyPreference', el('AirSyncBase', 'Type', '2')));
        const xml = toXml(tree, { declaration: false });

        assert.equal(xml.match(/xmlns:airsyncbase=/g)?.length, 1);
    });

    it('renders ComposeMail:Mime as base64, matching the C# rendering', () => {
        const tree = el('ComposeMail', 'Mime', opaque(new TextEncoder().encode('From: a@b')));
        assert.match(toXml(tree, { declaration: false }), />RnJvbTogYUBi</);
    });

    it('uses the code page prefix rather than the lowercased namespace', () => {
        const xml = toXml(el('WindowsLive', 'Annotations'), { declaration: false });
        assert.equal(xml, '<live:Annotations xmlns:live="WindowsLive" />');
    });

    it('escapes markup in text', () => {
        assert.match(toXml(el('AirSync', 'Sync', 'a & b < c'), { declaration: false }), /a &amp; b &lt; c/);
    });

    // rendering this a byte per char is what showed "Síoda" as "SÃ­oda"
    it('renders a non-binary opaque run as UTF-8, matching the C# rendering', () => {
        const tree = el('Contacts', 'FirstName', opaque(new TextEncoder().encode('Síoda')));
        assert.match(toXml(tree, { declaration: false }), /<!\[CDATA\[Síoda\]\]>/);
    });
});
