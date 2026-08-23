using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// Sync is one way. The provider decides what exists, you decide what it says, and the moment you say
// anything the contact stops following the provider. PlanWrites and PlanDeletes are where that is
// decided, before anything is touched.
public class MirrorPlanTests
{
    private static RemoteContact Remote(string id, string first = "Ada", string? etag = null) =>
        new(id, new ContactItem { FirstName = first, FileAs = first }, etag ?? $"etag-{id}");

    private static MirrorBatch FullSync(params RemoteContact[] contacts) =>
        new(contacts, [], null, IsFullSync: true);

    private static MirrorBatch Delta(RemoteContact[] contacts, params string[] deleted) =>
        new(contacts, deleted, "token", IsFullSync: false);

    private static Dictionary<string, KnownItem> Known(params KnownItem[] items) =>
        items.ToDictionary(i => i.ServerId.Replace("server-", ""), StringComparer.Ordinal);

    private static KnownItem Tracked(string id, string? etag = null) =>
        new($"server-{id}", etag ?? $"etag-{id}", IsDeleted: false, RemoteSynced: true);

    private static KnownItem Edited(string id, string? etag = null) =>
        new($"server-{id}", etag ?? $"etag-{id}", IsDeleted: false, RemoteSynced: false);

    private static KnownItem Deleted(string id) =>
        new($"server-{id}", $"etag-{id}", IsDeleted: true, RemoteSynced: true);

    private static List<MirrorWrite> Writes(
        Dictionary<string, KnownItem> known, MirrorBatch batch) =>
        MirrorPlanner.PlanWrites(known, batch);

    private static List<string> Deletes(Dictionary<string, KnownItem> known, MirrorBatch batch) =>
        MirrorPlanner.PlanDeletes(known, batch);

    [Fact]
    public void A_new_remote_contact_is_created()
    {
        var (remote, serverId) = Assert.Single(Writes(Known(), FullSync(Remote("c1"))));

        Assert.Equal("c1", remote.ExternalId);
        Assert.Null(serverId);
    }

    [Fact]
    public void An_unchanged_contact_is_skipped_on_its_etag()
    {
        Assert.Empty(Writes(Known(Tracked("c1")), FullSync(Remote("c1"))));
    }

    [Fact]
    public void A_changed_contact_is_updated_in_place()
    {
        var writes = Writes(Known(Tracked("c1", "etag-old")), FullSync(Remote("c1")));

        Assert.Equal("server-c1", Assert.Single(writes).ServerId);
    }

    // the rule the whole rewrite rests on
    [Fact]
    public void A_contact_you_have_edited_is_left_alone()
    {
        var writes = Writes(Known(Edited("c1", "etag-old")), FullSync(Remote("c1", "Changed remotely")));

        Assert.Empty(writes);
    }

    [Fact]
    public void One_edited_contact_does_not_stop_the_others_updating()
    {
        var writes = Writes(
            Known(Edited("c1", "etag-old"), Tracked("c2", "etag-old")),
            FullSync(Remote("c1"), Remote("c2"), Remote("c3")));

        Assert.Equal(["c2", "c3"], writes.Select(w => w.Remote.ExternalId).Order());
    }

    [Fact]
    public void A_contact_listed_twice_in_one_batch_is_only_written_once()
    {
        Assert.Single(Writes(Known(), FullSync(Remote("c1"), Remote("c1"))));
    }

    // deleting a contact is how you ask for a fresh copy, so it comes back beside the old row rather
    // than trying to update something that is no longer there
    [Fact]
    public void A_contact_you_deleted_comes_back_as_a_new_row()
    {
        var writes = Writes(Known(Deleted("c1")), FullSync(Remote("c1")));

        Assert.Null(Assert.Single(writes).ServerId);
    }

    [Fact]
    public void A_contact_missing_from_a_full_sync_is_gone()
    {
        Assert.Equal(["c2"], Deletes(Known(Tracked("c1"), Tracked("c2")), FullSync(Remote("c1"))));
    }

    // an edited contact is skipped, not absent. counting it as gone would delete the very thing the
    // skip exists to protect.
    [Fact]
    public void An_edited_contact_is_not_mistaken_for_a_remote_deletion()
    {
        Assert.Empty(Deletes(Known(Edited("c1")), FullSync(Remote("c1"))));
    }

    // your copy outliving the provider's is the point of owning it
    [Fact]
    public void An_edited_contact_survives_disappearing_from_the_provider()
    {
        Assert.Empty(Deletes(Known(Edited("c1")), FullSync()));
    }

    [Fact]
    public void An_edited_contact_survives_an_explicit_remote_delete()
    {
        var gone = Deletes(Known(Edited("c1"), Tracked("c2")), Delta([], "c1", "c2"));

        Assert.Equal(["c2"], gone);
    }

    // a card the provider would not give us is not a card the provider has dropped
    [Fact]
    public void An_unreadable_card_is_not_treated_as_a_deletion()
    {
        var batch = new MirrorBatch([], [], null, IsFullSync: true, UnreadableExternalIds: ["c1"]);

        Assert.Empty(Deletes(Known(Tracked("c1")), batch));
    }

    [Fact]
    public void A_contact_you_deleted_is_not_also_reported_gone()
    {
        Assert.Empty(Deletes(Known(Deleted("c1")), FullSync(Remote("c1"))));
    }

    [Fact]
    public void A_delta_only_deletes_what_the_provider_named()
    {
        var gone = Deletes(Known(Tracked("c1"), Tracked("c2")), Delta([], "c2"));

        Assert.Equal(["c2"], gone);
    }

    // the run counters derive the skipped total from this, so every contact has to land on exactly
    // one side of the decision
    [Fact]
    public void Every_contact_is_either_written_or_skipped()
    {
        var batch = FullSync(Remote("c1"), Remote("c2"), Remote("c3"), Remote("c3"));
        var writes = Writes(Known(Edited("c1"), Tracked("c2")), batch);

        Assert.Single(writes);
        Assert.Equal(3, batch.Items.Count - writes.Count);
    }
}
