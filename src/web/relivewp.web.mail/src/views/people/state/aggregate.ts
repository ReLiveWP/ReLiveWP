import { isMeContact, type Contact, type LabelledValue } from "@relivewp/eas-store";

export type OverrideKind = "merge" | "split";
export type AggregateOverrides = ReadonlyMap<string, OverrideKind>;

export function pairKey(a: string, b: string): string {
    return a < b ? `${a}|${b}` : `${b}|${a}`;
}

export function withOverride(overrides: AggregateOverrides, a: string, b: string, kind: OverrideKind | null): AggregateOverrides {
    const next = new Map(overrides);
    const key = pairKey(a, b);

    if (kind === null) next.delete(key);
    else next.set(key, kind);

    return next;
}

export function withoutMember(overrides: AggregateOverrides, member: Contact, others: Contact[]): AggregateOverrides {
    let next = overrides;
    for (const other of others)
        if (other.id !== member.id) next = withOverride(next, member.id, other.id, "split");

    return next;
}

export type Aggregate = {
    key: string,
    primary: Contact,
    members: Contact[],
    displayName: string,
    sortName: string,
};

export type AggregateOptions = {
    // which account a contact came from, null when unknown.
    sourceOf?: (contact: Contact) => string | null,
    // WP7 links two nameless contacts that share a company. off by default, it merges colleagues
    linkOnCompany?: boolean,
    // the user's own merges and splits, applied over everything the rules would do
    overrides?: AggregateOverrides,
};

type NameSource = "name" | "nickname" | "company" | "email" | "phone" | "display";

type AutoLinkingName = {
    text: string,
    source: NameSource,
};

type Draft = {
    members: Contact[],
    cid: string | null,
    firstLastKey: string | null,
    autoNameKey: string | null,
};

const PHONE_LABEL_ORDER = ["mobile", "home", "work", "home2", "work2", "company", "pager"];

const MIN_PHONE_TAIL = 7;

export function present(value: string | null | undefined): value is string {
    return value !== null && value !== undefined && value.trim().length > 0;
}

export function normalizeName(value: string): string {
    return value
        .normalize("NFKD")
        .replace(/[\p{M}\p{P}\p{S}]/gu, "")
        .replace(/\s+/g, " ")
        .trim()
        .toLowerCase();
}

export function normalizeEmail(value: string): string {
    return value.trim().toLowerCase();
}

export function normalizePhone(value: string): string {
    return value.replace(/\D/g, "");
}

// a national trunk zero is what stops "07700 900123" being a suffix of "+44 7700 900123"
function withoutTrunkPrefix(digits: string): string {
    return digits.startsWith("0") ? digits.slice(1) : digits;
}

// the phone index on the device matches on the trailing digits, so a national and an
// international spelling of one number line up. same idea here
export function phonesMatch(left: string, right: string): boolean {
    const a = withoutTrunkPrefix(left);
    const b = withoutTrunkPrefix(right);
    const shorter = a.length <= b.length ? a : b;
    const longer = shorter === a ? b : a;

    return shorter.length >= MIN_PHONE_TAIL && longer.endsWith(shorter);
}

function fullName(contact: Contact): string {
    return [contact.firstName, contact.middleName, contact.lastName]
        .filter(present)
        .map((part) => part.trim())
        .join(" ")
        .replace(/\s+/g, " ");
}

function orderedPhones(phones: LabelledValue[]): LabelledValue[] {
    const rank = (phone: LabelledValue): number => {
        const index = PHONE_LABEL_ORDER.indexOf(phone.label);
        return index === -1 ? PHONE_LABEL_ORDER.length : index;
    };

    return [...phones].sort((a, b) => rank(a) - rank(b));
}

