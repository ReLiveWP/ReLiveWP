using System.Text.Json;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// The delta envelope is what makes an incremental sync incremental: miss @removed and deletions never
// land, miss @odata.deltaLink and every poll is a full pull.
public class GraphDeltaShapeTests
{
    private const string Page = """
        {
          "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#contacts",
          "@odata.deltaLink": "https://graph.microsoft.com/v1.0/me/contacts/delta?$deltatoken=abc123",
          "value": [
            { "id": "keep-1", "displayName": "Ada Lovelace", "givenName": "Ada", "surname": "Lovelace" },
            { "id": "gone-1", "@removed": { "reason": "deleted" } }
          ]
        }
        """;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static GraphDeltaResponse Parse(string json) =>
        JsonSerializer.Deserialize<GraphDeltaResponse>(json, Json)!;

    [Fact]
    public void Reads_the_delta_link_from_the_odata_annotation()
        => Assert.Equal("https://graph.microsoft.com/v1.0/me/contacts/delta?$deltatoken=abc123",
            Parse(Page).DeltaLink);

    [Fact]
    public void A_final_page_has_no_next_link()
        => Assert.Null(Parse(Page).NextLink);

    [Fact]
    public void Reads_the_next_link_while_paging()
        => Assert.Equal("https://graph.microsoft.com/v1.0/me/contacts/delta?$skiptoken=p2",
            Parse("""
                {
                  "@odata.nextLink": "https://graph.microsoft.com/v1.0/me/contacts/delta?$skiptoken=p2",
                  "value": []
                }
                """).NextLink);

    [Fact]
    public void A_removed_entry_is_distinguishable_from_a_contact()
    {
        var entries = Parse(Page).Value;

        Assert.False(entries[0].TryGetProperty("@removed", out _));
        Assert.True(entries[1].TryGetProperty("@removed", out _));
    }

    // a tombstone carries an id and nothing else, which is why the driver tests @removed before it
    // projects rather than after
    [Fact]
    public void A_removed_entry_carries_an_id_but_no_contact()
    {
        var removed = Parse(Page).Value[1];

        Assert.Equal("gone-1", removed.GetProperty("id").GetString());
        Assert.False(GraphContactSyncDriver.Project(removed)!.Contact.HasFileAs);
    }

    [Fact]
    public void A_live_entry_still_projects()
        => Assert.Equal("Ada Lovelace", GraphContactSyncDriver.Project(Parse(Page).Value[0])!.Contact.FileAs);

    // Graph answers 400 ErrorInvalidUrlQuery if any of these reach change tracking on contacts, and
    // it names them explicitly: '$orderby, $filter, $select, $expand, $search, $top'
    [Theory]
    [InlineData(GraphContactSyncDriver.DefaultSourceId)]
    [InlineData("AAMkAGZvbGRlcg==")]
    public void The_delta_url_carries_no_parameter_change_tracking_rejects(string sourceId)
    {
        var path = GraphContactSyncDriver.DeltaPath(sourceId);

        Assert.DoesNotContain('?', path);

        foreach (var rejected in new[] { "$orderby", "$filter", "$select", "$expand", "$search", "$top" })
            Assert.DoesNotContain(rejected, path);
    }

    [Fact]
    public void The_default_folder_and_a_named_folder_have_distinct_delta_urls()
    {
        Assert.Equal("graph.microsoft.com/v1.0/me/contacts/delta",
            GraphContactSyncDriver.DeltaPath(GraphContactSyncDriver.DefaultSourceId));

        Assert.Equal("graph.microsoft.com/v1.0/me/contactFolders/AAMkAGZvbGRlcg%3D%3D/contacts/delta",
            GraphContactSyncDriver.DeltaPath("AAMkAGZvbGRlcg=="));
    }
}
