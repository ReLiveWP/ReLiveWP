using Microsoft.Extensions.Configuration;

namespace ReLiveWP.Backend.Mail.Tests;

public class MailOptionsTests
{
    private static MailOptions Bind(params (string Key, string Value)[] settings)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s =>
                new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        var options = new MailOptions();
        config.GetSection(MailOptions.SectionName).Bind(options);
        return options;
    }

    [Fact]
    public void VerifyLocalDomains_binds_from_configuration()
    {
        Assert.False(Bind(("Mail:VerifyLocalDomains", "false")).VerifyLocalDomains);
    }

    [Fact]
    public void VerifyLocalDomains_defaults_on()
    {
        Assert.True(Bind().VerifyLocalDomains);
    }

    [Fact]
    public void LocalDomains_binds_from_configuration()
    {
        Assert.Equal(["relivewp.net"], Bind(("Mail:LocalDomains:0", "relivewp.net")).LocalDomains);
    }
}
