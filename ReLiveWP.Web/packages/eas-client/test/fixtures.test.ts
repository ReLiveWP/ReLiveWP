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
