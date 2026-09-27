namespace ReLiveWP.Backend.DeviceUpdate.Model;

public class UpdatePrerequisite
{
    public long UpdateRevisionId { get; set; }
    public Update? Update { get; set; }

    public Guid PrerequisiteUpdateId { get; set; }
    public int GroupId { get; set; }
    public bool IsCategory { get; set; }
}
