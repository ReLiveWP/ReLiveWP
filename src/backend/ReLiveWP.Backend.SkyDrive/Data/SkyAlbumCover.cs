namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyAlbumCover
{
    public Guid OwnerId { get; set; }

    public string AlbumRef { get; set; } = null!;

    public string ResourceRef { get; set; } = null!;

    public DateTimeOffset Updated { get; set; }
}