export function autoLinkingName(contact: Contact): AutoLinkingName | null {
    const name = fullName(contact);
    if (name.length > 0) return { text: name, source: "name" };

    if (present(contact.nickname)) return { text: contact.nickname.trim(), source: "nickname" };
    if (present(contact.company)) return { text: contact.company.trim(), source: "company" };

    const email = contact.emails.find((entry) => present(entry.value));
    if (email !== undefined) return { text: email.value.trim(), source: "email" };

    const phone = orderedPhones(contact.phones).find((entry) => present(entry.value));
    if (phone !== undefined) return { text: phone.value.trim(), source: "phone" };

    if (present(contact.displayName)) return { text: contact.displayName.trim(), source: "display" };

    return null;
}

function firstLastKeyOf(contact: Contact): string | null {
    if (!present(contact.firstName) || !present(contact.lastName)) return null;

    return `${normalizeName(contact.firstName)}|${normalizeName(contact.lastName)}`;
}

function autoNameKeyOf(name: AutoLinkingName | null): string | null {
    if (name === null) return null;

    const key = name.source === "email" ? normalizeEmail(name.text)
        : name.source === "phone" ? normalizePhone(name.text)
        : normalizeName(name.text);

    return key.length > 0 ? key : null;
}

export function originOf(contact: Contact): string | null {
    const service = contact.annotation?.originService;
    if (service === null || service === undefined || service.length === 0) return null;

    const collection = contact.annotation?.originCollection ?? "";
    return `${service}/${collection}`;
}

function cidOf(contact: Contact): string | null {
    const cid = contact.annotation?.cid?.trim().toLowerCase();
    return cid !== undefined && cid.length > 0 ? cid : null;
}

function emailsOf(contact: Contact): string[] {
    return contact.emails.filter((entry) => present(entry.value)).map((entry) => normalizeEmail(entry.value));
}

function phonesOf(contact: Contact): string[] {
    return contact.phones.filter((entry) => present(entry.value)).map((entry) => normalizePhone(entry.value));
}

function sharesEmail(contact: Contact, draft: Draft): boolean {
    const mine = emailsOf(contact);
    if (mine.length === 0) return false;

    return draft.members.some((member) => emailsOf(member).some((email) => mine.includes(email)));
}

function sharesPhone(contact: Contact, draft: Draft): boolean {
    const mine = phonesOf(contact);
    if (mine.length === 0) return false;

    return draft.members.some((member) =>
        phonesOf(member).some((theirs) => mine.some((ours) => phonesMatch(ours, theirs))));
}

function middleNameConflicts(contact: Contact, draft: Draft): boolean {
    if (!present(contact.middleName)) return false;
    const mine = normalizeName(contact.middleName);

    return draft.members.some((member) =>
        present(member.middleName) && normalizeName(member.middleName) !== mine);
}

function sharesSource(contact: Contact, draft: Draft, sourceOf: (contact: Contact) => string | null): boolean {
    const mine = sourceOf(contact);
    if (mine === null) return false;

    return draft.members.some((member) => sourceOf(member) === mine);
}

class Index {
    private readonly byKey = new Map<string, Set<Draft>>();

    add(key: string | null, draft: Draft): void {
        if (key === null) return;

        const drafts = this.byKey.get(key);
        if (drafts === undefined) this.byKey.set(key, new Set([draft]));
        else drafts.add(draft);
    }

    // exactly one hit or nothing, the way the device refuses to guess between two candidates
    single(key: string | null): Draft | null {
        if (key === null) return null;

        const drafts = this.byKey.get(key);
        if (drafts === undefined || drafts.size !== 1) return null;

        return drafts.values().next().value ?? null;
    }
}

function byStableOrder(a: Contact, b: Contact): number {
    const bySortName = a.sortName.localeCompare(b.sortName);
    return bySortName !== 0 ? bySortName : a.id.localeCompare(b.id);
}

function toAggregate(draft: Draft): Aggregate {
    const primary = draft.members[0]!;

    return {
        key: primary.id,
        primary,
        members: draft.members,
        displayName: primary.displayName,
        sortName: primary.sortName,
    };
}

