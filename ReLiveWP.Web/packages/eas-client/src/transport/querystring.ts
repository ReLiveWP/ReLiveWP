import { commandForCode, COMMAND_CODES, protocolVersionFromByte, type EasCommand } from './wire.ts';

export const PARAMETER_TAGS = {
    AttachmentName: 0,
    CollectionId: 1,
    ItemId: 3,
    LongId: 4,
    Occurrence: 6,
    Options: 7,
    User: 8,
} as const;

export const OPTION_SAVE_IN_SENT = 0x01;
export const OPTION_ACCEPT_MULTIPART = 0x02;

export const LOCALE_EN_US = 0x0409;

export interface QueryParameters {
    command: EasCommand;
    deviceId: string;
    deviceType: string;
    user?: string;
    attachmentName?: string;
    collectionId?: string;
    itemId?: string;
    longId?: string;
    occurrence?: string;
    saveInSent?: boolean;
    acceptMultiPart?: boolean;
}

export interface Base64QueryParameters extends QueryParameters {
    protocolVersion: string;
    policyKey?: number;
    locale?: number;
}

// every protocol version takes this form
export function buildPlainQueryString(p: QueryParameters): string {
    const q = new URLSearchParams();
    q.set('Cmd', p.command);
    if (p.user !== undefined) q.set('User', p.user);
    q.set('DeviceId', p.deviceId);
    q.set('DeviceType', p.deviceType);

    if (p.attachmentName !== undefined) q.set('AttachmentName', p.attachmentName);
    if (p.collectionId !== undefined) q.set('CollectionId', p.collectionId);
    if (p.itemId !== undefined) q.set('ItemId', p.itemId);
    if (p.longId !== undefined) q.set('LongId', p.longId);
    if (p.occurrence !== undefined) q.set('Occurrence', p.occurrence);
    if (p.saveInSent) q.set('SaveInSent', 'T');

    return q.toString();
}

// 12.1 and up only
export function buildBase64QueryString(p: Base64QueryParameters): string {
    const version = versionByte(p.protocolVersion);
    const deviceId = deviceIdBytes(p.deviceId);
    const deviceType = utf8(p.deviceType);

    if (deviceId.length === 0) throw new RangeError('Device ID must not be empty');
    if (deviceId.length > 255) throw new RangeError('Device ID exceeds 255 bytes');
    if (deviceType.length > 255) throw new RangeError('Device type exceeds 255 bytes');

    const parameters: number[] = [];
    const add = (tag: number, value: Uint8Array): void => {
        if (value.length > 255) throw new RangeError(`command parameter ${tag} exceeds 255 bytes`);
        parameters.push(tag, value.length, ...value);
    };

    if (p.attachmentName !== undefined) add(PARAMETER_TAGS.AttachmentName, utf8(p.attachmentName));
    if (p.collectionId !== undefined) add(PARAMETER_TAGS.CollectionId, utf8(p.collectionId));
    if (p.itemId !== undefined) add(PARAMETER_TAGS.ItemId, utf8(p.itemId));
    if (p.longId !== undefined) add(PARAMETER_TAGS.LongId, utf8(p.longId));
    if (p.occurrence !== undefined) add(PARAMETER_TAGS.Occurrence, utf8(p.occurrence));

    const options =
        (p.saveInSent ? OPTION_SAVE_IN_SENT : 0) | (p.acceptMultiPart ? OPTION_ACCEPT_MULTIPART : 0);
    if (options !== 0) add(PARAMETER_TAGS.Options, new Uint8Array([options]));

    if (p.user !== undefined) add(PARAMETER_TAGS.User, utf8(p.user));

    const locale = p.locale ?? LOCALE_EN_US;
    const bytes = [
        version,
        COMMAND_CODES[p.command],
        locale & 0xff,
        (locale >> 8) & 0xff,
        deviceId.length,
        ...deviceId,
        ...(p.policyKey === undefined
            ? [0]
            : [4, p.policyKey & 0xff, (p.policyKey >>> 8) & 0xff, (p.policyKey >>> 16) & 0xff, (p.policyKey >>> 24) & 0xff]),
        deviceType.length,
        ...deviceType,
        ...parameters,
    ];

    return base64(new Uint8Array(bytes));
}

export interface ParsedQuery {
    protocolVersion: string;
    command: EasCommand | undefined;
    commandCode: number;
    locale: number;
    deviceId: string;
    deviceType: string;
    policyKey: number | undefined;
    user: string | undefined;
    attachmentName: string | undefined;
    collectionId: string | undefined;
    itemId: string | undefined;
    longId: string | undefined;
    occurrence: string | undefined;
    saveInSent: boolean;
    acceptMultiPart: boolean;
}

