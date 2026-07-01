using System.ComponentModel.DataAnnotations;

namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyPhoto
{
    [Key]
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }
    public string Category { get; set; } = null!; // album key, e.g. "mobilephotos"
    public string FileName { get; set; } = null!;
    public string? ContentType { get; set; }
    public string? Summary { get; set; }

    // bytes live at the provider(s); we only keep their item ids (JSON list of { service, itemId, url })
    public string ProviderItemsJson { get; set; } = "[]";

    public DateTimeOffset Created { get; set; }
}
