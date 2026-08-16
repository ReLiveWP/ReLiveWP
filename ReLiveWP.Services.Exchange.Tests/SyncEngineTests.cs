using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class SyncEngineTests
{
    // one event per transaction, which is the ordinary case: a watermark is a commit id, so these
    // give each event its own. In(commit, ...) builds events that shared a transaction.
    private static SyncEvent Add(long id, string serverId = "a") => new(id, id, serverId, ChangeEventType.Add);
    private static SyncEvent Update(long id, string serverId = "a") => new(id, id, serverId, ChangeEventType.Update);
    private static SyncEvent Delete(long id, string serverId = "a") => new(id, id, serverId, ChangeEventType.Delete);

    private static SyncEvent In(long commitId, long id, string serverId, ChangeEventType type) =>
        new(commitId, id, serverId, type);

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
    public void Collapse_orders_groups_by_their_first_id_not_their_max_id()
    {
        // "b" starts at id 2 and is updated at id 6. Ordering by latest event would put it after
        // "c", excluding it while including an item that starts later - and then the watermark
        // has to be dragged back below b's Add to avoid stranding it, which is where paging used
        // to stop making progress. Ordering by earliest event cannot produce that situation.
        var delta = SyncEngine.Collapse(
            [Add(1, "a"), Add(2, "b"), Add(3, "c"), Update(6, "b")], windowSize: 2);

        Assert.True(delta.MoreAvailable);
        Assert.Equal(["a", "b"], delta.Added);
        // Just below "c", the first thing not delivered. b's later update is above this and comes
        // back as a Change next round, which is correct: the client already holds b.
        Assert.Equal(2, delta.Watermark);
    }

    [Fact]
    public void Collapse_does_not_strand_an_excluded_groups_add_below_the_watermark()
    {
        // "b" Added at id 1 then Updated at id 5. Stranding means advancing the watermark past
        // b's Add without delivering b, because the next read would then see only Update(5) and
        // report a Change for an item the client never received.
        var delta = SyncEngine.Collapse(
            [Add(1, "b"), Add(2, "a"), Add(3, "c"), Update(5, "b")], windowSize: 2);

        Assert.True(delta.MoreAvailable);
        Assert.Equal(["b", "a"], delta.Added);
        Assert.Equal(2, delta.Watermark);

        // The invariant, stated directly: nothing whose earliest event is at or below the
        // watermark may be left undelivered.
        Assert.Contains("b", delta.Added);
    }

    // The reason the cursor is a commit id and not the row id: a sequence allocates outside the
    // transaction, so a transaction can take a low id and commit after one that took a higher id.
    // Cursoring on the row id lets a reader advance past the higher id and never see the lower one.
    [Fact]
    public void Watermark_follows_commit_order_not_row_id_order()
    {
        // row ids say b(1) then a(2); commit order says a(10) then b(20)
        var delta = SyncEngine.Collapse(
        [
            In(20, 1, "b", ChangeEventType.Add),
            In(10, 2, "a", ChangeEventType.Add),
        ]);

        Assert.Equal(20, delta.Watermark);
        Assert.Equal(["a", "b"], delta.Added);
    }

    [Fact]
    public void Events_sharing_a_transaction_order_by_row_id_within_it()
    {
        // one transaction wrote both: the Add must be seen before the Update or the item would
        // classify as a Change for something never delivered
        var delta = SyncEngine.Collapse(
        [
            In(10, 2, "a", ChangeEventType.Update),
            In(10, 1, "a", ChangeEventType.Add),
        ]);

        Assert.Equal(["a"], delta.Added);
        Assert.Empty(delta.Updated);
    }

    [Fact]
    public void A_windowed_cut_never_lands_inside_a_transaction()
    {
        // three items written by one transaction (commit 10), a fourth by a later one (commit 20)
        var delta = SyncEngine.Collapse(
        [
            In(10, 1, "a", ChangeEventType.Add),
            In(10, 2, "b", ChangeEventType.Add),
            In(10, 3, "c", ChangeEventType.Add),
            In(20, 4, "d", ChangeEventType.Add),
        ], windowSize: 2);

        Assert.True(delta.MoreAvailable);
        // whatever the cut, the resulting watermark is a commit id or one below one, so the
        // follow-up read takes transaction 10 whole or skips it whole - never half of it
        Assert.True(delta.Watermark == 9 || delta.Watermark == 10,
            $"watermark {delta.Watermark} fell inside transaction 10");
    }

    [Fact]
    public void A_transaction_larger_than_the_window_still_makes_progress()
    {
        // one transaction wrote more items than the window; the watermark must still advance or
        // the collection can never drain
        var delta = SyncEngine.Collapse(
        [
            In(10, 1, "a", ChangeEventType.Add),
            In(10, 2, "b", ChangeEventType.Add),
            In(10, 3, "c", ChangeEventType.Add),
        ], windowSize: 2);

        Assert.True(delta.Watermark > 0, "watermark must advance or the collection stalls");
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
    [InlineData(null, 100)]   // absent -> default
    [InlineData(0, 512)]      // "interprets the value 0 (zero) ... as 512" (MS-ASCMD 2.2.3.199)
    [InlineData(-1, 100)]     // can't occur on the wire (unsignedInt); treated as absent
    [InlineData(513, 512)]
    [InlineData(512, 512)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    public void ResolveWindowSize_clamps_per_spec(int? requested, int expected) =>
        Assert.Equal(expected, SyncEngine.ResolveWindowSize(requested));

    // A collection larger than the window must drain. The watermark after a truncated window is
    // "just before the first event not delivered", so if an excluded group owns an earlier event
    // than an included one the watermark moves backwards and the client loops forever. Found by
    // the conformance harness against a live mailbox, at WindowSize 2.
    [Fact]
    public void Truncated_window_advances_the_watermark_past_the_baseline()
    {
        // "old" was added at commit 1 and touched again at 100, so it sorts last by latest event
        // and first by earliest. Everything else is a plain add.
        var events = new List<SyncEvent>
        {
            In(1, 1, "old", ChangeEventType.Add),
            In(2, 2, "b", ChangeEventType.Add),
            In(3, 3, "c", ChangeEventType.Add),
            In(4, 4, "d", ChangeEventType.Add),
            In(100, 100, "old", ChangeEventType.Update),
        };

        var delta = SyncEngine.Collapse(events, windowSize: 2);

        Assert.True(delta.MoreAvailable);
        Assert.True(delta.Watermark > 0,
            $"watermark must advance past the baseline, got {delta.Watermark}");
    }

    [Fact]
    public void Draining_a_collection_larger_than_the_window_terminates()
    {
        var events = new List<SyncEvent>
        {
            In(1, 1, "old", ChangeEventType.Add),
            In(2, 2, "b", ChangeEventType.Add),
            In(3, 3, "c", ChangeEventType.Add),
            In(4, 4, "d", ChangeEventType.Add),
            In(5, 5, "e", ChangeEventType.Add),
            In(100, 100, "old", ChangeEventType.Update),
        };

        var delivered = new HashSet<string>();
        long watermark = 0;
        int rounds = 0;

        while (rounds++ < 20)
        {
            var visible = events.Where(e => e.CommitId > watermark).ToList();
            if (visible.Count == 0) break;

            var delta = SyncEngine.Collapse(visible, windowSize: 2);

            Assert.True(delta.Watermark > watermark,
                $"round {rounds}: watermark stalled at {watermark}");

            watermark = delta.Watermark;
            foreach (var id in delta.Added.Concat(delta.Updated)) delivered.Add(id);

            if (!delta.MoreAvailable) break;
        }

        Assert.True(rounds < 20, "draining did not terminate");
        Assert.Equal(new[] { "b", "c", "d", "e", "old" }, delivered.OrderBy(x => x));
    }

    [Fact]
    public void Window_cut_never_strands_an_earlier_event_behind_a_later_one()
    {
        var events = new List<SyncEvent>
        {
            In(1, 1, "straddler", ChangeEventType.Add),
            In(2, 2, "b", ChangeEventType.Add),
            In(50, 50, "straddler", ChangeEventType.Update),
        };

        var delta = SyncEngine.Collapse(events, windowSize: 1);

        // Whatever is delivered, nothing below the watermark may still be undelivered: the
        // straddler's add is at commit 1, so a watermark above 1 requires it to have been sent.
        if (delta.Watermark >= 1)
            Assert.Contains("straddler", delta.Added.Concat(delta.Updated));
    }
}
