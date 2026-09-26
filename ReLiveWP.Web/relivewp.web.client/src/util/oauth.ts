export const OAUTH_CHANNEL = "a0eb0210-bc9a-4bc5-be15-44ff49b71027";

export type OAuthLinkResult = { connectionId: string } | { error: string };

const LINK_ERRORS = new Map<string, string>([
    ["denied", "you cancelled the account link, or that provider doesn't allow you to link that account."],
    ["expired", "you're too slow! token expired, try again"],
    ["failed", "something went wrong linking that account, try again"],
]);

export function describeLinkError(code: string): string {
    return LINK_ERRORS.get(code) ?? LINK_ERRORS.get("failed")!;
}
