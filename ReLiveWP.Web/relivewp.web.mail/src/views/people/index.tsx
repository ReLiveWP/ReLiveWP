import "./people.scss";

import type { Contact } from "@relivewp/eas-store";
import { useTitle } from "@relivewp/ui";
import { useCallback, useState } from "preact/hooks";

import Page from "~/components/Page";
import { useSearch, useShell } from "~/state/shell";
import { useSync } from "~/state/sync";
import ContactList from "./ContactList";
import Favourites from "./Favourites";
import MeCard from "./MeCard";
import WhatsNew from "./WhatsNew";
import { useContacts, useContactsFolder } from "./useContacts";
import { useContactSearch } from "./useContactSearch";
import { usePhotos } from "./usePhotos";

const SEARCH = { placeholder: "search contacts" };

export default function People() {
    useTitle("people");
    useSearch(SEARCH);

    const { query } = useShell();
    const client = useSync().client.value;

    const { folderId, loaded, error: folderError } = useContactsFolder(client);
    const { contacts, favourites, me, loading, error } = useContacts(client, folderId);
    const { results, searching, error: searchError } = useContactSearch(
        client, folderId, query.value);

    const photos = usePhotos(client, folderId);
    const [selected, setSelected] = useState<string | null>(null);

    const select = useCallback((contact: Contact) => { setSelected(contact.id); }, []);

    const listed = results ?? contacts;
    const problem = folderError ?? error ?? searchError;

    return (
        <Page
            class="people-page"
            title="people"
            subtitle={contacts.length === 0 ? undefined : `${contacts.length} contacts`}
            sidebar={<MeCard me={me} photo={me === undefined ? undefined : photos.get(me.id)} />}
            detail={
                <aside class="contact-rail">
                    <div class="panel-head">
                        <h2>all contacts</h2>
                        <span class="panel-count">{contacts.length}</span>
                    </div>

                    {problem !== null && <p class="error">{problem}</p>}

                    {client === null && <p class="note">Connecting.</p>}

                    {client !== null && folderId === null && loaded && problem === null
                        && <p class="note">No contacts folder yet. Sync to fetch the folder list.</p>}

                    {folderId !== null && problem === null && (
                        loading && contacts.length === 0
                            ? <p class="note">Loading.</p>
                            : (
                                <ContactList
                                    contacts={listed}
                                    photos={photos}
                                    selected={selected}
                                    searching={results !== null || searching}
                                    onSelect={select}
                                />
                            )
                    )}
                </aside>
            }
        >
            <Favourites
                favourites={favourites}
                total={contacts.length}
                photos={photos}
                onSelect={select}
            />
            <WhatsNew />
        </Page>
    );
}
