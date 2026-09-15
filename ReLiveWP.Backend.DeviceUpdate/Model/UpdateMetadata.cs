namespace ReLiveWP.Backend.DeviceUpdate.Model;

public class UpdateMetadata
{
    public long RevisionId { get; set; }
    public Update? Update { get; set; }

    public string SyncXml { get; set; } = "";
}
