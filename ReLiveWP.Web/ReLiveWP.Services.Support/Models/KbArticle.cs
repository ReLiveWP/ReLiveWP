using YamlDotNet.Serialization;

namespace ReLiveWP.Services.Support.Models;

public enum KbArticleType
{
    Info,
    Howto,
    Prb,
    Bug,
    Fix
}

public class KbFrontMatter
{
    public int Id { get; set; }
    public string Type { get; set; } = "INFO";
    public string Title { get; set; } = "";
    public string? Summary { get; set; }

    [YamlMember(Alias = "applies_to")]
    public List<string> AppliesTo { get; set; } = [];

    [YamlMember(Alias = "error_codes")]
    public List<string> ErrorCodes { get; set; } = [];

    public List<string> Keywords { get; set; } = [];

    public int? Fwlink { get; set; }

    public string Revision { get; set; } = "1.0";

    [YamlMember(Alias = "last_review")]
    public DateOnly LastReview { get; set; }

    [YamlMember(Alias = "see_also")]
    public List<int> SeeAlso { get; set; } = [];
}

public record KbArticle
{
    public required int Id { get; init; }
    public required KbArticleType Type { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Summary { get; init; }
    public required IReadOnlyList<string> AppliesTo { get; init; }
    public required IReadOnlyList<string> ErrorCodes { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
    public int? Fwlink { get; init; }
    public required string Revision { get; init; }
    public required DateOnly LastReview { get; init; }
    public required IReadOnlyList<int> SeeAlso { get; init; }
    public required string Html { get; init; }
    public required string PlainText { get; init; }

    public string Url => $"/kb/{Id}/{Slug}";

    public int Area => Id / 10000;

    public string MetaDescription
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Summary))
                return Summary;

            var flat = string.Join(' ', PlainText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (flat.Length <= 200)
                return flat;

            var cut = flat.LastIndexOf(' ', 200);
            return (cut > 0 ? flat[..cut] : flat[..200]) + "…";
        }
    }
}

public class AreaFile
{
    public Dictionary<int, string> Areas { get; set; } = [];
    public string Fallback { get; set; } = "Other";
}
