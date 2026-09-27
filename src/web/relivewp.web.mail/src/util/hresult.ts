const PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD = 0x80048821;

const MESSAGE_LOOKUP: Record<number, string> = {
    [PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD]: "The sign-in name or password does not match one in the ReLive account system.",
}

export const toString = (hr: number): string => {
    const hex = `0x${hr.toString(16).padStart(6, '0')}`;
    const message = MESSAGE_LOOKUP[hr];
    if (message) {
        return `${message} (${hex})`;
    }

    return hex;
}
