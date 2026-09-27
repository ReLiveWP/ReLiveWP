namespace ReLiveWP.Services.Activity.Models;

public abstract class ActivityModel
{
    public required string Id { get; set; }
    public required string CanonicalUrl { get; set; }
}
