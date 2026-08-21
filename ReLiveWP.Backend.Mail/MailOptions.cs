namespace ReLiveWP.Backend.Mail;

public class MailOptions
{
    public const string SectionName = "Mail";

    public string[] LocalDomains { get; set; } = [];

    public string MessageIdDomain { get; set; } = "relivewp.net";

    public int MaxMessageBytes { get; set; } = 25 * 1024 * 1024;
}
