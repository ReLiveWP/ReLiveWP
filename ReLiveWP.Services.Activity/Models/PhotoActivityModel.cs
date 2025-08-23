namespace ReLiveWP.Services.Activity.Models;

public class PhotoActivityModel : ActivityModel
{
    public required string ThumbnailUrl { get; set; }
    public required string FullSizeUrl { get; set; }
    public required string MimeType { get; set; }
}
