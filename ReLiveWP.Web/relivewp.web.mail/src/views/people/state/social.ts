import { ENDPOINT_ACTIVITY } from "~/util/endpoints";

export type SocialAuthor = {
    cid: string | null,
    is_me: boolean,
    provider: string,
    external_id: string,
    display_name: string,
    screen_name: string,
    avatar_url: string,
    canonical_url: string,
};

export type SocialPhoto = {
    thumbnail_url: string,
    full_size_url: string,
    canonical_url: string,
    mime_type: string,
};

export type SocialEntry = {
    id: string,
    provider_id: string,
    type: "post" | "article",
    published: string,
    title: string,
    content: string,
    generator: string,
    canonical_url: string,
    categories: string[],
    can_reply: boolean,
    reply_count: number | null,
    author: SocialAuthor,
    photos: SocialPhoto[],
};

export type SocialFeed = {
    connected: boolean,
    entries: SocialEntry[],
};

export type SocialContactFeed = {
    cid: string,
    shared: boolean,
    entries: SocialEntry[],
};

const CACHE_LIFETIME = 60_000;

type Cached = { at: number, value: unknown };

// the server fetches from bluesky on every miss, so coming back to people within a minute
// reuses what it already answered rather than asking again
const cache = new Map<string, Cached>();

async function getJson<T>(fetcher: typeof fetch, url: string): Promise<T> {
    const hit = cache.get(url);
    if (hit !== undefined && Date.now() - hit.at < CACHE_LIFETIME) return hit.value as T;

    const response = await fetcher(url, { headers: { "Accept": "application/json" } });
    if (!response.ok) throw await failure(response);

    const value = await response.json() as T;
    cache.set(url, { at: Date.now(), value });

    return value;
}

export type SocialReplies = {
    activity_id: string,
    entries: SocialEntry[],
};

export class SocialError extends Error {
    constructor(public readonly status: number, public readonly code: string) {
        super(code);
    }
}

async function failure(response: Response): Promise<SocialError> {
    const detail = await response.json().catch(() => ({})) as { error?: string };
    return new SocialError(response.status, detail.error ?? `${response.status} ${response.statusText}`);
}

export function forgetSocial(prefix = ""): void {
    for (const key of cache.keys())
        if (key.startsWith(prefix)) cache.delete(key);
}

function repliesUrl(activityId: string, count: number): string {
    return `${ENDPOINT_ACTIVITY}/api/social/replies?activityId=${encodeURIComponent(activityId)}&count=${count}`;
}

export type SocialIdentity = {
    provider: string,
    external_id: string,
    handle: string,
    display_name: string,
    avatar_url: string,
};

export type SocialIdentities = {
    cid: string,
    identities: SocialIdentity[],
};

export type SocialProvider = {
    provider: string,
    name: string,
};

export type SocialProviders = {
    providers: SocialProvider[],
};

export function fetchProvidersAsync(fetcher: typeof fetch): Promise<SocialProviders> {
    return getJson(fetcher, `${ENDPOINT_ACTIVITY}/api/social/providers`);
}

function identitiesUrl(serverId: string): string {
    return `${ENDPOINT_ACTIVITY}/api/social/contacts/${encodeURIComponent(serverId)}/identities`;
}

function forgetContact(serverId: string, cid: string): void {
    forgetSocial(identitiesUrl(serverId));
    forgetSocial(`${ENDPOINT_ACTIVITY}/api/social/contacts/${encodeURIComponent(cid)}/feed`);
    forgetSocial(`${ENDPOINT_ACTIVITY}/api/social/contacts/${encodeURIComponent(cid)}/albums`);
}

export function fetchIdentitiesAsync(fetcher: typeof fetch, serverId: string): Promise<SocialIdentities> {
    return getJson(fetcher, identitiesUrl(serverId));
}

export async function bindIdentityAsync(
    fetcher: typeof fetch, serverId: string, provider: string, handle: string): Promise<SocialIdentities> {
    const response = await fetcher(identitiesUrl(serverId), {
        method: "POST",
        headers: { "Accept": "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ provider, handle }),
    });

    if (!response.ok) throw await failure(response);

    const bound = await response.json() as SocialIdentities;
    forgetContact(serverId, bound.cid);
    return bound;
}

export async function unbindIdentityAsync(
    fetcher: typeof fetch, serverId: string, cid: string, provider: string): Promise<void> {
    const response = await fetcher(`${identitiesUrl(serverId)}/${encodeURIComponent(provider)}`, { method: "DELETE" });
    if (!response.ok && response.status !== 404) throw await failure(response);

    forgetContact(serverId, cid);
}

export function fetchRepliesAsync(fetcher: typeof fetch, activityId: string, count = 20): Promise<SocialReplies> {
    return getJson(fetcher, repliesUrl(activityId, count));
}

export async function postReplyAsync(fetcher: typeof fetch, activityId: string, text: string): Promise<void> {
    const response = await fetcher(`${ENDPOINT_ACTIVITY}/api/social/replies`, {
        method: "POST",
        headers: { "Accept": "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ activity_id: activityId, text }),
    });

    if (!response.ok) throw await failure(response);

    forgetSocial(`${ENDPOINT_ACTIVITY}/api/social/replies?activityId=${encodeURIComponent(activityId)}`);
}

export function fetchWhatsNewAsync(fetcher: typeof fetch, count = 20): Promise<SocialFeed> {
    return getJson(fetcher, `${ENDPOINT_ACTIVITY}/api/social/feed?count=${count}`);
}

export function fetchContactFeedAsync(fetcher: typeof fetch, cid: string, count = 20): Promise<SocialContactFeed> {
    return getJson(fetcher, `${ENDPOINT_ACTIVITY}/api/social/contacts/${encodeURIComponent(cid)}/feed?count=${count}`);
}

export type SocialAlbumSummary = {
    resource_id: string,
    title: string,
    cover_url: string | null,
};

export type SocialAlbumPhoto = {
    ref: string,
    kind: "photo" | "video",
    thumbnail_url: string,
    full_size_url: string,
    alt: string | null,
    created: string,
    width: number,
    height: number,
};

export type SocialAlbums = {
    albums: SocialAlbumSummary[],
};

export type SocialContactAlbums = {
    cid: string,
    shared: boolean,
    albums: SocialAlbumSummary[],
};

export type SocialAlbum = {
    resource_id: string,
    title: string,
    photos: SocialAlbumPhoto[],
};

// a null cid is the signed-in user's own albums
function albumsUrl(cid: string | null): string {
    return cid === null
        ? `${ENDPOINT_ACTIVITY}/api/social/albums`
        : `${ENDPOINT_ACTIVITY}/api/social/contacts/${encodeURIComponent(cid)}/albums`;
}

export function fetchOwnAlbumsAsync(fetcher: typeof fetch): Promise<SocialAlbums> {
    return getJson(fetcher, albumsUrl(null));
}

export function fetchContactAlbumsAsync(fetcher: typeof fetch, cid: string): Promise<SocialContactAlbums> {
    return getJson(fetcher, albumsUrl(cid));
}

export function fetchAlbumAsync(fetcher: typeof fetch, cid: string | null, resourceId: string): Promise<SocialAlbum> {
    return getJson(fetcher, `${albumsUrl(cid)}/${encodeURIComponent(resourceId)}`);
}
