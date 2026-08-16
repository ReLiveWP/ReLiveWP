using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

// The TypeScript client generates its code page tables from MS-ASWBXML. This checks that every
// token this server does define agrees with the specification, so the two implementations cannot
// drift apart silently. It deliberately says nothing about tags the specification has and we do
// not - those are gaps to close, not behaviour to pin.
public class WbxmlGeneratedTableParityTests
{
    // Slots MS-ASWBXML revision 24 no longer lists. They held 2.5-era tags in earlier revisions
    // and have not been reassigned, so these entries sit in historically correct reserved slots.
    private static readonly HashSet<(int Page, string Tag)> NotInCurrentRevision =
    [
        (6, "Version"),
        (6, "DateTime"),
        (13, "AutdState"),
    ];

    private record SpecPage(int Page, string Ns, string Xmlns, Dictionary<string, int> Tokens);

    private static string GeneratedTablePath([CallerFilePath] string thisFile = "")
    {
        var repo = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(thisFile)))!;
        return Path.Combine(repo, "ReLiveWP.Web", "packages", "eas-client", "spec", "codepages.json");
    }

    private static Dictionary<int, SpecPage> LoadSpec()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(GeneratedTablePath()));

        var pages = new Dictionary<int, SpecPage>();
        foreach (var page in doc.RootElement.GetProperty("pages").EnumerateArray())
        {
            var tokens = new Dictionary<string, int>();
            foreach (var token in page.GetProperty("tokens").EnumerateObject())
                tokens[token.Name] = token.Value.GetInt32();

            var number = page.GetProperty("page").GetInt32();
            pages[number] = new SpecPage(
                number,
                page.GetProperty("ns").GetString()!,
                page.GetProperty("xmlns").GetString()!,
                tokens);
        }

        return pages;
    }

    private static Dictionary<int, ASWBXMLCodePage> ServerPages()
    {
        var field = typeof(ASWBXML).GetField("codePages", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Dictionary<int, ASWBXMLCodePage>)field.GetValue(new ASWBXML())!;
    }

    private static Dictionary<byte, string> ServerTokens(ASWBXMLCodePage page)
    {
        var field = typeof(ASWBXMLCodePage).GetField("tokenLookup", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Dictionary<byte, string>)field.GetValue(page)!;
    }

    [Fact]
    public void Generated_table_is_present()
    {
        Assert.True(File.Exists(GeneratedTablePath()),
            $"{GeneratedTablePath()} is missing; run `pnpm -C ReLiveWP.Web/packages/eas-client extract`");
    }

    [Fact]
    public void Every_server_token_matches_the_specification()
    {
        var spec = LoadSpec();
        var problems = new List<string>();

        foreach (var (number, page) in ServerPages())
        {
            if (!spec.TryGetValue(number, out var expected))
            {
                problems.Add($"code page {number} ({page.Namespace}) is in neither MS-ASWBXML nor codepages.extra.json");
                continue;
            }

            if (!string.Equals(page.Namespace, expected.Ns, StringComparison.OrdinalIgnoreCase))
                problems.Add($"page {number}: namespace '{page.Namespace}', specification says '{expected.Ns}'");

            if (page.Xmlns != expected.Xmlns)
                problems.Add($"page {number} ({page.Namespace}): prefix '{page.Xmlns}', specification says '{expected.Xmlns}'");

            foreach (var (token, tag) in ServerTokens(page))
            {
                if (NotInCurrentRevision.Contains((number, tag)))
                    continue;

                if (!expected.Tokens.TryGetValue(tag, out var specToken))
                    problems.Add($"page {number} ({page.Namespace}): '{tag}' (0x{token:X2}) is not in the specification");
                else if (specToken != token)
                    problems.Add($"page {number} ({page.Namespace}): '{tag}' is 0x{token:X2}, specification says 0x{specToken:X2}");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Reserved_slots_stay_reserved()
    {
        // The three entries above are only tolerable because nothing else claims those tokens.
        // If a future revision assigns one, the allowlist has to go rather than quietly shadow it.
        var spec = LoadSpec();

        foreach (var (page, tag) in NotInCurrentRevision)
        {
            var token = ServerTokens(ServerPages()[page]).First(t => t.Value == tag).Key;
            var claimed = spec[page].Tokens.FirstOrDefault(t => t.Value == token);

            Assert.True(claimed.Key is null,
                $"page {page} token 0x{token:X2} is '{tag}' here but MS-ASWBXML now assigns it to '{claimed.Key}'");
        }
    }
}
