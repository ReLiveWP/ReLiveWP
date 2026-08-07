using ReLiveWP.Services.Support.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReLiveWP.Services.Support.Services;

public class FwlinkStore : ContentStore<IReadOnlyDictionary<int, Fwlink>>
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public FwlinkStore(IWebHostEnvironment env, ILogger<FwlinkStore> logger)
        : base(Path.Combine(env.ContentRootPath, "Content"), "fwlinks.yaml", env, logger)
    {
    }

    public Fwlink? Find(int id) => Current.GetValueOrDefault(id);

    public IEnumerable<Fwlink> All => Current.Values;

    protected override IReadOnlyDictionary<int, Fwlink> Load(IReadOnlyList<string> files)
    {
        var path = files.FirstOrDefault();
        if (path is null)
        {
            Logger.LogWarning("No fwlinks.yaml found, every forwarding link will report as unknown");
            return new Dictionary<int, Fwlink>();
        }

        FwlinkFile parsed;
        try
        {
            parsed = Yaml.Deserialize<FwlinkFile>(File.ReadAllText(path)) ?? new FwlinkFile();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "fwlinks.yaml is malformed, keeping the link table empty");
            return new Dictionary<int, Fwlink>();
        }

        var links = new Dictionary<int, Fwlink>();
        foreach (var (id, entry) in parsed.Links)
        {
            if (string.IsNullOrWhiteSpace(entry.Target))
            {
                Logger.LogWarning("Link {Id} has no target, ignoring", id);
                continue;
            }

            links[id] = new Fwlink(id, entry.Target, entry.Passthrough, entry.Note);
        }

        Logger.LogInformation("Loaded {Count} forwarding links", links.Count);

        return links;
    }
}
