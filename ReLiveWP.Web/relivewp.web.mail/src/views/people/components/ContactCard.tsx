import type { Contact, PostalAddress } from "@relivewp/eas-store";
import type { ComponentChildren } from "preact";
import { useMemo, useState } from "preact/hooks";

import { normalizeEmail, normalizePhone, present, searchLinks, suggestLinks, type Aggregate } from "../state/aggregate";
import ContactName from "./ContactName";
import { initialFor, initialOf } from "../state/groups";
import type { SocialIdentity } from "../state/social";
import Tile from "./Tile";

const BLUESKY = "atproto";

type Field = {
    key: string,
    label: string,
    value: string,
    href?: string,
    lines?: string[],
};

export type LinkedAccounts = {
    identities: SocialIdentity[],
    loading: boolean,
    error: string | null,
    busy: boolean,
    bindError: string | null,
    bind: (provider: string, handle: string) => Promise<boolean>,
    unbind: (provider: string) => Promise<void>,
};

type Props = {
    contact: Contact | undefined,
    // every profile folded into this card, the contact itself included. absent means just the one
    members?: Contact[],
    photo: string | undefined,
    fallbackName?: string,
    fallbackEmail?: string | null,
    onBack?: () => void,
    links?: LinkedAccounts,
    onUnlink?: (member: Contact) => void,
    // the row this card is, everyone it could be joined to, and what joining does
    linkable?: {
        self: Aggregate,
        candidates: Aggregate[],
        onLink: (other: Aggregate) => void,
    },
};

function addressLines(address: PostalAddress): string[] {
    const locality = [address.city, address.state, address.postalCode].filter(present).join(" ");
    return [address.street, locality, address.country].filter(present);
}

// the keys double as the dedupe: one address that two linked profiles both carry shows once
function fieldsOf(members: Contact[]): Field[] {
    const fields = new Map<string, Field>();
    const add = (field: Field): void => { if (!fields.has(field.key)) fields.set(field.key, field); };

    for (const contact of members) {
        for (const email of contact.emails)
            add({ key: `email:${normalizeEmail(email.value)}`, label: email.label, value: email.value, href: `mailto:${email.value}` });

        for (const phone of contact.phones)
            add({ key: `phone:${normalizePhone(phone.value)}`, label: phone.label, value: phone.value, href: `tel:${phone.value}` });

        for (const im of contact.imAddresses)
            add({ key: `im:${im.value.trim().toLowerCase()}`, label: im.label, value: im.value });

        for (const address of contact.addresses) {
            const lines = addressLines(address);
            if (lines.length > 0)
                add({ key: `address:${lines.join("|").toLowerCase()}`, label: address.label, value: lines.join(", "), lines });
        }

        if (present(contact.webPage))
            add({ key: `web:${contact.webPage.trim().toLowerCase()}`, label: "web", value: contact.webPage, href: contact.webPage });

        if (contact.birthday !== null)
            add({ key: "birthday", label: "birthday", value: new Date(contact.birthday).toLocaleDateString() });
    }

    return [...fields.values()];
}

// the name comes apart into the parts eas stores, since that is what an edit would have to write
function editFieldsOf(contact: Contact): Field[] {
    const names: Field[] = [
        { key: "first", label: "first", value: contact.firstName ?? "" },
        { key: "middle", label: "middle", value: contact.middleName ?? "" },
        { key: "last", label: "last", value: contact.lastName ?? "" },
        { key: "nickname", label: "nickname", value: contact.nickname ?? "" },
        { key: "company", label: "company", value: contact.company ?? "" },
        { key: "jobTitle", label: "job title", value: contact.jobTitle ?? "" },
    ];

    return [...names, ...fieldsOf([contact]).filter((field) => field.key !== "birthday")];
}

type LinkedProfilesProps = {
    members: Contact[],
    editing: boolean,
    onUnlink?: (member: Contact) => void,
    linkable?: Props["linkable"],
};

type AccountRowProps = {
    tile: ComponentChildren,
    name: string,
    title?: string,
    note?: string | null,
    action?: ComponentChildren,
};

