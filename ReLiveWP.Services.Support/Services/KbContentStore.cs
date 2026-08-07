using System.Text.RegularExpressions;
using Markdig;
using ReLiveWP.Services.Support.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReLiveWP.Services.Support.Services;

public partial class KbContentStore : ContentStore<KbContentStore.Snapshot>
{
    public record Snapshot(
        IReadOnlyDictionary<int, KbArticle> ById,
        IReadOnlyDictionary<string, int> ByErrorCode,
        IReadOnlyDictionary<int, int> ByFwlink,
        IReadOnlyList<KbArticle> All);

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UsePipeTables()
        .UseDefinitionLists()
        .UseGenericAttributes()
        .Build();

    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public KbContentStore(IWebHostEnvironment env, ILogger<KbContentStore> logger)
        : base(Path.Combine(env.ContentRootPath, "Content", "kb"), "*.md", env, logger)
    {
    }

    public KbArticle? Find(int id) => Current.ById.GetValueOrDefault(id);

    public KbArticle? FindByErrorCode(string code) =>
        Current.ByErrorCode.TryGetValue(NormaliseErrorCode(code), out var id) ? Find(id) : null;

    public KbArticle? FindByFwlink(int linkId) =>
        Current.ByFwlink.TryGetValue(linkId, out var id) ? Find(id) : null;

    public bool ClaimsFwlink(int linkId) => Current.ByFwlink.ContainsKey(linkId);

    public IReadOnlyList<KbArticle> All => Current.All;

    public IReadOnlyList<KbArticle> Search(string query)
    {
        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToArray();

        if (terms.Length == 0)
            return [];

        return Current.All
            .Select(a => (Article: a, Score: ScoreOf(a, terms)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Article.Id)
            .Select(x => x.Article)
            .ToList();
    }

    private static int ScoreOf(KbArticle article, string[] terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (article.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (article.Keywords.Any(k => k.Contains(term, StringComparison.OrdinalIgnoreCase))) score += 6;
            if (article.ErrorCodes.Any(c => c.Contains(term, StringComparison.OrdinalIgnoreCase))) score += 8;
            if (article.Summary?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score += 4;
            if (article.PlainText.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 1;
        }

        return score;
    }

    public static string NormaliseErrorCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];

        var stripped = trimmed.TrimStart('0');
        return stripped.Length == 0 ? "0" : stripped.ToUpperInvariant();
    }

    protected override Snapshot Load(IReadOnlyList<string> files)
    {
        var byId = new Dictionary<int, KbArticle>();
        var byErrorCode = new Dictionary<string, int>();
        var byFwlink = new Dictionary<int, int>();

        foreach (var file in files.Order())
        {
            var article = Parse(file);
            if (article is null)
                continue;

            if (!byId.TryAdd(article.Id, article))
            {
                Logger.LogWarning("KB{Id} in {File} duplicates an article already loaded, ignoring", article.Id, Path.GetFileName(file));
                continue;
            }

            foreach (var code in article.ErrorCodes)
            {
                var key = NormaliseErrorCode(code);
                if (!byErrorCode.TryAdd(key, article.Id))
                    Logger.LogWarning("Error code {Code} is claimed by both KB{Existing} and KB{Duplicate}", code, byErrorCode[key], article.Id);
            }

            if (article.Fwlink is { } linkId && !byFwlink.TryAdd(linkId, article.Id))
                Logger.LogWarning("Link {Link} is claimed by both KB{Existing} and KB{Duplicate}", linkId, byFwlink[linkId], article.Id);
        }

        foreach (var article in byId.Values)
        {
            foreach (var related in article.SeeAlso.Where(r => !byId.ContainsKey(r)))
                Logger.LogWarning("KB{Id} references KB{Missing} in see_also, which does not exist", article.Id, related);
        }

        Logger.LogInformation("Loaded {Count} knowledge base articles", byId.Count);

        return new Snapshot(byId, byErrorCode, byFwlink, byId.Values.OrderBy(a => a.Id).ToList());
    }

    private KbArticle? Parse(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var text = File.ReadAllText(path);

        var match = FrontMatterPattern().Match(text);
        if (!match.Success)
        {
            Logger.LogWarning("{File} has no YAML front matter block, skipping", Path.GetFileName(path));
            return null;
        }

        KbFrontMatter front;
        try
        {
            front = Yaml.Deserialize<KbFrontMatter>(match.Groups["yaml"].Value);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{File} has malformed front matter, skipping", Path.GetFileName(path));
            return null;
        }

        if (front.Id <= 0 || string.IsNullOrWhiteSpace(front.Title))
        {
            Logger.LogWarning("{File} is missing an id or title, skipping", Path.GetFileName(path));
            return null;
        }

        var separator = name.IndexOf('-');
        var prefix = separator < 0 ? name : name[..separator];
        if (!int.TryParse(prefix, out var fromName) || fromName != front.Id)
            Logger.LogWarning("{File} is named for KB{FromName} but declares id {Declared}", Path.GetFileName(path), prefix, front.Id);

        var slug = separator < 0 ? Slugify(front.Title) : name[(separator + 1)..];
        var body = match.Groups["body"].Value;

        if (!Enum.TryParse<KbArticleType>(front.Type, ignoreCase: true, out var type))
        {
            Logger.LogWarning("KB{Id} has unrecognised type {Type}, treating as INFO", front.Id, front.Type);
            type = KbArticleType.Info;
        }

        return new KbArticle
        {
            Id = front.Id,
            Type = type,
            Title = front.Title.Trim(),
            Slug = slug,
            Summary = front.Summary?.Trim(),
            AppliesTo = front.AppliesTo,
            ErrorCodes = front.ErrorCodes,
            Keywords = front.Keywords,
            Fwlink = front.Fwlink,
            Revision = front.Revision,
            LastReview = front.LastReview,
            SeeAlso = front.SeeAlso,
            Html = Markdown.ToHtml(body, Pipeline),
            PlainText = Markdown.ToPlainText(body, Pipeline)
        };
    }

    public static string Slugify(string title)
    {
        var slug = NonSlugPattern().Replace(title.ToLowerInvariant(), "-").Trim('-');
        if (slug.Length <= 60)
            return slug;

        var cut = slug.LastIndexOf('-', 60);
        return cut > 0 ? slug[..cut] : slug[..60];
    }

    [GeneratedRegex(@"\A---\r?\n(?<yaml>.*?)\r?\n---\r?\n(?<body>.*)\z", RegexOptions.Singleline)]
    private static partial Regex FrontMatterPattern();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonSlugPattern();
}
