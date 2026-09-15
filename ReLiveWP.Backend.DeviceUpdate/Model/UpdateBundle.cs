namespace ReLiveWP.Backend.DeviceUpdate.Model;

public class UpdateBundle
{
    public long BundleRevisionId { get; set; }
    public Update? Bundle { get; set; }

    public Guid BundledUpdateId { get; set; }
    public int? BundledRevisionNumber { get; set; }
}
