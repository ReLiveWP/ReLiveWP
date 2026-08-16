import type { EasClient } from "@relivewp/eas-sync/host";
import type { Contact } from "@relivewp/eas-store";
import { useCallback, useEffect, useMemo, useState } from "preact/hooks";

import { useFolderChanges } from "~/hooks/useFolderChanges";
import { useFolders } from "~/hooks/useFolders";

const LIMIT = 5000;

export type ContactsState = {
    contacts: Contact[],
    favourites: Contact[],
    me: Contact | undefined,
    loading: boolean,
    error: string | null,
};

const EMPTY: ContactsState = {
    contacts: [], favourites: [], me: undefined, loading: false, error: null,
};

function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}

// there is normally exactly one, so this resolves to a folder rather than a list to choose from
export function useContactsFolder(client: EasClient | null): {
    folderId: string | null,
    loaded: boolean,
    error: string | null,
} {
    const { folders, loaded, error } = useFolders(client);

    const folderId = useMemo(() => {
        const contacts = folders.filter((folder) => folder.class === "Contact");
        const preferred = contacts.find((folder) => folder.role === "contacts") ?? contacts[0];
        return preferred?.id ?? null;
    }, [folders]);

    return { folderId, loaded, error };
}

export function useContacts(client: EasClient | null, folderId: string | null): ContactsState {
    const [state, setState] = useState<ContactsState>(EMPTY);

    const load = useCallback(() => {
        if (client === null || folderId === null) {
            setState(EMPTY);
            return;
        }

        let live = true;
        setState((prev) => ({ ...prev, loading: true, error: null }));

        Promise.all([
            client.listContacts({ folderId, limit: LIMIT }),
            client.listFavourites({ folderId, limit: 6 }),
            client.meContact(folderId),
        ]).then(([contacts, favourites, me]) => {
            if (!live) return;
            setState({ contacts, favourites, me, loading: false, error: null });
        }).catch((thrown: unknown) => {
            if (!live) return;
            setState((prev) => ({ ...prev, loading: false, error: reason(thrown) }));
        });

        return () => { live = false; };
    }, [client, folderId]);

    useEffect(() => load(), [load]);
    useFolderChanges(client, [folderId], load);

    return state;
}
