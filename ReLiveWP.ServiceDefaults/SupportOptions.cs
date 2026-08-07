namespace ReLiveWP.ServiceDefaults;

public class SupportOptions
{
    public const string SectionName = "Support";

    public string? BaseUrl { get; set; }
    
    public string? FwlinkBaseUrl { get; set; }

    public string Resolve(string relative) =>
        string.IsNullOrEmpty(BaseUrl) ? relative : $"{BaseUrl.TrimEnd('/')}{relative}";

    public string Article(int articleId) => Resolve($"/kb/{articleId}");

    public string ErrorCode(string code) => Resolve($"/errors/{code}");

    public string Fwlink(int linkId) =>
        string.IsNullOrEmpty(FwlinkBaseUrl)
            ? $"/fwlink/p/?LinkID={linkId}"
            : $"{FwlinkBaseUrl.TrimEnd('/')}/fwlink/p/?LinkID={linkId}";
}
