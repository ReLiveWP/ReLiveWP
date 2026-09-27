import type { SocialAlbumPhoto } from "../state/social";

type Props = {
    photos: SocialAlbumPhoto[],
};

export default function PhotoGrid({ photos }: Props) {
    return (
        <div class="photo-grid">
            {photos.map((photo) => (
                <a
                    key={photo.ref}
                    class="photo-tile"
                    href={photo.full_size_url}
                    target="_blank"
                    rel="noopener noreferrer"
                    title={photo.alt ?? undefined}
                >
                    <img src={photo.thumbnail_url} alt={photo.alt ?? ""} loading="lazy" decoding="async" />
                    {photo.kind === "video" && <span class="photo-badge">video</span>}
                </a>
            ))}
        </div>
    );
}
