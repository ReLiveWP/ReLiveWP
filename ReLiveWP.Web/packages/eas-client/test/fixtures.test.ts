import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { decode } from '../src/wbxml/decode.ts';
import { encode } from '../src/wbxml/encode.ts';
import { hex, loadFixtures } from './fixtures.ts';

const fixtures = loadFixtures();

describe('golden fixtures', () => {
    it('finds the fixture set', () => {
        assert.ok(fixtures.length >= 8, `expected the fixture set, found ${fixtures.length}`);
    });

    for (const fixture of fixtures) {
        describe(fixture.name, () => {
            it('decodes to the expected tree', () => {
                const { root } = decode(fixture.bytes);
                assert.deepEqual(root, fixture.tree, fixture.description);
            });

            if (!fixture.decodeOnly) {
                it('encodes to the expected bytes', () => {
                    const actual = encode(fixture.tree);
                    assert.equal(hex(actual), hex(fixture.bytes), fixture.description);
                });
            }

            it('round-trips decode -> encode -> decode', () => {
                const first = decode(fixture.bytes);
                if (fixture.decodeOnly) return;
                assert.deepEqual(decode(encode(first.root)).root, first.root);
            });

            if (fixture.expect?.unknownTokens) {
                it('reports the unknown tokens', () => {
                    const found = decode(fixture.bytes).unknownTokens.map(
                        ({ page, ns, token, name }) => ({ page, ns, token, name }));
                    assert.deepEqual(found, fixture.expect!.unknownTokens);
                });
            }
        });
    }
});

describe('fixtures that pin specific hazards', () => {
    const byName = new Map(fixtures.map((f) => [f.name, f]));

    it('opaque-empty emits the mandatory zero length octet', () => {
        const f = byName.get('opaque-empty')!;
        const bytes = encode(f.tree);
        const opaqueAt = bytes.indexOf(0xc3);

        assert.notEqual(opaqueAt, -1, 'no OPAQUE token in the output');
        assert.equal(bytes[opaqueAt + 1], 0x00, 'OPAQUE must be followed by an mb_u_int32 length of zero');
    });

    it('composemail-mime-opaque carries raw RFC 5322, not base64', () => {
        const f = byName.get('composemail-mime-opaque')!;
        const { root } = decode(f.bytes);

        const mime = root.children.find((c) => 'name' in c && c.name === 'Mime');
        assert.ok(mime && 'children' in mime);

        const payload = mime.children[0];
        assert.ok(payload && 'opaque' in payload, 'Mime content must decode as opaque bytes');
        assert.ok(
            new TextDecoder().decode(payload.opaque).startsWith('From:'),
            'the opaque run must be the message itself, not a base64 rendering of it');
    });

    it('opaque-multibyte-length round-trips a length above 127', () => {
        const f = byName.get('opaque-multibyte-length')!;
        const { root } = decode(f.bytes);

        const payload = root.children[0];
        assert.ok(payload && 'opaque' in payload);
        assert.equal(payload.opaque.length, 200);
    });
});
