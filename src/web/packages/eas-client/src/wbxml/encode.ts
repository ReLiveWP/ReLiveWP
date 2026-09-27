import { codePageByNamespace, tokenFor } from './codepages.ts';
import { WbxmlError } from './errors.ts';
import { isNode, isOpaque, isText, type EasChild, type EasNode } from '../nodes/node.ts';
import * as T from './tokens.ts';
import { ByteWriter } from './writer.ts';

export function encode(root: EasNode): Uint8Array {
    const w = new ByteWriter();

    w.u8(T.WBXML_VERSION_1_3);
    w.u8(T.PUBLIC_ID_UNKNOWN);
    w.u8(T.CHARSET_UTF8);
    w.u8(0x00);

    writeNode(w, root, { page: 0 });

    return w.take();
}

interface State {
    page: number;
}

function writeNode(w: ByteWriter, node: EasNode, state: State): void {
    const page = codePageByNamespace(node.ns);
    if (!page) throw new WbxmlError(`no code page for namespace '${node.ns}'`);

    const token = tokenFor(node.ns, node.name);
    if (token === undefined)
        throw new WbxmlError(`no WBXML token for '${node.ns}:${node.name}' in code page ${page.page}`);

    if (page.page !== state.page) {
        w.u8(T.SWITCH_PAGE);
        w.u8(page.page);
        state.page = page.page;
    }

    const hasContent = node.children.length > 0;
    w.u8(hasContent ? token | T.TAG_CONTENT : token);
    if (!hasContent) return;

    for (const child of node.children) writeChild(w, child, state);
    w.u8(T.END);
}

function writeChild(w: ByteWriter, child: EasChild, state: State): void {
    if (isNode(child)) {
        writeNode(w, child, state);
        return;
    }

    if (isText(child)) {
        w.u8(T.STR_I);
        w.stringNul(child.text);
        return;
    }

    if (isOpaque(child)) {
        w.u8(T.OPAQUE);
        w.mbUInt32(child.opaque.length);
        w.bytes(child.opaque);
        return;
    }

    throw new WbxmlError(`unrecognised child node: ${JSON.stringify(child)}`);
}
