import { codePageByNumber, type CodePage } from './codepages.ts';
import { WbxmlError } from './errors.ts';
import type { EasNode } from '../nodes/node.ts';
import { ByteReader, decodeUtf8 } from './reader.ts';
import * as T from './tokens.ts';

export interface UnknownToken {
    readonly page: number;
    readonly ns: string;
    readonly token: number;
    readonly offset: number;
    readonly name: string;
}

export interface DecodeResult {
    readonly root: EasNode;
    readonly unknownTokens: readonly UnknownToken[];
    readonly unknownPages: readonly number[];
    readonly literals: readonly string[];
}

interface Mutable {
    unknownTokens: UnknownToken[];
    unknownPages: number[];
    literals: string[];
}

function unknownPage(page: number): CodePage {
    return { page, ns: `CodePage${page}`, xmlns: `cp${page}`, tags: [] };
}

export function decode(bytes: Uint8Array): DecodeResult {
    const r = new ByteReader(bytes);
    const found: Mutable = { unknownTokens: [], unknownPages: [], literals: [] };

    r.u8();
    r.mbUInt32();

    const charsetAt = r.offset;
    const charset = r.mbUInt32();
    if (charset !== T.CHARSET_UTF8 && charset !== T.CHARSET_UNKNOWN)
        throw new WbxmlError(`charset ${charset} is not UTF-8`, charsetAt);

    const stringTable = r.bytesOf(r.mbUInt32());

    const roots: EasNode[] = [];
    const stack: EasNode[] = [];
    const top = (): EasNode | undefined => stack[stack.length - 1];

    let page = codePageByNumber(0)!;

    const append = (node: EasNode): void => {
        const parent = top();
        if (parent) parent.children.push(node);
        else roots.push(node);
    };

    const appendChild = (child: { text: string } | { opaque: Uint8Array }, at: number): void => {
        const parent = top();
        if (!parent) throw new WbxmlError('content outside the root element', at);
        parent.children.push(child);
    };

    while (!r.eof) {
        const at = r.offset;
        const b = r.u8();

        switch (b) {
            case T.SWITCH_PAGE: {
                const next = r.u8();
                const known = codePageByNumber(next);
                if (!known && !found.unknownPages.includes(next)) found.unknownPages.push(next);
                page = known ?? unknownPage(next);
                break;
            }

            case T.END:
                if (stack.length === 0) throw new WbxmlError('END with no open element', at);
                stack.pop();
                break;

            case T.STR_I:
                appendChild({ text: r.stringNul() }, at);
                break;

            case T.STR_T:
                appendChild({ text: stringTableEntry(stringTable, r.mbUInt32(), at) }, at);
                break;

            case T.ENTITY:
                appendChild({ text: String.fromCodePoint(r.mbUInt32()) }, at);
                break;

            case T.OPAQUE:
                appendChild({ opaque: r.bytesOf(r.mbUInt32()).slice() }, at);
                break;

            case T.LITERAL:
            case T.LITERAL_A:
            case T.LITERAL_C:
            case T.LITERAL_AC: {
                if (b === T.LITERAL_A || b === T.LITERAL_AC)
                    throw new WbxmlError('literal tag declares attributes, which EAS never uses', at);

                const name = stringTableEntry(stringTable, r.mbUInt32(), at);
                if (!found.literals.includes(name)) found.literals.push(name);

                const node: EasNode = { ns: page.ns, name, children: [] };
                append(node);
                if (b === T.LITERAL_C) stack.push(node);
                break;
            }

            // nothing in EAS defines extensions, but they're self delimiting so skipping keeps
            // the stream aligned instead of binning a response we could still read
            case T.EXT_I_0:
            case T.EXT_I_1:
            case T.EXT_I_2:
                r.stringNul();
                break;

            case T.EXT_T_0:
            case T.EXT_T_1:
            case T.EXT_T_2:
                r.mbUInt32();
                break;

            case T.EXT_0:
            case T.EXT_1:
            case T.EXT_2:
                break;

            // needs an attribute code space EAS doesn't define, and skipping blind desyncs the tags
            case T.PI:
                throw new WbxmlError('processing instruction encountered; EAS defines no attributes', at);

            default: {
                if ((b & T.TAG_ATTRIBUTES) !== 0)
                    throw new WbxmlError(
                        `tag 0x${b.toString(16)} declares attributes, which EAS never uses`, at);

                const token = b & T.TAG_MASK;
                let name = page.tags[token];
                if (name === undefined) {
                    name = `UNKNOWN_TAG_${token.toString(16).toUpperCase().padStart(2, '0')}`;
                    found.unknownTokens.push({ page: page.page, ns: page.ns, token, offset: at, name });
                }

                const node: EasNode = { ns: page.ns, name, children: [] };
                append(node);
                if ((b & T.TAG_CONTENT) !== 0) stack.push(node);
                break;
            }
        }
    }

    if (stack.length > 0)
        throw new WbxmlError(`document ends with ${stack.length} element(s) still open`, r.offset);
    if (roots.length === 0) 
        throw new WbxmlError('document contains no elements');
    if (roots.length > 1)
        throw new WbxmlError(`document has ${roots.length} root elements, expected one`);

    return {
        root: roots[0]!,
        unknownTokens: found.unknownTokens,
        unknownPages: found.unknownPages,
        literals: found.literals,
    };
}

function stringTableEntry(table: Uint8Array, offset: number, at: number): string {
    if (offset >= table.length) throw new WbxmlError(`string table offset ${offset} out of range`, at);
    let end = offset;
    while (end < table.length && table[end] !== 0x00) end++;
    return decodeUtf8(table.subarray(offset, end));
}