export function parseBase64QueryString(value: string): ParsedQuery {
    const bytes = unbase64(value);
    const text = new TextDecoder();

    let at = 0;
    const u8 = (): number => {
        if (at >= bytes.length) throw new RangeError('base64 query value truncated');
        return bytes[at++]!;
    };
    const run = (length: number): string => {
        if (at + length > bytes.length) throw new RangeError('base64 query value truncated');
        const value = text.decode(bytes.subarray(at, at + length));
        at += length;
        return value;
    };

    const protocolByte = u8();
    const commandCode = u8();
    const locale = u8() | (u8() << 8);
    const deviceIdLength = u8();
    if (at + deviceIdLength > bytes.length) throw new RangeError('base64 query value truncated');
    const deviceId = deviceIdHex(bytes.subarray(at, at + deviceIdLength));
    at += deviceIdLength;

    const policyKeyLength = u8();
    let policyKey: number | undefined;
    if (policyKeyLength === 4) {
        policyKey = (u8() | (u8() << 8) | (u8() << 16) | (u8() << 24)) >>> 0;
    } else if (policyKeyLength !== 0) {
        throw new RangeError(`policy key length must be 0 or 4, got ${policyKeyLength}`);
    }

    const deviceType = run(u8());

    const parsed: ParsedQuery = {
        protocolVersion: protocolVersionFromByte(protocolByte),
        command: commandForCode(commandCode),
        commandCode,
        locale,
        deviceId,
        deviceType,
        policyKey,
        user: undefined,
        attachmentName: undefined,
        collectionId: undefined,
        itemId: undefined,
        longId: undefined,
        occurrence: undefined,
        saveInSent: false,
        acceptMultiPart: false,
    };

    while (at + 2 <= bytes.length) {
        const tag = u8();
        const length = u8();

        if (tag === PARAMETER_TAGS.Options) {
            const options = length === 1 ? u8() : 0;
            at += length === 1 ? 0 : length;
            parsed.saveInSent = (options & OPTION_SAVE_IN_SENT) !== 0;
            parsed.acceptMultiPart = (options & OPTION_ACCEPT_MULTIPART) !== 0;
            continue;
        }

        const value = run(length);
        switch (tag) {
            case PARAMETER_TAGS.AttachmentName: parsed.attachmentName = value; break;
            case PARAMETER_TAGS.CollectionId: parsed.collectionId = value; break;
            case PARAMETER_TAGS.ItemId: parsed.itemId = value; break;
            case PARAMETER_TAGS.LongId: parsed.longId = value; break;
            case PARAMETER_TAGS.Occurrence: parsed.occurrence = value; break;
            case PARAMETER_TAGS.User: parsed.user = value; break;
            default: break;
        }
    }

    return parsed;
}

function versionByte(version: string): number {
    const [major, minor] = version.split('.');
    const value = Number(major) * 10 + Number(minor);

    if (!Number.isInteger(value) || value < 121 || value > 255)
        throw new RangeError(
            `protocol version ${version} cannot be carried in a base64 query value; use the plain text form`);

    return value;
}

function utf8(value: string): Uint8Array {
    return new TextEncoder().encode(value);
}

// a device id is a hex string, and the binary query carries it as the bytes behind that string.
// the plain text query carries the same id as hex, so both spellings name one device.
export function isDeviceIdHex(value: string): boolean {
    return value.length % 2 === 0 && value.length > 0 && /^[0-9a-fA-F]+$/.test(value);
}

function deviceIdBytes(value: string): Uint8Array {
    if (!isDeviceIdHex(value))
        throw new RangeError(`device id must be a hex string, got ${JSON.stringify(value)}`);

    const bytes = new Uint8Array(value.length / 2);
    for (let i = 0; i < bytes.length; i++) bytes[i] = parseInt(value.substr(i * 2, 2), 16);
    return bytes;
}

function deviceIdHex(bytes: Uint8Array): string {
    let out = '';
    for (const byte of bytes) out += byte.toString(16).padStart(2, '0').toUpperCase();
    return out;
}

function base64(bytes: Uint8Array): string {
    let binary = '';
    for (let i = 0; i < bytes.length; i += 0x8000)
        binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));

    return btoa(binary).replace(/=+$/, '');
}

function unbase64(value: string): Uint8Array {
    const normalised = value.replace(/-/g, '+').replace(/_/g, '/');
    const binary = atob(normalised.padEnd(Math.ceil(normalised.length / 4) * 4, '='));

    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}
