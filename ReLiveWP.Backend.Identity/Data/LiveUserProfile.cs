namespace ReLiveWP.Backend.Identity.Data;

/// <summary>
/// Specifies how Live users can discover each other
/// </summary>
public enum LiveProfileVisibility
{
    /// <summary>
    /// No discovery
    /// </summary>
    Private,
    /// <summary>
    /// Discoverable if both users are in each other's address book
    /// </summary>
    Mutual,
    /// <summary>
    /// One way discovery
    /// </summary>
    Public
}

public class LiveUserProfile
{
    public Guid UserId { get; set; }
    public LiveUser User { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? PictureFileName { get; set; }
    public string? PictureContentType { get; set; }
    public string? PictureEtag { get; set; }

    public byte[]? PictureThumbnail { get; set; }
    public DateTime UpdatedAt { get; set; }

    public LiveProfileVisibility ProfileVisibility { get; set; } 
        = LiveProfileVisibility.Private;
}
