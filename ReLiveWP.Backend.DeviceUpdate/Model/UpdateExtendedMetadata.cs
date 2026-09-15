namespace ReLiveWP.Backend.DeviceUpdate.Model;

public class UpdateExtendedMetadata
{
    public long RevisionId { get; set; }
    public Update? Update { get; set; }

    public string Xml { get; set; } = "";
}