export function aggregateContacts(contacts: Contact[], options: AggregateOptions = {}): Aggregate[] {
    const sourceOf = options.sourceOf ?? originOf;
    const overrides = options.overrides ?? new Map<string, never>();
    const drafts: Draft[] = [];

    const splitFrom = (contact: Contact, draft: Draft): boolean =>
        draft.members.some((member) => overrides.get(pairKey(contact.id, member.id)) === "split");
    const mergedInto = (contact: Contact): Draft | undefined => {
        if (overrides.size === 0) return undefined;
        return drafts.find((draft) => draft.members.some((member) => overrides.get(pairKey(contact.id, member.id)) === "merge"));
    };

    const byFirstLast = new Index();
    const byAutoName = new Index();
    const byCid = new Map<string, Draft>();

    const open = (contact: Contact, cid: string | null, firstLastKey: string | null, autoNameKey: string | null): void => {
        const draft: Draft = { members: [contact], cid, firstLastKey, autoNameKey };
        drafts.push(draft);

        byFirstLast.add(firstLastKey, draft);
        byAutoName.add(autoNameKey, draft);
        if (cid !== null) byCid.set(cid, draft);
    };

    const join = (contact: Contact, draft: Draft, cid: string | null): void => {
        draft.members.push(contact);

        if (cid !== null && draft.cid === null) {
            draft.cid = cid;
            byCid.set(cid, draft);
        }
    };

    for (const contact of [...contacts].sort(byStableOrder)) {
        const cid = cidOf(contact);
        const name = autoLinkingName(contact);
        const firstLastKey = firstLastKeyOf(contact);
        const autoNameKey = autoNameKeyOf(name);

        if (isMeContact(contact)) {
            drafts.push({ members: [contact], cid, firstLastKey, autoNameKey });
            continue;
        }

        const forced = mergedInto(contact);
        if (forced !== undefined && !splitFrom(contact, forced)) {
            join(contact, forced, cid);
            continue;
        }

        const known = cid === null ? undefined : byCid.get(cid);
        if (known !== undefined && !splitFrom(contact, known)) {
            join(contact, known, cid);
            continue;
        }

        let candidate = byFirstLast.single(firstLastKey);
        let matchedOnAutoName = false;

        if (candidate === null) {
            const companyOnly = name?.source === "company" && options.linkOnCompany !== true;
            candidate = companyOnly ? null : byAutoName.single(autoNameKey);
            matchedOnAutoName = candidate !== null;
        }

        if (candidate === null
            || splitFrom(contact, candidate)
            || (cid !== null && candidate.cid !== null && candidate.cid !== cid)) {
            open(contact, cid, firstLastKey, autoNameKey);
            continue;
        }

        const needsCorroboration =
            (matchedOnAutoName && name !== null && !name.text.includes(" "))
            || middleNameConflicts(contact, candidate)
            || sharesSource(contact, candidate, sourceOf);

        if (needsCorroboration && !sharesEmail(contact, candidate) && !sharesPhone(contact, candidate)) {
            open(contact, cid, firstLastKey, autoNameKey);
            continue;
        }

        join(contact, candidate, cid);
    }

    return drafts.map(toAggregate);
}

// member id to the row it was folded into, so a feed author or an old url still lands somewhere
export function aggregateIndex(aggregates: Aggregate[]): ReadonlyMap<string, Aggregate> {
    const index = new Map<string, Aggregate>();

    for (const aggregate of aggregates)
        for (const member of aggregate.members) index.set(member.id, aggregate);

    return index;
}

export function singleAggregate(contact: Contact): Aggregate {
    return {
        key: contact.id,
        primary: contact,
        members: [contact],
        displayName: contact.displayName,
        sortName: contact.sortName,
    };
}

// search hits and favourites come back as contacts. two hits inside one row collapse to that row,
// and a contact the pass has not seen yet stands on its own rather than vanishing
export function aggregatesFor(contacts: Contact[], index: ReadonlyMap<string, Aggregate>): Aggregate[] {
    const seen = new Set<string>();
    const rows: Aggregate[] = [];

    for (const contact of contacts) {
        const aggregate = index.get(contact.id) ?? singleAggregate(contact);
        if (seen.has(aggregate.key)) continue;

        seen.add(aggregate.key);
        rows.push(aggregate);
    }

    return rows;
}

