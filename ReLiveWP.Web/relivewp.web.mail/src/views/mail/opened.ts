const KEY = "relivewp.mail.opened";

type Opened = Record<string, string>;

// sessionStorage, not local: which message you had open is where you were in this tab, not a
// preference. it should survive a refresh and die with the tab. private modes can refuse it
// outright, so every path here treats storage as a nicety rather than a place to keep state
function read(): Opened {
    try {
        const raw = sessionStorage.getItem(KEY);
        if (raw === null) return {};

        const parsed: unknown = JSON.parse(raw);
        return typeof parsed === "object" && parsed !== null ? parsed as Opened : {};
    } catch {
        return {};
    }
}

function write(opened: Opened): void {
    try {
        sessionStorage.setItem(KEY, JSON.stringify(opened));
    } catch {
        // nothing to do about it and nothing that depends on it
    }
}

export function recall(folderId: string): string | null {
    return read()[folderId] ?? null;
}

export function remember(folderId: string, messageId: string): void {
    const opened = read();
    if (opened[folderId] === messageId) return;

    write({ ...opened, [folderId]: messageId });
}

export function forget(folderId: string): void {
    const opened = read();
    if (!(folderId in opened)) return;

    const { [folderId]: _dropped, ...rest } = opened;
    write(rest);
}
