using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public interface ILiveAnnotatable
{
    Annotations? BuildAnnotations(IReadOnlySet<string> requested);
}

// in a request, Annotation.Value is null: the client is declaring which names it wants back
[XmlRoot("Annotations", Namespace = Constants.WindowsLive)]
public class Annotations
{
    [XmlElement("Annotation", Namespace = Constants.WindowsLive)]
    public List<Annotation> Items { get; set; } = [];

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
