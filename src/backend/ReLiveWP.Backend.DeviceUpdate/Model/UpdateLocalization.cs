namespace ReLiveWP.Backend.DeviceUpdate.Model;

public class UpdateLocalization
{
    public long UpdateRevisionId { get; set; }
    public Update? Update { get; set; }

    public string Language { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? MoreInfoUrl { get; set; }
    public string? SupportUrl { get; set; }
}
