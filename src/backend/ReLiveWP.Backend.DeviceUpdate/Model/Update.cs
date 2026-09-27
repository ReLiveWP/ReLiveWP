namespace ReLiveWP.Backend.DeviceUpdate.Model;

public enum UpdateType
{
    Category,
    Detectoid,
    Software
}

public enum UpdateOrigin
{
    Crawled,
    Authored
}

public class Update
{
    public long RevisionId { get; set; }

    public Guid UpdateId { get; set; }
    public int RevisionNumber { get; set; }

    public UpdateType UpdateType { get; set; }
    public UpdateOrigin Origin { get; set; }

    // What upstream said, kept because the served value is narrower: a revision is a leaf only when
    // upstream and our own prerequisite graph agree. Upstream knows about dependents outside the
    // slice we hold (66 of 9274), and dropping those to leaf would stop the client reporting them.
    public bool IsLeafReported { get; set; }
    public bool IsLeaf { get; set; }

    // Action verbatim from the Deployment block (Evaluate/Install/Bundle). Stays a string because it
    // goes back on the wire unchanged and MS-WUSP defines more actions than we have ever seen.
    public string DeploymentAction { get; set; } = "Evaluate";
    public bool IsBundle { get; set; }

    public DateTime? LastChangeTime { get; set; }

    public UpdateMetadata? Metadata { get; set; }
    public UpdateExtendedMetadata? ExtendedMetadata { get; set; }

    public List<UpdatePrerequisite> Prerequisites { get; set; } = [];
    public List<UpdateBundle> BundledUpdates { get; set; } = [];
    public List<UpdateFile> Files { get; set; } = [];
    public List<UpdateLocalization> Localizations { get; set; } = [];
    public List<UpdateFragment> Fragments { get; set; } = [];
}
