import type { SocialAlbumSummary } from "../state/social";

type Props = {
    albums: SocialAlbumSummary[],
    hrefFor: (resourceId: string) => string,
};

export default function AlbumGrid({ albums, hrefFor }: Props) {
    return (
        <div class="album-grid">
            {albums.map((album) => (
                <a key={album.resource_id} class="album-tile" href={hrefFor(album.resource_id)} title={album.title}>
                    {album.cover_url === null
                        ? <span class="album-blank" />
                        : <img src={album.cover_url} alt="" loading="lazy" decoding="async" />}
                    <span class="album-caption">{album.title}</span>
                </a>
            ))}
        </div>
    );
}
