using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Tests;

public class PinnedPeepsTests
{
    // captured: GET /ContactsActivities?...&$xslt=wp7ctsm&$xslt_peeps=,WL:6021637387831981387,&Count=75
    [Fact]
    public void A_single_pinned_live_contact()
    {
        var peeps = PinnedPeeps.ParsePeeps(",WL:6021637387831981387,");

        Assert.Equal([new PinnedPeep("WL", "6021637387831981387")], peeps);
        Assert.Equal([6021637387831981387L], PinnedPeeps.SelectLiveCids(peeps));
    }

    [Fact]
    public void Several_pinned_contacts_keep_their_order()
    {
        var peeps = PinnedPeeps.ParsePeeps(",WL:1,TWITR:someone,WL:2,");

        Assert.Equal(["WL", "TWITR", "WL"], peeps.Select(p => p.SourceId));
        Assert.Equal([1L, 2L], PinnedPeeps.SelectLiveCids(peeps));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(",")]
    [InlineData(",junk,WL:,:42,")]
    public void Nothing_usable_is_no_peeps(string? raw)
        => Assert.Empty(PinnedPeeps.ParsePeeps(raw));

    // GetContactID prints the WL remote id with %lld, so a cid with the top bit set arrives negative
    [Fact]
    public void Negative_cids_are_cids()
        => Assert.Equal([-42L], PinnedPeeps.SelectLiveCids(PinnedPeeps.ParsePeeps(",WL:-42,")));

    [Fact]
    public void Repeated_and_unreadable_live_ids_are_dropped()
        => Assert.Equal([7L], PinnedPeeps.SelectLiveCids(PinnedPeeps.ParsePeeps(",WL:7,wl:7,WL:TWITR,")));
}
