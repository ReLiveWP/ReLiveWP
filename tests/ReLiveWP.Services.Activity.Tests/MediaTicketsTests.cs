using Microsoft.Extensions.Configuration;
using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Tests;

public class MediaTicketsTests
{
    private const string UserId = "3f2b8c1e-0000-4000-8000-000000000001";
    private const string ResourceRef = "atproto+did:plc:abc+bafyphoto";

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static FixedClock Clock() => new(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));

    private static MediaTicketService Tickets(TimeProvider clock, string? secret = "secret-one")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:TicketSecret"] = secret })
            .Build();

        return new MediaTicketService(configuration, clock);
    }

    [Fact]
    public void ATicketRoundTrips()
    {
        var tickets = Tickets(Clock());

        var ticket = tickets.MintTicket(UserId, ResourceRef, 320);

        Assert.True(tickets.TryVerifyTicket(ticket, ResourceRef, 320, out var userId));
        Assert.Equal(UserId, userId);
    }

    [Fact]
    public void ATicketIsBoundToItsPhoto()
    {
        var tickets = Tickets(Clock());

        var ticket = tickets.MintTicket(UserId, ResourceRef, 320);

        Assert.False(tickets.TryVerifyTicket(ticket, "atproto+did:plc:abc+bafyother", 320, out _));
    }

    [Fact]
    public void ATicketIsBoundToItsSize()
    {
        var tickets = Tickets(Clock());

        var ticket = tickets.MintTicket(UserId, ResourceRef, 320);

        Assert.False(tickets.TryVerifyTicket(ticket, ResourceRef, 800, out _));
        Assert.False(tickets.TryVerifyTicket(ticket, ResourceRef, 0, out _));
    }

    [Fact]
    public void ATicketExpires()
    {
        var clock = Clock();
        var tickets = Tickets(clock);

        var ticket = tickets.MintTicket(UserId, ResourceRef, 320);

        clock.Now = clock.Now.Add(MediaTicketService.Lifetime);
        Assert.True(tickets.TryVerifyTicket(ticket, ResourceRef, 320, out _));

        clock.Now = clock.Now.AddSeconds(1);
        Assert.False(tickets.TryVerifyTicket(ticket, ResourceRef, 320, out _));
    }

    [Fact]
    public void AnotherKeyRejectsIt()
    {
        var clock = Clock();
        var ticket = Tickets(clock).MintTicket(UserId, ResourceRef, 320);

        Assert.False(Tickets(clock, "secret-two").TryVerifyTicket(ticket, ResourceRef, 320, out _));
    }

    [Fact]
    public void ATamperedTicketIsRejected()
    {
        var tickets = Tickets(Clock());
        var ticket = tickets.MintTicket(UserId, ResourceRef, 320);
        var parts = ticket.Split('.');

        var otherUser = Convert.ToBase64String("someone-else"u8.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.False(tickets.TryVerifyTicket($"{otherUser}.{parts[1]}.{parts[2]}", ResourceRef, 320, out _));

        var later = long.Parse(parts[1]) + 3600;
        Assert.False(tickets.TryVerifyTicket($"{parts[0]}.{later}.{parts[2]}", ResourceRef, 320, out _));

        var flipped = parts[2][..^1] + (parts[2][^1] == 'A' ? 'B' : 'A');
        Assert.False(tickets.TryVerifyTicket($"{parts[0]}.{parts[1]}.{flipped}", ResourceRef, 320, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("a.b")]
    [InlineData("a.b.c.d")]
    [InlineData("a.notanumber.c")]
    [InlineData("%%%.1800000000.c")]
    public void GarbageIsRejected(string ticket)
    {
        Assert.False(Tickets(Clock()).TryVerifyTicket(ticket, ResourceRef, 320, out _));
    }

    [Fact]
    public void SignUrlAppendsTheTicketAsAQuery()
    {
        var tickets = Tickets(Clock());

        var url = tickets.SignUrl("https://api-live.example/Users(1)/Files/files('x')/thumbnail/320", UserId, "x", 320);

        var query = url[(url.IndexOf('?') + 1)..];
        Assert.StartsWith($"{MediaTicketService.QueryKey}=", query);
        Assert.True(tickets.TryVerifyTicket(query[(MediaTicketService.QueryKey.Length + 1)..], "x", 320, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AMissingSecretRefusesToStart(string? secret)
    {
        Assert.Throws<InvalidOperationException>(() => Tickets(Clock(), secret));
    }
}
