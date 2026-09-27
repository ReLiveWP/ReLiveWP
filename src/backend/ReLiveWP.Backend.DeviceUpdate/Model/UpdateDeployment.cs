namespace ReLiveWP.Backend.DeviceUpdate.Model;

// Per-ring override of an update's deployment action, which is the Deployment table MS-WUSP's
// abstract model has and we did not: until now the action came straight off the catalog row and was
// the same for every device.
//
// Keyed by UpdateId rather than RevisionId so a re-authored or re-crawled revision stays blocked
// without the row having to be rewritten. A missing row means "use the catalog's own action", so
// this is an override layer and not a replacement.
public class UpdateDeployment
{
    public DeviceRing Ring { get; set; }
    public Guid UpdateId { get; set; }

    public string Action { get; set; } = "";
    public string? Reason { get; set; }

    // Goes on the wire in place of the crawled date whenever this override applies. MS-WUSP treats
    // LastChangeTime as the deployment's change stamp, so leaving the 2013 date on a revision whose
    // action we just changed tells an already-synced device that nothing has changed.
    public DateTime LastChangeTime { get; set; }
}
