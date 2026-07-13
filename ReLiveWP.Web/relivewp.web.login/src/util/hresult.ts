const PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD = 0x80048821;

const MESSAGE_LOOKUP: { [code: number]: string } = {
    [PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD]: "That ReLive account or password isn't recognised. Please try again.",
};

export function toString(hr: number): string {
    const hex = "0x" + (hr >>> 0).toString(16).toUpperCase();
    const message = MESSAGE_LOOKUP[hr >>> 0];
    return message ? message + " (" + hex + ")" : hex;
}
