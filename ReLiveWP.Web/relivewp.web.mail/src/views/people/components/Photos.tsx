import type { ComponentChildren } from "preact";

import AlbumGrid from "./AlbumGrid";
import PhotoGrid from "./PhotoGrid";
import { useAlbum, useAlbums, type AlbumScope, type AlbumsState } from "../hooks/useAlbums";

export type PhotoSubject =
    | { kind: "own" }
    | { kind: "contact", name: string, cid: string | null };

type Props = {
    tabs: ComponentChildren,
    subject: PhotoSubject,
    albumId: string | null,
    albumHref: (resourceId: string | null) => string,
};

function scopeOf(subject: PhotoSubject): AlbumScope | null {
    return subject.kind === "own" ? "own" : subject.cid;
}

function albumsNoteFor(subject: PhotoSubject, { albums, shared, loading }: AlbumsState): string | null {
    if (loading) return albums.length === 0 ? "Loading." : null;
    if (subject.kind === "contact" && !shared) return "Not sharing any photos with you.";

    return albums.length === 0 ? "No albums yet." : null;
}

function RefreshButton({ loading, onClick }: { loading: boolean, onClick: () => void }) {
    return (
        <button type="button" class="panel-action" onClick={onClick} disabled={loading}>
            {loading ? "refreshing" : "refresh"}
        </button>
    );
}

function AlbumList({ tabs, subject, albumHref }: Omit<Props, "albumId">) {
    const listing = useAlbums(scopeOf(subject));
    const { albums, loading, error, refresh } = listing;
    const note = error === null ? albumsNoteFor(subject, listing) : null;

    return (
        <>
            <div class="panel-head">
                {tabs}
                <RefreshButton loading={loading} onClick={refresh} />
            </div>

            {error !== null && <p class="error">{error}</p>}
            {note !== null && <p class="note">{note}</p>}

            {albums.length > 0 && <AlbumGrid albums={albums} hrefFor={albumHref} />}
        </>
    );
}

function AlbumView({ tabs, subject, albumId, albumHref }: Props & { albumId: string }) {
    const { album, loading, error, refresh } = useAlbum(scopeOf(subject), albumId);
    const count = album?.photos.length ?? 0;

    return (
        <>
            <div class="panel-head">
                {tabs}
                <RefreshButton loading={loading} onClick={refresh} />
            </div>

            <div class="album-head">
                <a class="album-back" href={albumHref(null)}>all albums</a>
                {album !== undefined && (
                    <div class="album-info">
                        <h3 class="album-title">{album.title}</h3>
                        <span class="panel-count">{count === 1 ? "1 photo" : `${count} photos`}</span>
                    </div>
                )}
            </div>

            {error !== null && <p class="error">{error}</p>}
            {loading && album === undefined && <p class="note">Loading.</p>}
            {album !== undefined && count === 0 && <p class="note">Nothing in this album.</p>}

            {album !== undefined && count > 0 && <PhotoGrid photos={album.photos} />}
        </>
    );
}

export default function Photos({ tabs, subject, albumId, albumHref }: Props) {
    if (subject.kind === "contact" && subject.cid === null) {
        return (
            <section class="photos">
                <div class="panel-head">{tabs}</div>
                <p class="note">Link an account on {subject.name}&rsquo;s card to see their photos.</p>
            </section>
        );
    }

    return (
        <section class="photos">
            {albumId === null
                ? <AlbumList tabs={tabs} subject={subject} albumHref={albumHref} />
                : <AlbumView tabs={tabs} subject={subject} albumId={albumId} albumHref={albumHref} />}
        </section>
    );
}
