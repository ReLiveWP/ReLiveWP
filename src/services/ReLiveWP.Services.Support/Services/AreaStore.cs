using ReLiveWP.Services.Support.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReLiveWP.Services.Support.Services;

public class AreaStore : ContentStore<AreaStore.Snapshot>
{
    public record Snapshot(IReadOnlyDictionary<int, string> Names, string Fallback);

    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public AreaStore(IWebHostEnvironment env, ILogger<AreaStore> logger)
        : base(Path.Combine(env.ContentRootPath, "Content"), "areas.yaml", env, logger)
    {
    }

    public string NameFor(int area) =>
        Current.Names.TryGetValue(area, out var name) ? name : Current.Fallback;

    public bool IsNamed(int area) => Current.Names.ContainsKey(area);

    protected override Snapshot Load(IReadOnlyList<string> files)
    {
        var path = files.FirstOrDefault();
        if (path is null)
        {
            Logger.LogWarning("No areas.yaml found, every article will be grouped under the fallback heading");
            return new Snapshot(new Dictionary<int, string>(), "Other");
        }

        try
        {
            var parsed = Yaml.Deserialize<AreaFile>(File.ReadAllText(path)) ?? new AreaFile();
            Logger.LogInformation("Loaded {Count} knowledge base areas", parsed.Areas.Count);
            return new Snapshot(parsed.Areas, parsed.Fallback);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "areas.yaml is malformed, falling back to a single heading");
            return new Snapshot(new Dictionary<int, string>(), "Other");
        }
    }
}
