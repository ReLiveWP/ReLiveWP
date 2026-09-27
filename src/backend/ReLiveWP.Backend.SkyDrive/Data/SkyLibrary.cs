using System.ComponentModel.DataAnnotations;

namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyLibrary
{
    [Key]
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    // e.g. "photos"
    public string Category { get; set; } = null!;

    // e.g. "Library"
    public string Type { get; set; } = null!;

    // private | shared | publicshared
    public string SharingLevel { get; set; } = "private";

    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? EmailKeyword { get; set; }

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }
}