function AccountRow({ tile, name, title, note = null, action }: AccountRowProps) {
    return (
        <div class="card-account">
            {tile}
            <span class="card-account-name" title={title ?? name}>{name}</span>
            {note !== null && <span class="card-account-auto">{note}</span>}
            {action}
        </div>
    );
}

function UnlinkButton({ onClick }: { onClick: () => void }) {
    return <button type="button" class="card-account-remove" title="Unlink" onClick={onClick}>&times;</button>;
}

// suggestions until something is typed, then a plain name search, the way the device's link
// screen is laid out
function LinkPicker({ linkable }: { linkable: NonNullable<Props["linkable"]> }) {
    const [query, setQuery] = useState("");
    const trimmed = query.trim();

    const rows = useMemo(() => (trimmed === ""
        ? suggestLinks(linkable.self, linkable.candidates)
        : searchLinks(linkable.self, linkable.candidates, trimmed)),
    [trimmed, linkable.self, linkable.candidates]);

    const link = (other: Aggregate) => {
        linkable.onLink(other);
        setQuery("");
    };

    return (
        <>
            <div class="card-account-add">
                <input
                    type="text"
                    value={query}
                    placeholder="link a contact"
                    onInput={(event) => { setQuery((event.target as HTMLInputElement).value); }}
                />
            </div>

            {rows.map((other) => (
                <AccountRow
                    key={other.key}
                    tile={<span class="card-account-tile">{initialOf(other.primary)}</span>}
                    name={other.displayName}
                    action={<button type="button" class="text-button" onClick={() => { link(other); }}>link</button>}
                />
            ))}

            {trimmed !== "" && rows.length === 0 && <p class="note">Nobody by that name.</p>}
        </>
    );
}

function LinkedProfilesSection({ members, editing, onUnlink, linkable }: LinkedProfilesProps) {
    const unlinkable = editing && onUnlink !== undefined && members.length > 1;

    return (
        <section class="card-accounts">
            <h3 class="panel-label">linked profiles</h3>

            {members.map((member) => (
                <AccountRow
                    key={member.id}
                    tile={<span class="card-account-tile">{initialOf(member)}</span>}
                    name={member.displayName}
                    note={member.annotation?.originService}
                    action={unlinkable && <UnlinkButton onClick={() => { onUnlink(member); }} />}
                />
            ))}

            {editing && linkable !== undefined && <LinkPicker linkable={linkable} />}
        </section>
    );
}

function FieldView({ field }: { field: Field }) {
    if (field.lines !== undefined) {
        return (
            <dd class="card-lines">
                {field.lines.map((line) => <span key={line}>{line}</span>)}
            </dd>
        );
    }

    return (
        <dd title={field.value}>
            {field.href === undefined
                ? field.value
                : <a href={field.href} target={field.key === "web" ? "_blank" : undefined} rel="noopener noreferrer">{field.value}</a>}
        </dd>
    );
}

function IdentityRow({ identity, onUnlink }: { identity: SocialIdentity, onUnlink?: () => void }) {
    const bare = identity.handle === "" ? identity.display_name : identity.handle;
    const shown = identity.handle === "" ? bare : `@${bare}`;

    return (
        <AccountRow
            tile={<Tile class="card-account-tile" url={identity.avatar_url || undefined} initial={initialFor(bare)} />}
            name={shown}
            title={identity.display_name}
            action={onUnlink !== undefined && <UnlinkButton onClick={onUnlink} />}
        />
    );
}

