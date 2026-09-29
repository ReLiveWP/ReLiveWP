namespace ReLiveWP.ServiceDefaults.Contacts;

// the one network ViewABNetworks hands the phone, which every linked social account sits behind.
// the device then names that network's own identity with the DomainTag, as both SourceId and ObjectId
public static class AggregateNetwork
{
    public const int DomainId = 22;
    public const string DomainTag = "TWITR";
}
