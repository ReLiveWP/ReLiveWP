using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

/// <summary>
/// Implemented by entities that carry Windows Live annotation data.
/// Given the set of names the client subscribed to, produces the populated
/// <see cref="Annotations"/> response block (null when no values are available).
/// </summary>
public interface ILiveAnnotatable
{
    Annotations? BuildAnnotations(IReadOnlySet<string> requested);
}

/// <summary>
/// Windows Live annotation block — appears in FolderSync and Sync requests/responses.
/// In a request the <see cref="Annotation.Value"/> is absent (null); the client is
/// declaring which names it wants returned.  In a response both Name and Value are present.
/// </summary>
[XmlRoot("Annotations", Namespace = Constants.WindowsLive)]
public class Annotations
{
    [XmlElement("Annotation", Namespace = Constants.WindowsLive)]
    public List<Annotation> Items { get; set; } = [];

    /// <summary>
    /// Extracts the set of annotation names the client has subscribed to.
    /// Suitable for passing to <see cref="ILiveAnnotatable.BuildAnnotations"/>.
    /// </summary>
    public IReadOnlySet<string> RequestedNames()
        => Items.Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
}

public class Annotation
{
    [XmlElement("Name", Namespace = Constants.WindowsLive)]
    public string Name { get; set; } = null!;

    [XmlElement("Value", Namespace = Constants.WindowsLive)]
    public string? Value { get; set; }
}