export function photoOf(aggregate: Aggregate, photos: ReadonlyMap<string, string>): string | undefined {
    for (const member of aggregate.members) {
        const url = photos.get(member.id);
        if (url !== undefined) return url;
    }

    return undefined;
}

// the member carrying the live identity, which is where the feed and the bound accounts hang off
export function liveMemberOf(aggregate: Aggregate): Contact {
    return aggregate.members.find((member) => cidOf(member) !== null) ?? aggregate.primary;
}

function aggregateCid(aggregate: Aggregate): string | null {
    for (const member of aggregate.members) {
        const cid = cidOf(member);
        if (cid !== null) return cid;
    }

    return null;
}

// the same exclusions the device applies to its suggestion list: not yourself, not the me
// contact, and never two different live identities in one row
export function canLink(target: Aggregate, candidate: Aggregate): boolean {
    if (candidate.key === target.key) return false;
    if (target.members.some(isMeContact) || candidate.members.some(isMeContact)) return false;

    const mine = aggregateCid(target);
    const theirs = aggregateCid(candidate);
    return mine === null || theirs === null || mine === theirs;
}

// "First Last", plus each half of a hyphenated surname, per profile so one account's first name
// never crosses another's surname
function nameVariants(contact: Contact): string[] {
    if (!present(contact.firstName) || !present(contact.lastName)) return [];

    const first = contact.firstName.trim();
    const last = contact.lastName.trim();
    const variants = [`${first} ${last}`];

    const dash = last.indexOf("-");
    if (dash > 0 && dash < last.length - 1)
        variants.push(`${first} ${last.slice(0, dash)}`, `${first} ${last.slice(dash + 1)}`);

    return variants.map(normalizeName);
}

type MatchKeys = {
    names: Set<string>,
    emails: Set<string>,
    phones: string[],
};

function matchKeysOf(aggregate: Aggregate): MatchKeys {
    const keys: MatchKeys = { names: new Set(), emails: new Set(), phones: [] };

    for (const member of aggregate.members) {
        for (const name of nameVariants(member)) keys.names.add(name);
        keys.names.add(normalizeName(member.sortName));
        keys.names.add(normalizeName(member.displayName));
        for (const email of emailsOf(member)) keys.emails.add(email);
        keys.phones.push(...phonesOf(member));
    }

    keys.names.delete("");
    return keys;
}

function keysOverlap(mine: MatchKeys, theirs: MatchKeys): boolean {
    for (const name of mine.names) if (theirs.names.has(name)) return true;
    for (const email of mine.emails) if (theirs.emails.has(email)) return true;

    return mine.phones.some((ours) => theirs.phones.some((other) => phonesMatch(ours, other)));
}

export function suggestLinks(target: Aggregate, all: Aggregate[]): Aggregate[] {
    const mine = matchKeysOf(target);

    return all
        .filter((candidate) => canLink(target, candidate) && keysOverlap(mine, matchKeysOf(candidate)))
        .sort((a, b) => a.displayName.localeCompare(b.displayName));
}

export function searchLinks(target: Aggregate, all: Aggregate[], query: string, limit = 20): Aggregate[] {
    const wanted = normalizeName(query);
    if (wanted.length < 2) return [];

    const hits: Aggregate[] = [];
    for (const candidate of all) {
        if (!canLink(target, candidate)) continue;

        const named = candidate.members.some((member) =>
            normalizeName(member.displayName).includes(wanted) || normalizeName(member.sortName).includes(wanted));
        if (!named) continue;

        hits.push(candidate);
        if (hits.length >= limit) break;
    }

    return hits;
}

// joining two rows: any split that was keeping them apart goes, and one merge holds them together
export function withLinked(overrides: AggregateOverrides, target: Aggregate, other: Aggregate): AggregateOverrides {
    let next = overrides;

    for (const mine of target.members)
        for (const theirs of other.members)
            if (next.get(pairKey(mine.id, theirs.id)) === "split") next = withOverride(next, mine.id, theirs.id, null);

    return withOverride(next, target.primary.id, other.primary.id, "merge");
}
