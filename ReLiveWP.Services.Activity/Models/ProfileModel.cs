namespace ReLiveWP.Services.Activity.Models;

public class ProfileModel
{
    public bool IsMe { get; set; }
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string ScreenName { get; set; }
    public required string AvatarUrl { get; set; }
    public required string CanonicalUrl { get; set; }
}
