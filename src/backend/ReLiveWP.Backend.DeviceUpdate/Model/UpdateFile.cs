namespace ReLiveWP.Backend.DeviceUpdate.Model;

public class UpdateFile
{
    public long UpdateRevisionId { get; set; }
    public Update? Update { get; set; }

    public string FileName { get; set; } = "";
    public long Size { get; set; }

    public string? DigestSha1 { get; set; }
    public string? DigestSha256 { get; set; }

    public DateTime? Modified { get; set; }
    public string? SourceUrl { get; set; }
    public string? LocalPath { get; set; }
}
