import { readEmail, type EmailItem } from '@relivewp/eas-client';
import { bytes, int, pick, pickAll, tags, text, type EasNode } from '@relivewp/eas-client/nodes';
import type { AttachmentRef, Body, BodyType, Importance, Message } from '@relivewp/eas-store';

import { parseAddress, parseAddressList, decodeEncodedWords } from './addresses.ts';

const { AirSyncBase: AB, Email: E, Email2: E2 } = tags;

const PREVIEW_LENGTH = 255;

const IMPORTANCE: Readonly<Record<number, Importance>> = { 0: 'low', 1: 'normal', 2: 'high' };

const ENTITIES: Readonly<Record<string, string>> = {
    amp: '&', lt: '<', gt: '>', quot: '"', apos: "'", nbsp: ' ',
};

const MARKUP = /<[a-z!/][^>]*>/i;

function hex(value: Uint8Array): string {
    let out = '';
    for (const byte of value) out += byte.toString(16).padStart(2, '0');
    return out;
}

// byte arrays, but a server can send them inline as text instead
function identifier(node: EasNode | undefined, tag: typeof E2.ConversationId): string | null {
    const raw = bytes(node, tag);
    if (raw !== undefined && raw.length > 0) return hex(raw);

    const value = text(node, tag);
    return value === undefined || value.length === 0 ? null : value;
}

function stripMarkup(value: string): string {
    return value
        .replace(/<(script|style)\b[^>]*>[\s\S]*?<\/\1>/gi, ' ')
        .replace(/<[^>]*>/g, ' ')
        .replace(/&(#x?[0-9a-fA-F]+|[a-zA-Z]+);/g, (whole, entity: string) => {
            if (entity.startsWith('#x') || entity.startsWith('#X'))
                return String.fromCodePoint(parseInt(entity.slice(2), 16));
            if (entity.startsWith('#')) return String.fromCodePoint(Number(entity.slice(1)));
            return ENTITIES[entity.toLowerCase()] ?? whole;
        });
}

function snippet(value: string, type: BodyType): string {
    const flat = (type === 'html' ? stripMarkup(value) : value).replace(/\s+/g, ' ').trim();
    return flat.length <= PREVIEW_LENGTH ? flat : flat.slice(0, PREVIEW_LENGTH);
}

// preview is meant to be plain text and ours comes back with markup in it
function choosePreview(supplied: string | undefined, body: Body | null): string {
    if (supplied !== undefined && !MARKUP.test(supplied)) return supplied;

    const source = supplied ?? body?.content ?? '';
    return snippet(source, MARKUP.test(source) ? 'html' : body?.type ?? 'text');
}

// the Properties of an ItemOperations Fetch carry the same AirSyncBase Body as a Sync change does
export function readMessageBody(properties: EasNode | undefined): Body | null {
    return readBody(pick(properties, AB.Body));
}

function readBody(node: EasNode | undefined): Body | null {
    if (node === undefined) return null;

    const type = int(node, AB.Type) === 2 ? 'html' : 'text';
    const truncated = int(node, AB.Truncated) === 1;
    const size = int(node, AB.EstimatedDataSize);

    return {
        type,
        content: text(node, AB.Data) ?? '',
        truncated,
        fullSize: truncated ? size ?? null : null,
    };
}

function readAttachments(node: EasNode | undefined): AttachmentRef[] {
    return pickAll(node, AB.Attachment)
        .map((attachment): AttachmentRef | undefined => {
            const id = text(attachment, AB.FileReference);
            if (id === undefined) return undefined;

            return {
                id,
                name: decodeEncodedWords(text(attachment, AB.DisplayName) ?? ''),
                size: int(attachment, AB.EstimatedDataSize) ?? 0,
                contentType: text(attachment, AB.ContentType) ?? null,
                inline: int(attachment, AB.IsInline) === 1,
                contentId: text(attachment, AB.ContentId) ?? null,
            };
        })
        .filter((attachment): attachment is AttachmentRef => attachment !== undefined);
}

// a Flag with Status 0 was cleared, the container sticks around to say so
function isFlagged(node: EasNode | undefined): boolean {
    if (node === undefined) return false;

    const status = int(node, E.Status);
    if (status !== undefined) return status !== 0;
    return text(node, E.FlagType) !== undefined;
}

export function readMessage(data: EasNode, folderId: string, id: string): Message {
    const item: EmailItem = readEmail(data);
    const body = readBody(item.body);

    return {
        id,
        folderId,
        receivedAt: item.dateReceived?.getTime() ?? 0,
        from: parseAddress(item.from),
        sender: parseAddress(item.sender),
        to: parseAddressList(item.to),
        cc: parseAddressList(item.cc),
        replyTo: parseAddressList(item.replyTo),
        subject: decodeEncodedWords(item.subject ?? ''),
        preview: choosePreview(text(item.body, AB.Preview), body),
        read: item.read === true,
        flagged: isFlagged(item.flag),
        importance: IMPORTANCE[item.importance ?? 1] ?? 'normal',
        messageClass: item.messageClass ?? 'IPM.Note',
        conversationId: identifier(data, E2.ConversationId),
        threadIndex: identifier(data, E2.ConversationIndex),
        attachments: readAttachments(pick(data, AB.Attachments)),
        body,
        isMeetingRequest: item.meetingRequest !== undefined,
    };
}
