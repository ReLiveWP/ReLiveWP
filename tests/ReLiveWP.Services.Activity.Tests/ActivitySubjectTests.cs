using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Tests;

public class ActivitySubjectTests
{
    private const long Owner = 0x1122334455667788;

    [Fact]
    public void No_identifier_is_the_owner_in_the_live_store()
        => Assert.Equal(new ActivitySubject(ActivitySubjectKind.Owner, Owner, "WL"), ActivitySubjects.Resolve(null, null, Owner));

    [Fact]
    public void A_live_cid_that_is_not_the_owner_is_a_contact()
        => Assert.Equal(new ActivitySubject(ActivitySubjectKind.Contact, 42, "WL"), ActivitySubjects.Resolve("WL", "42", Owner));

    [Fact]
    public void A_live_cid_that_is_the_owner_is_the_owner()
        => Assert.Equal(ActivitySubjectKind.Owner, ActivitySubjects.Resolve("WL", Owner.ToString(), Owner).Kind);

    // captured: POST /Activities?$xslt=wp7rafeed&Count=75 from GetContactsActivities, once the
    // aggregate network's store existed. this used to 500 on long.Parse("TWITR")
    [Fact]
    public void The_aggregate_networks_own_identity_is_the_owner()
        => Assert.Equal(ActivitySubjectKind.Owner, ActivitySubjects.Resolve("TWITR", "TWITR", Owner).Kind);

    // PersistActivity drops any entry whose live:SourceId is not the asking store's NPWLAggServiceID
    [Fact]
    public void Entries_for_the_aggregate_store_are_stamped_with_its_tag()
        => Assert.Equal("TWITR", ActivitySubjects.Resolve("TWITR", "TWITR", Owner).StoreSourceId);

    [Theory]
    [InlineData("WL", "TWITR", "WL")]
    [InlineData("TWITR", "12345", "TWITR")]
    [InlineData("FB", "12345", "WL")]
    public void Anything_else_names_nobody_rather_than_throwing(string sourceId, string objectId, string storeSourceId)
    {
        var subject = ActivitySubjects.Resolve(sourceId, objectId, Owner);

        Assert.Equal(ActivitySubjectKind.Unknown, subject.Kind);
        Assert.Equal(storeSourceId, subject.StoreSourceId);
    }
}
