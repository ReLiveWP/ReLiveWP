using System.Text;
using ReLiveWP.Services.Messenger.Msnp;
using static ReLiveWP.Services.Messenger.Tests.MsnpSamples;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpSdgTests
{
    private static readonly DateTimeOffset SentAt = new(2026, 9, 27, 21, 4, 5, TimeSpan.FromHours(1));

    [Fact]
    public void ArrivalTimeIsUtcWithThreeFractionDigitsAndAZ()
    {
        Assert.Equal("2026-09-27T20:04:05.000Z", MsnpSdg.FormatArrivalTime(SentAt));
    }

    [Fact]
    public void ArrivalTimeGoesAtTheEndOfTheMessagingBlockAndTheBodyIsUntouched()
    {
        var payload = Encoding.UTF8.GetBytes(SdgPayload("hi", channel: "IM/Offline"));

        var written = Encoding.UTF8.GetString(MsnpSdg.WithOriginalArrivalTime(payload, SentAt));

        var expected = SdgPayload("hi", channel: "IM/Offline")
            .Replace("7bit\r\n\r\nhi", "7bit\r\nOriginal-Arrival-Time: 2026-09-27T20:04:05.000Z\r\n\r\nhi");
        Assert.Equal(expected, written);
    }

    [Fact]
    public void ASenderSuppliedArrivalTimeIsReplaced()
    {
        var forged = SdgPayload("hi", channel: "IM/Offline")
            .Replace("7bit\r\n", "7bit\r\noriginal-arrival-time: 2001-01-01T00:00:00Z\r\n");

        var written = Encoding.UTF8.GetString(MsnpSdg.WithOriginalArrivalTime(Encoding.UTF8.GetBytes(forged), SentAt));

        Assert.DoesNotContain("2001-01-01", written);
        Assert.Single(written.Split("\r\n"), line => line.StartsWith("Original-Arrival-Time:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void APayloadWithoutAMessagingBlockIsLeftAlone()
    {
        var payload = Encoding.UTF8.GetBytes(PresencePayload);

        Assert.Same(payload, MsnpSdg.WithOriginalArrivalTime(payload, SentAt));
    }
}
