import type { EasClient } from "@relivewp/eas-sync/host";
import type { Contact } from "@relivewp/eas-store";
import { useMemo } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
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

const NONE: Contact[] = [];

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
    const { value, loading, error, reload } = useAsync(
        client === null || folderId === null
            ? null
            : async () => {
                const [contacts, favourites, me] = await Promise.all([
                    client.listContacts({ folderId, limit: LIMIT }),
                    client.listFavourites({ folderId, limit: 6 }),
                    client.meContact(folderId),
                ]);

                return { contacts, favourites, me };
            },
        [client, folderId],
        { keep: true });

    useFolderChanges(client, [folderId], reload);

    return {
        contacts: value?.contacts ?? NONE,
        favourites: value?.favourites ?? NONE,
        me: value?.me,
        loading,
        error,
    };
}
