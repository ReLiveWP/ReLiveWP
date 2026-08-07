namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyProviderAlbum
{
    public Guid OwnerId { get; set; }

    // wmphotos | mobilephotos | twitterphotos
    public string Album { get; set; } = null!;

    public string Provider { get; set; } = null!;
    public string ProviderAlbumId { get; set; } = null!;

    public DateTimeOffset Created { get; set; }
}