function LinkedAccountsSection({ automatic, links, editing }: { automatic: string | null, links: LinkedAccounts, editing: boolean }) {
    const [handle, setHandle] = useState("");
    const trimmed = handle.trim();
    const hasBluesky = links.identities.some((identity) => identity.provider === BLUESKY);
    const empty = automatic === null && links.identities.length === 0;

    const submit = async () => {
        if (trimmed === "" || links.busy) return;
        if (await links.bind(BLUESKY, trimmed)) setHandle("");
    };

    return (
        <section class="card-accounts">
            <h3 class="panel-label">linked accounts</h3>

            {links.error !== null && <p class="error">{links.error}</p>}

            {automatic !== null && (
                <AccountRow
                    tile={<span class="card-account-tile card-account-live">L</span>}
                    name={automatic}
                    note={editing ? "auto" : null}
                />
            )}

            {links.identities.map((identity) => (
                <IdentityRow
                    key={`${identity.provider}:${identity.external_id}`}
                    identity={identity}
                    onUnlink={editing && !links.busy ? () => { void links.unbind(identity.provider); } : undefined}
                />
            ))}

            {!editing && empty && !links.loading && <p class="note">None yet.</p>}

            {editing && !hasBluesky && (
                <form class="card-account-add" onSubmit={(event) => { event.preventDefault(); void submit(); }}>
                    <input
                        type="text"
                        value={handle}
                        placeholder="handle.bsky.social"
                        disabled={links.busy}
                        onInput={(event) => { setHandle((event.target as HTMLInputElement).value); }}
                    />
                    <button type="submit" class="text-button" disabled={trimmed === "" || links.busy}>
                        {links.busy ? "linking" : "link"}
                    </button>
                </form>
            )}

            {links.bindError !== null && <p class="error">{links.bindError}</p>}
        </section>
    );
}

function roleOf(members: Contact[]): string {
    for (const member of members) {
        const role = [member.jobTitle, member.company].filter(present).join(", ");
        if (role !== "") return role;
    }

    return "";
}

export default function ContactCard({ contact, members, photo, fallbackName = "you", fallbackEmail = null, onBack, links, onUnlink, linkable }: Props) {
    const [editing, setEditing] = useState(false);

    const profiles = members ?? (contact === undefined ? [] : [contact]);
    const name = contact?.displayName ?? fallbackName;
    const initial = contact === undefined ? initialFor(name) : initialOf(contact);
    const role = roleOf(profiles);

    const fields = contact === undefined
        ? (fallbackEmail === null ? [] : [{ key: "email", label: "email", value: fallbackEmail, href: `mailto:${fallbackEmail}` }])
        : fieldsOf(profiles);

    const heading: ComponentChildren = contact === undefined ? name : <ContactName contact={contact} />;
    const editable = contact !== undefined && links !== undefined;
    const automatic = contact?.annotation?.wlid ?? null;

    return (
        <aside class={editing ? "contact-card editing" : "contact-card"}>
            <span class="card-accent" />

            <div class="card-actions">
                {editing
                    ? (
                        <>
                            <button type="button" class="card-action" onClick={() => { setEditing(false); }}>cancel</button>
                            <button type="button" class="card-action" onClick={() => { setEditing(false); }}>done</button>
                        </>
                    )
                    : (
                        <>
                            {onBack !== undefined && <button type="button" class="card-action card-back" onClick={onBack}>all contacts</button>}
                            {editable && <button type="button" class="card-action" onClick={() => { setEditing(true); }}>edit</button>}
                        </>
                    )}
            </div>

            <div class="card-identity">
                <Tile class="card-tile" url={photo} initial={initial} />
            </div>

            {editing && contact !== undefined
                ? (
                    <dl class="card-fields card-edit">
                        {editFieldsOf(contact).map((field) => (
                            <div key={field.key}>
                                <dt>{field.label}</dt>
                                <dd><input type="text" value={field.value} disabled /></dd>
                            </div>
                        ))}
                    </dl>
                )
                : (
                    <>
                        <p class="card-name">
                            <span class="card-heading">{heading}</span>
                            {role !== "" && <span class="card-role">{role}</span>}
                        </p>

                        {fields.length > 0 && (
                            <dl class="card-fields">
                                {fields.map((field) => (
                                    <div key={field.key}>
                                        <dt>{field.label}</dt>
                                        <FieldView field={field} />
                                    </div>
                                ))}
                            </dl>
                        )}
                    </>
                )}

            {(profiles.length > 1 || (editing && linkable !== undefined))
                && <LinkedProfilesSection members={profiles} editing={editing} onUnlink={onUnlink} linkable={linkable} />}

            {links !== undefined && <LinkedAccountsSection automatic={automatic} links={links} editing={editing} />}
        </aside>
    );
}
