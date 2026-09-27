const utf8 = new TextEncoder();

export class ByteWriter {
    private buf: Uint8Array;
    private len = 0;

    constructor(initialCapacity = 1024) {
        this.buf = new Uint8Array(initialCapacity);
    }

    get length(): number {
        return this.len;
    }

    private reserve(extra: number): void {
        if (this.len + extra <= this.buf.length) return;
        let capacity = this.buf.length * 2;
        while (capacity < this.len + extra) capacity *= 2;
        const grown = new Uint8Array(capacity);
        grown.set(this.buf.subarray(0, this.len));
        this.buf = grown;
    }

    u8(value: number): void {
        this.reserve(1);
        this.buf[this.len++] = value;
    }

    // zero is one 0x00 octet, never an empty run. get that wrong and everything after it desyncs.
    mbUInt32(value: number): void {
        if (!Number.isInteger(value) || value < 0 || value > 0xffffffff)
            throw new RangeError(`multi-byte integer out of range: ${value}`);

        const octets: number[] = [value & 0x7f];
        let rest = Math.floor(value / 0x80);
        while (rest > 0) {
            octets.unshift((rest & 0x7f) | 0x80);
            rest = Math.floor(rest / 0x80);
        }

        this.reserve(octets.length);
        for (const o of octets) this.buf[this.len++] = o;
    }

    bytes(value: Uint8Array): void {
        this.reserve(value.length);
        this.buf.set(value, this.len);
        this.len += value.length;
    }

    stringNul(value: string): void {
        this.bytes(utf8.encode(value));
        this.u8(0x00);
    }

    take(): Uint8Array {
        return this.buf.slice(0, this.len);
    }
}

export function encodeUtf8(value: string): Uint8Array {
    return utf8.encode(value);
}
