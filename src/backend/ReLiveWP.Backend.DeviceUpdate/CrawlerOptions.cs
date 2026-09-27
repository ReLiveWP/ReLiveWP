namespace ReLiveWP.Backend.DeviceUpdate;

public class CrawlerOptions
{
    public const string SectionName = "Crawler";

    public string ClientEndpoint { get; set; } = "https://fe2.update.microsoft.com/v6/ClientWebService/client.asmx";
}
