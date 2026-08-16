import { WbxmlError } from './errors.ts';

const utf8 = new TextDecoder('utf-8', { fatal: false });

export class ByteReader {
    private readonly bytes: Uint8Array;
    private pos = 0;

    constructor(bytes: Uint8Array) {
        this.bytes = bytes;
    }

    get offset(): number {
        return this.pos;
    }

    get remaining(): number {
        return this.bytes.length - this.pos;
    }

    get eof(): boolean {
        return this.pos >= this.bytes.length;
    }

    u8(): number {
        if (this.pos >= this.bytes.length) throw new WbxmlError('unexpected end of document', this.pos);
        return this.bytes[this.pos++]!;
    }

    peek(): number | undefined {
        return this.bytes[this.pos];
    }

    mbUInt32(): number {
        const start = this.pos;
        let value = 0;
        for (let i = 0; i < 5; i++) {
            const b = this.u8();
            value = value * 0x80 + (b & 0x7f);
            if ((b & 0x80) === 0) {
                if (value > 0xffffffff) throw new WbxmlError('multi-byte integer exceeds 32 bits', start);
                return value;
            }
        }
        throw new WbxmlError('multi-byte integer longer than five octets', start);
    }

    bytesOf(length: number): Uint8Array {
        if (length < 0 || this.pos + length > this.bytes.length)
            throw new WbxmlError(`run of ${length} bytes overruns the document`, this.pos);
        const slice = this.bytes.subarray(this.pos, this.pos + length);
        this.pos += length;
        return slice;
    }

    stringNul(): string {
        const start = this.pos;
        let end = this.pos;
        while (end < this.bytes.length && this.bytes[end] !== 0x00) end++;
        if (end >= this.bytes.length) throw new WbxmlError('unterminated inline string', start);
        const text = utf8.decode(this.bytes.subarray(start, end));
        this.pos = end + 1;
        return text;
    }
}

export function decodeUtf8(bytes: Uint8Array): string {
    return utf8.decode(bytes);
}
