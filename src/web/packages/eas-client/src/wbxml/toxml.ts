// debug only, matches the shape Services.Exchange logs under Eas:LogMessageBodies so the two can
// be diffed. nothing parses xml back in.

import { codePageByNamespace } from './codepages.ts';
import { decodeUtf8 } from './reader.ts';
import { isNode, isOpaque, isText, type EasChild, type EasNode } from '../nodes/node.ts';

const BASE64_RENDERED = new Set(['Email2:ConversationId', 'Email2:ConversationIndex', 'ComposeMail:Mime']);

export interface ToXmlOptions {
    indent?: boolean;
    declaration?: boolean;
}

export function toXml(root: EasNode, options: ToXmlOptions = {}): string {
    const indent = options.indent ?? false;
    const parts: string[] = [];

    if (options.declaration ?? true) {
        parts.push('<?xml version="1.0" encoding="utf-8"?>');
        if (indent) parts.push('\n');
    }

    write(parts, root, new Set(), indent, 0);
    return parts.join('');
}

function write(out: string[], node: EasNode, inScope: Set<string>, indent: boolean, depth: number): void {
    // not lowercase(ns), WindowsLive goes out as "live"
    const prefix = codePageByNamespace(node.ns)?.xmlns ?? node.ns.toLowerCase();
    const qname = `${prefix}:${node.name}`;

    const declares = !inScope.has(prefix);
    const scope = declares ? new Set(inScope).add(prefix) : inScope;

    if (indent && depth > 0) out.push('\n', '  '.repeat(depth));
    out.push('<', qname);
    if (declares) out.push(` xmlns:${prefix}="${node.ns}"`);

    if (node.children.length === 0) {
        out.push(' />');
        return;
    }

    out.push('>');

    const elementChildren = node.children.some(isNode);
    for (const child of node.children) writeChild(out, node, child, scope, indent, depth + 1);
    if (indent && elementChildren) out.push('\n', '  '.repeat(depth));

    out.push('</', qname, '>');
}

function writeChild(
    out: string[], parent: EasNode, child: EasChild, scope: Set<string>, indent: boolean, depth: number): void {
    if (isNode(child)) {
        write(out, child, scope, indent, depth);
        return;
    }

    if (isText(child)) {
        out.push(escapeText(child.text));
        return;
    }

    if (isOpaque(child)) {
        if (BASE64_RENDERED.has(`${parent.ns}:${parent.name}`)) out.push(base64(child.opaque));
        else out.push('<![CDATA[', decodeUtf8(child.opaque), ']]>');
    }
}

function escapeText(value: string): string {
    return value.replace(/[&<>]/g, (c) => (c === '&' ? '&amp;' : c === '<' ? '&lt;' : '&gt;'));
}

function latin1(bytes: Uint8Array): string {
    let out = '';
    for (let i = 0; i < bytes.length; i += 0x8000)
        out += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
    return out;
}

function base64(bytes: Uint8Array): string {
    return btoa(latin1(bytes));
}
