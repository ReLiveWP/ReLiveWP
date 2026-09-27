export const COMMANDS = [
    'Sync',
    'SendMail',
    'SmartForward',
    'SmartReply',
    'GetAttachment',
    'FolderSync',
    'FolderCreate',
    'FolderDelete',
    'FolderUpdate',
    'MoveItems',
    'GetItemEstimate',
    'MeetingResponse',
    'Search',
    'Settings',
    'Ping',
    'ItemOperations',
    'Provision',
    'ResolveRecipients',
    'ValidateCert',
    'Find',
] as const;

export type EasCommand = (typeof COMMANDS)[number];

// 5 to 8 belonged to commands dropped after 2.5, nothing reuses them
export const COMMAND_CODES: Readonly<Record<EasCommand, number>> = {
    Sync: 0,
    SendMail: 1,
    SmartForward: 2,
    SmartReply: 3,
    GetAttachment: 4,
    FolderSync: 9,
    FolderCreate: 10,
    FolderDelete: 11,
    FolderUpdate: 12,
    MoveItems: 13,
    GetItemEstimate: 14,
    MeetingResponse: 15,
    Search: 16,
    Settings: 17,
    Ping: 18,
    ItemOperations: 19,
    Provision: 20,
    ResolveRecipients: 21,
    ValidateCert: 22,
    Find: 23,
};

const BY_CODE = new Map<number, EasCommand>(
    COMMANDS.map((c) => [COMMAND_CODES[c], c]));

export function commandForCode(code: number): EasCommand | undefined {
    return BY_CODE.get(code);
}

export const PROTOCOL_VERSIONS = ['16.1', '16.0', '14.1', '14.0', '12.1', '12.0', '2.5'] as const;

export type ProtocolVersion = (typeof PROTOCOL_VERSIONS)[number];

// the base64 form packs the version into one byte, so 2.5 and 12.0 don't fit and have to go out
// as plain text
export function protocolVersionByte(version: string): number | undefined {
    const [major, minor] = version.split('.');
    const value = Number(major) * 10 + Number(minor);
    return Number.isInteger(value) && value >= 121 && value <= 255 ? value : undefined;
}

export function protocolVersionFromByte(value: number): string {
    return `${Math.floor(value / 10)}.${value % 10}`;
}
