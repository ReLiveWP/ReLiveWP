namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyAlbumItem
{
    public Guid OwnerId { get; set; }

    // wmphotos | mobilephotos | twitterphotos
    public string Album { get; set; } = null!;

    public string Provider { get; set; } = null!;
    public string ProviderItemId { get; set; } = null!;

    public bool SuppressNotification { get; set; }

    public DateTimeOffset Created { get; set; }
}
