using Microsoft.Extensions.Options;

namespace ReLiveWP.ServiceDefaults;

public class SupportLinks(IOptionsMonitor<SupportOptions> options)
{
    private SupportOptions Options => options.CurrentValue;

    public string ForErrorCode(uint code) => Options.ErrorCode($"0x{code:X8}");

    public string ForArticle(int articleId) => Options.Article(articleId);

    public string ForLink(int linkId) => Options.Fwlink(linkId);
}
