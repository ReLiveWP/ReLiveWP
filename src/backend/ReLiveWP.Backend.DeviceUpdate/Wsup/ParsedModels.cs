using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

// Intermediate parse results, decoupled from the EF entities so parsing has no DB dependency.

public class ParsedUpdate
{
    public long RevisionId { get; set; }
    public Guid UpdateId { get; set; }
    public int RevisionNumber { get; set; }
    public UpdateType UpdateType { get; set; }
    public bool IsLeaf { get; set; }
    public string DeploymentAction { get; set; } = "Evaluate";
    public bool IsBundle { get; set; }
    public DateTime? LastChangeTime { get; set; }
    public string SyncMetadataXml { get; set; } = "";

    public List<ParsedPrerequisite> Prerequisites { get; set; } = [];
    public List<ParsedBundle> Bundles { get; set; } = [];
}

public class ParsedPrerequisite
{
    public Guid PrerequisiteUpdateId { get; set; }
    public int GroupId { get; set; }
    public bool IsCategory { get; set; }
}

public class ParsedBundle
{
    public Guid BundledUpdateId { get; set; }
    public int? BundledRevisionNumber { get; set; }
}

public class ParsedExtendedUpdate
{
    public long RevisionId { get; set; }
    public string ExtendedMetadataXml { get; set; } = "";
    public List<ParsedFragment> Fragments { get; set; } = [];
    public List<ParsedFile> Files { get; set; } = [];
    public List<ParsedLocalization> Localizations { get; set; } = [];
}

public class ParsedFragment
{
    public string FragmentType { get; set; } = "";
    public string Language { get; set; } = "";
    public int Ordinal { get; set; }
    public string Xml { get; set; } = "";
}

public class ParsedFile
{
    public string FileName { get; set; } = "";
    public long Size { get; set; }
    public string? DigestSha1 { get; set; }
    public string? DigestSha256 { get; set; }
    public DateTime? Modified { get; set; }
    public string? SourceUrl { get; set; }
}

public class ParsedLocalization
{
    public string Language { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? MoreInfoUrl { get; set; }
    public string? SupportUrl { get; set; }
}
