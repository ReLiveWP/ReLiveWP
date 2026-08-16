import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { WbxmlError } from '../src/wbxml/errors.ts';
import { ByteReader } from '../src/wbxml/reader.ts';
import { ByteWriter } from '../src/wbxml/writer.ts';

function written(f: (w: ByteWriter) => void): number[] {
    const w = new ByteWriter(4);
    f(w);
    return [...w.take()];
}

describe('mb_u_int32', () => {
    // both worked examples straight out of the wbxml spec
    it('encodes 0xA0 as 0x81 0x20', () => {
        assert.deepEqual(written((w) => w.mbUInt32(0xa0)), [0x81, 0x20]);
    });

    it('encodes 0x60 as 0x60', () => {
        assert.deepEqual(written((w) => w.mbUInt32(0x60)), [0x60]);
    });

    // the one ASWBXML gets wrong, `while (value > 0)` gives an empty run for zero
    it('encodes zero as a single 0x00 octet', () => {
        assert.deepEqual(written((w) => w.mbUInt32(0)), [0x00]);
    });

    it('encodes the 7-bit boundary', () => {
        assert.deepEqual(written((w) => w.mbUInt32(0x7f)), [0x7f]);
        assert.deepEqual(written((w) => w.mbUInt32(0x80)), [0x81, 0x00]);
    });

    it('encodes the largest 32-bit value in five octets', () => {
        assert.deepEqual(written((w) => w.mbUInt32(0xffffffff)), [0x8f, 0xff, 0xff, 0xff, 0x7f]);
    });

    it('rejects values outside the 32-bit range', () => {
        assert.throws(() => written((w) => w.mbUInt32(-1)), RangeError);
        assert.throws(() => written((w) => w.mbUInt32(0x1_0000_0000)), RangeError);
    });

    it('round-trips through the reader', () => {
        for (const value of [0, 1, 0x7f, 0x80, 0xa0, 0x3fff, 0x4000, 1_000_000, 0xffffffff]) {
            const bytes = new Uint8Array(written((w) => w.mbUInt32(value)));
            assert.equal(new ByteReader(bytes).mbUInt32(), value, `value ${value}`);
        }
    });

    it('rejects a run longer than five octets', () => {
        const bytes = new Uint8Array([0x80, 0x80, 0x80, 0x80, 0x80, 0x00]);
        assert.throws(() => new ByteReader(bytes).mbUInt32(), WbxmlError);
    });
});

describe('inline strings', () => {
    it('round-trips UTF-8 above the BMP', () => {
        const value = 'café 東京 👨‍👩‍👧';
        const bytes = new Uint8Array(written((w) => w.stringNul(value)));

        assert.equal(bytes[bytes.length - 1], 0x00, 'must be NUL terminated');
        assert.equal(new ByteReader(bytes).stringNul(), value);
    });

    it('writes accented characters as UTF-8, not Latin-1', () => {
        assert.deepEqual(written((w) => w.stringNul('é')), [0xc3, 0xa9, 0x00]);
    });

    it('rejects an unterminated string', () => {
        assert.throws(() => new ByteReader(new Uint8Array([0x41, 0x42])).stringNul(), WbxmlError);
    });
});

describe('reader bounds', () => {
    it('rejects reading past the end', () => {
        const r = new ByteReader(new Uint8Array([0x01]));
        r.u8();
        assert.throws(() => r.u8(), WbxmlError);
    });

    it('rejects an opaque run that overruns the document', () => {
        assert.throws(() => new ByteReader(new Uint8Array([0x01, 0x02])).bytesOf(9), WbxmlError);
    });
});

describe('writer growth', () => {
    it('grows past its initial capacity without corrupting content', () => {
        const expected = Array.from({ length: 5000 }, (_, i) => i & 0xff);
        assert.deepEqual(written((w) => { for (const b of expected) w.u8(b); }), expected);
    });
});
