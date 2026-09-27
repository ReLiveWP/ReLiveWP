import type { User } from "./auth-types";

export type Bootstrap =
    | { kind: "ok", user: User }
    | { kind: "rejected" }
    | { kind: "unreachable" };

// the distinction this draws is the whole reason the app survives a reload with no network:
// only a refusal invalidates the session, everything else leaves it alone
export async function bootstrapUser(request: typeof fetch, url: string): Promise<Bootstrap> {
    let response: Response;

    try {
        response = await request(url, {
            method: "GET",
            headers: { 'Accept': "application/json" },
        });
    } catch {
        return { kind: "unreachable" };
    }

    if (response.status === 401 || response.status === 403) return { kind: "rejected" };
    if (!response.ok) return { kind: "unreachable" };

    try {
        return { kind: "ok", user: await response.json() as User };
    } catch {
        return { kind: "unreachable" };
    }
}
