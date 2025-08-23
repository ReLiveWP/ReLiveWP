namespace ReLiveWP.Services.Activity.Models;

public class EntryModel
{
    public required string Id { get; set; }
    public required string ProviderId { get; set; }
    public required EntryType EntryType { get; set; } = EntryType.Post;
    public required ProfileModel Author { get; set; }
    public required DateTimeOffset Published { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Generator { get; set; }
    public required string CanonicalUrl { get; set; }
    public required List<string> Categories { get; set; } = [];

    public List<ActivityModel> AdditionalActivities { get; set; } = [];

    public bool CanReply { get; set; } = true;
    public int? ReplyCount { get; set; } = null;
}