using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class SyncEngineTests
{
    private static SyncEvent Add(long id, string serverId = "a") => new(id, serverId, ChangeEventType.Add);
    private static SyncEvent Update(long id, string serverId = "a") => new(id, serverId, ChangeEventType.Update);
    private static SyncEvent Delete(long id, string serverId = "a") => new(id, serverId, ChangeEventType.Delete);

    [Fact]
    public void Add_then_Delete_collapses_to_nothing()
    {
        // created and gone before any device saw it
        var delta = SyncEngine.Collapse([Add(1), Delete(2)]);

        Assert.Empty(delta.Added);
        Assert.Empty(delta.Updated);
        Assert.Empty(delta.Deleted);
    }

    [Fact]
    public void Add_then_Update_collapses_to_Add()
    {
        var delta = SyncEngine.Collapse([Add(1), Update(2)]);

        Assert.Equal(["a"], delta.Added);
        Assert.Empty(delta.Updated);
    }

    [Fact]
    public void Update_then_Update_collapses_to_a_single_Update()
    {
        var delta = SyncEngine.Collapse([Update(1), Update(2)]);

        Assert.Equal(["a"], delta.Updated);
        Assert.Empty(delta.Added);
    }

    [Fact]
    public void Update_then_Delete_collapses_to_Delete()
    {
        var delta = SyncEngine.Collapse([Update(1), Delete(2)]);

        Assert.Equal(["a"], delta.Deleted);
        Assert.Empty(delta.Updated);
    }

    [Fact]
    public void Delete_then_Add_collapses_to_Add()
    {
        // only a leading Add cancels a trailing Delete, so a re-add survives instead of collapsing away
        var delta = SyncEngine.Collapse([Delete(1), Add(2)]);

        Assert.Equal(["a"], delta.Added);
        Assert.Empty(delta.Deleted);
    }

    [Fact]
    public void Out_of_order_event_ids_are_reordered_before_collapsing()
    {
        // id order is the source of truth, not list order
        var delta = SyncEngine.Collapse([Update(5), Add(2)]);

        Assert.Equal(["a"], delta.Added);
        Assert.Empty(delta.Updated);
    }

    [Fact]
    public void Multiple_ServerIds_are_grouped_and_resolved_independently()
    {
        var delta = SyncEngine.Collapse([
            Add(1, "added"),
            Update(2, "updated"),
            Delete(3, "deleted"),
            Add(4, "cancelled"),
            Delete(5, "cancelled"),
        ]);

        Assert.Equal(["added"], delta.Added);
        Assert.Equal(["updated"], delta.Updated);
        Assert.Equal(["deleted"], delta.Deleted);
        Assert.DoesNotContain("cancelled", delta.Added.Concat(delta.Updated).Concat(delta.Deleted));
    }

    [Fact]
    public void Watermark_is_the_max_Id_across_all_events_regardless_of_outcome()
    {
        // the watermark must advance past a group that collapses to nothing, or the next sync re-reads it
        var delta = SyncEngine.Collapse([
            Update(3, "kept"),
            Add(7, "cancelled"),
            Delete(9, "cancelled"),
        ]);

        Assert.Equal(["kept"], delta.Updated);
        Assert.Equal(9, delta.Watermark);
    }

    [Fact]
    public void Empty_input_returns_an_empty_delta_with_zero_watermark()
    {
        var delta = SyncEngine.Collapse([]);

        Assert.Empty(delta.Added);
        Assert.Empty(delta.Updated);
        Assert.Empty(delta.Deleted);
        Assert.Equal(0, delta.Watermark);
    }

    [Theory]
    [InlineData("0", "1")]
    [InlineData("1", "2")]
    [InlineData("41", "42")]
    public void NextSyncKey_increments_a_numeric_key(string current, string expected) =>
        Assert.Equal(expected, SyncEngine.NextSyncKey(current));

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    public void NextSyncKey_falls_back_to_1_for_an_unparsable_key(string current) =>
        Assert.Equal("1", SyncEngine.NextSyncKey(current));

    [Fact]
    public void SuppressDeleted_drops_server_Adds_and_Changes_for_a_just_deleted_item()
    {
        var commands = new SyncCommands
        {
            Add = [new SyncAdd { ServerId = "keep-add" }, new SyncAdd { ServerId = "gone" }],
            Change = [new SyncChange { ServerId = "gone" }, new SyncChange { ServerId = "keep-change" }],
        };

        SyncEngine.SuppressDeleted(commands, ["gone"]);

        Assert.Equal(["keep-add"], commands.Add.Select(a => a.ServerId));
        Assert.Equal(["keep-change"], commands.Change.Select(c => c.ServerId));
    }

    [Fact]
    public void SuppressDeleted_is_a_no_op_when_nothing_was_deleted()
    {
        var commands = new SyncCommands { Add = [new SyncAdd { ServerId = "a" }] };

        SyncEngine.SuppressDeleted(commands, []);

        Assert.Equal(["a"], commands.Add.Select(a => a.ServerId));
    }

    [Fact]
    public void SuppressDeleted_tolerates_null_commands() =>
        SyncEngine.SuppressDeleted(null, ["gone"]);

    [Fact]
    public void Collapse_without_a_windowSize_never_sets_MoreAvailable()
    {
        var delta = SyncEngine.Collapse([Add(1, "a"), Add(2, "b"), Add(3, "c"), Add(4, "d")]);

        Assert.False(delta.MoreAvailable);
        Assert.Equal(["a", "b", "c", "d"], delta.Added);
        Assert.Equal(4, delta.Watermark);
    }

    [Fact]
    public void Collapse_windowSize_that_fits_everything_behaves_unwindowed()
    {
        var delta = SyncEngine.Collapse([Add(1, "a"), Add(2, "b"), Add(3, "c"), Add(4, "d")], windowSize: 10);

        Assert.False(delta.MoreAvailable);
        Assert.Equal(["a", "b", "c", "d"], delta.Added);
        Assert.Equal(4, delta.Watermark);
    }

    [Fact]
    public void Collapse_truncates_to_windowSize_and_sets_MoreAvailable()
    {
        var delta = SyncEngine.Collapse(
            [Add(1, "a"), Add(2, "b"), Add(3, "c"), Add(4, "d")], windowSize: 2);

        Assert.True(delta.MoreAvailable);
        Assert.Equal(["a", "b"], delta.Added);
    }

    [Fact]
    public void Collapse_watermark_under_truncation_is_the_last_included_groups_max_id_not_the_global_max()
    {
        // 4 groups, id order a(1) b(2) c(3) d(4); windowSize 2 keeps a,b only
        var delta = SyncEngine.Collapse(
            [Add(1, "a"), Add(2, "b"), Add(3, "c"), Add(4, "d")], windowSize: 2);

        // must be 2 (last-included group "b"), never 4 (the global max) or the client would
        // skip c/d without ever seeing them
        Assert.Equal(2, delta.Watermark);
    }

    [Fact]
    public void Collapse_orders_groups_by_their_max_id_not_their_first_id()
    {
        // group "b" starts early (id 2) but is later updated at id 6, so its true position in
        // the window ordering is after "c" (id 3), not right after "a"
        var delta = SyncEngine.Collapse(
            [Add(1, "a"), Add(2, "b"), Add(3, "c"), Update(6, "b")], windowSize: 2);

        Assert.True(delta.MoreAvailable);
        Assert.Equal(["a", "c"], delta.Added);
        // "b" is excluded but its Add (id 2) sits below the last-included MaxId (c, id 3); the
        // watermark is clamped to just below b's earliest event so the follow-up sync re-reads
        // b whole as an Add rather than stranding it and surfacing a bogus Change.
        Assert.Equal(1, delta.Watermark);
    }

    [Fact]
    public void Collapse_does_not_strand_an_excluded_groups_add_below_the_watermark()
    {
        // "b" Added at id 1 then Updated at id 5; "a"/"c" (ids 2,3) sort ahead of b by MaxId and
        // fill the window. Advancing to c's id 3 would leave b's Add (id 1) below the watermark,
        // so the next AfterWatermark read would see only Update(5,"b") and misclassify b as a
        // Change for an item never delivered. The clamp must hold the watermark below b's Add.
        var delta = SyncEngine.Collapse(
            [Add(1, "b"), Add(2, "a"), Add(3, "c"), Update(5, "b")], windowSize: 2);

        Assert.True(delta.MoreAvailable);
        Assert.Equal(["a", "c"], delta.Added);
        Assert.Equal(0, delta.Watermark);
    }

    [Fact]
    public void Collapse_AllUpdatedServerIds_includes_ids_truncated_out_of_the_window()
    {
        var delta = SyncEngine.Collapse(
            [Update(1, "x"), Update(2, "y"), Update(3, "z")], windowSize: 1);

        // only "x" is surfaced this fetch...
        Assert.Equal(["x"], delta.Updated);
        Assert.True(delta.MoreAvailable);

        // ...but the full server-changed set (for A1 conflict detection) is unaffected by the cut
        Assert.Equal(new HashSet<string> { "x", "y", "z" }, delta.AllUpdatedServerIds);
    }

    [Fact]
    public void Collapse_AllUpdatedServerIds_excludes_Add_then_Update_groups()
    {
        // classified as Added (first == Add), not Updated, so it must not read as a conflict source
        var delta = SyncEngine.Collapse([Add(1, "a"), Update(2, "a")]);

        Assert.Empty(delta.AllUpdatedServerIds);
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(513, 512)]
    [InlineData(512, 512)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    public void ResolveWindowSize_clamps_per_spec(int? requested, int expected) =>
        Assert.Equal(expected, SyncEngine.ResolveWindowSize(requested));
}
