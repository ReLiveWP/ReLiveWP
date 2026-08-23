function base64Url(bytes: Uint8Array): string {
    let binary = "";
    for (const byte of bytes) binary += String.fromCharCode(byte);

    return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function randomBase64Url(length: number): string {
    const bytes = new Uint8Array(length);
    crypto.getRandomValues(bytes);

    return base64Url(bytes);
}

export function createState(): string {
    return randomBase64Url(32);
}

export function createVerifier(): string {
    return randomBase64Url(32);
}

export async function challengeFor(verifier: string): Promise<{ challenge: string, method: string }> {
    if (!crypto?.subtle) 
        return { challenge: verifier, method: "plain" };

    const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
    return { challenge: base64Url(new Uint8Array(digest)), method: "S256" };
}
