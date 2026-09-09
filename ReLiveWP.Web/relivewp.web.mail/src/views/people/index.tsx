import "./people.scss";

import type { Contact } from "@relivewp/eas-store";
import { useTitle } from "@relivewp/ui";
import { useLocation } from "preact-iso";
import { useCallback, useMemo } from "preact/hooks";

import Page from "~/components/Page";
import { useAppState } from "~/state/app-state";
import { useSearch, useShell } from "~/state/shell";
import { useSync } from "~/state/sync";
import { peopleAlbumsPath, peoplePath } from "~/util/routes";
import {
    aggregateContacts, aggregateIndex, aggregatesFor, liveMemberOf, photoOf, type Aggregate,
} from "./state/aggregate";
import ContactActivity from "./components/ContactActivity";
import ContactCard from "./components/ContactCard";
import ContactList from "./components/ContactList";
import Favourites from "./components/Favourites";
import MeCard from "./components/MeCard";
import PeopleTabs, { type PeopleTab } from "./components/PeopleTabs";
import Photos from "./components/Photos";
import WhatsNew from "./components/WhatsNew";
import { useContactIdentities } from "./hooks/useContactIdentities";
import { useContacts, useContactsFolder } from "./hooks/useContacts";
import { useContactSearch } from "./hooks/useContactSearch";
import { useAggregateOverrides } from "./state/overrides";
import { usePhotos } from "./hooks/usePhotos";

const SEARCH = { placeholder: "search contacts" };

type Props = {
    contactId?: string,
    tab?: PeopleTab,
    albumId?: string,
};

export default function People({ contactId, tab = "feed", albumId }: Props) {
    useTitle("people");
    useSearch(SEARCH);

    const { route } = useLocation();
    const { query } = useShell();
    const user = useAppState().user.value;
    const client = useSync().client.value;

    const { folderId, loaded, error: folderError } = useContactsFolder(client);
    const { contacts, favourites, me, loading, error } = useContacts(client, folderId);
    const { results, searching, error: searchError } = useContactSearch(
        client, folderId, query.value);

    const photos = usePhotos(client, folderId);
    const selected = contactId ?? null;

    const { overrides, unlink, link } = useAggregateOverrides(user?.id ?? null);
    const aggregates = useMemo(() => aggregateContacts(contacts, { overrides }), [contacts, overrides]);
    const aggregateOf = useMemo(() => aggregateIndex(aggregates), [aggregates]);

    // the url carries the primary member, so a link keeps working however the rest get folded in
    const select = useCallback((aggregate: Aggregate) => { route(peoplePath(aggregate.key)); }, [route]);
    const selectContact = useCallback((contact: Contact) => {
        route(peoplePath(aggregateOf.get(contact.id)?.key ?? contact.id));
    }, [route, aggregateOf]);
    const back = useCallback(() => { route(peoplePath()); }, [route]);

    const opened = selected === null ? undefined : aggregateOf.get(selected);

    const listed = useMemo(
        () => (results === null ? aggregates : aggregatesFor(results, aggregateOf)),
        [results, aggregates, aggregateOf]);
    const pinned = useMemo(() => aggregatesFor(favourites, aggregateOf), [favourites, aggregateOf]);
    const problem = folderError ?? error ?? searchError;

    const viewer = useMemo(() => ({
        name: me?.displayName ?? user?.username ?? "you",
        photo: me === undefined ? undefined : photos.get(me.id),
    }), [me, user, photos]);

    const liveMember = opened === undefined ? undefined : liveMemberOf(opened);
    const links = useContactIdentities(liveMember?.id ?? null);

    // a fresh binding hands back the cid before the annotation has made it round through eas
    const openedCid = liveMember?.annotation?.cid ?? (links.identities.length > 0 ? links.cid : null);

    const openedKey = opened?.key ?? null;
    const tabHref = useCallback(
        (which: PeopleTab) => (which === "feed" ? peoplePath(openedKey) : peopleAlbumsPath(openedKey)),
        [openedKey]);
    const albumHref = useCallback(
        (resourceId: string | null) => peopleAlbumsPath(openedKey, resourceId),
        [openedKey]);

    const tabs = <PeopleTabs active={tab} hrefFor={tabHref} />;

    const panel = opened === undefined
        ? (tab === "feed"
            ? <WhatsNew tabs={tabs} contacts={contacts} photos={photos} viewer={viewer} onSelect={selectContact} />
            : <Photos tabs={tabs} subject={{ kind: "own" }} albumId={albumId ?? null} albumHref={albumHref} />)
        : (tab === "feed"
            ? <ContactActivity key={opened.key} tabs={tabs} contact={opened.primary} cid={openedCid} contacts={contacts} photos={photos} viewer={viewer} />
            : (
                <Photos
                    key={opened.key}
                    tabs={tabs}
                    subject={{ kind: "contact", name: opened.primary.displayName, cid: openedCid }}
                    albumId={albumId ?? null}
                    albumHref={albumHref}
                />
            ));

    const content = opened === undefined
        ? (
            <>
                <Favourites
                    favourites={pinned}
                    total={aggregates.length}
                    photos={photos}
                    onSelect={select}
                />
                {panel}
            </>
        )
        : panel;

    const sidebar = opened === undefined
        ? <MeCard me={me} photo={me === undefined ? undefined : photos.get(me.id)} />
        : (
            <ContactCard
                key={opened.key}
                contact={opened.primary}
                members={opened.members}
                photo={photoOf(opened, photos)}
                onBack={back}
                links={links}
                onUnlink={(member) => { unlink(member, opened.members); }}
                linkable={{ self: opened, candidates: aggregates, onLink: (other) => { link(opened, other); } }}
            />
        );

    return (
        <Page
            class={opened === undefined ? "people-page" : "people-page people-open"}
            title="people"
            subtitle={aggregates.length === 0 ? undefined : `${aggregates.length} contacts`}
            sidebar={sidebar}
            detail={
                <aside class="contact-rail">
                    <div class="panel-head">
                        <h2>all contacts</h2>
                        <span class="panel-count">{aggregates.length}</span>
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
                                    aggregates={listed}
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
            {selected !== null && opened === undefined && !loading && contacts.length > 0
                && <p class="note">That contact is not in the address book any more.</p>}
            {content}
        </Page>
    );
}
