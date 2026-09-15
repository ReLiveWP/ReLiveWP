namespace ReLiveWP.Backend.DeviceUpdate.Model;

public static class FragmentTypes
{
    public const string Core = "Core";
    public const string Extended = "Extended";
    public const string LocalizedProperties = "LocalizedProperties";
    public const string Eula = "Eula";
}

public class UpdateFragment
{
    public long UpdateRevisionId { get; set; }
    public Update? Update { get; set; }

    // Position within the revision list
    public int Ordinal { get; set; }

    public string FragmentType { get; set; } = "";
    public string Language { get; set; } = "";

    public string Xml { get; set; } = "";
}
