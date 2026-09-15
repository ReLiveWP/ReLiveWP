namespace ReLiveWP.Backend.DeviceUpdate.Model;

// The revision ids we have handed out. A device caches revision ids forever, because we never issue
// ServerChanged and never implement RefreshCache, so an id that has shipped can never be reused for
// different content. ContentHash is what catches an edit that forgot to bump RevisionNumber.
public class AuthoredRevision
{
    public Guid UpdateId { get; set; }
    public int RevisionNumber { get; set; }

    public long RevisionId { get; set; }
    public string ContentHash { get; set; } = "";
}
