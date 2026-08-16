import type { EasClient } from "@relivewp/eas-sync/host";
import type { Folder } from "@relivewp/eas-store";
import { useCallback, useMemo, useState } from "preact/hooks";

import { useFolders } from "~/hooks/useFolders";

const HIDDEN_KEY = "relivewp.calendar.hidden";

export const PALETTE = 8;

export type CalendarFolder = {
    id: string,
    name: string,
    colour: number,
    enabled: boolean,
};

export type Calendars = {
    calendars: CalendarFolder[],
    colours: Map<string, number>,
    error: string | null,
    toggle: (id: string) => void,
};

function readHidden(): string[] {
    try {
        const raw = localStorage.getItem(HIDDEN_KEY);
        const parsed: unknown = raw === null ? [] : JSON.parse(raw);

        return Array.isArray(parsed) ? parsed.filter((id): id is string => typeof id === "string") : [];
    } catch {
        return [];
    }
}

function writeHidden(hidden: string[]): void {
    try {
        localStorage.setItem(HIDDEN_KEY, JSON.stringify(hidden));
    } catch {
        // private browsing and a full quota both land here, and neither is worth failing a toggle over
    }
}

function inOrder(a: Folder, b: Folder): number {
    if ((a.type === 8) !== (b.type === 8)) return a.type === 8 ? -1 : 1;

    return a.name.localeCompare(b.name);
}

export function useCalendars(client: EasClient | null): Calendars {
    const { folders: all, error } = useFolders(client);
    const [hidden, setHidden] = useState<string[]>(readHidden);

    const folders = useMemo(
        () => all.filter((folder) => folder.class === "Calendar").sort(inOrder),
        [all],
    );

    const toggle = useCallback((id: string) => {
        setHidden((current) => {
            const next = current.includes(id) ? current.filter((held) => held !== id) : [...current, id];
            writeHidden(next);

            return next;
        });
    }, []);

    const calendars = useMemo(() => folders.map((folder, index) => ({
        id: folder.id,
        name: folder.name,
        colour: index % PALETTE,
        enabled: !hidden.includes(folder.id),
    })), [folders, hidden]);

    const colours = useMemo(
        () => new Map(calendars.map((calendar) => [calendar.id, calendar.colour])),
        [calendars],
    );

    return { calendars, colours, error, toggle };
}
