namespace ReLiveWP.Backend.Mailbox.Data.Entities;

// one connected network per row. never synced as an item: 8.1 reads these off the me contact as
// suffixed WindowsLive annotations
public class DbNetwork
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;

    public int DomainId { get; set; }
    public string UserEmail { get; set; } = null!;

    public string? DisplayName { get; set; }
    public string? AccountName { get; set; }
    public string? DomainTag { get; set; }

    public DateTime? LastSync { get; set; }
    public string? PsaState { get; set; }
    public DateTime? PsaLastChanged { get; set; }

    public int? Offers { get; set; }
    public int? PartnerOffers { get; set; }

    public string? ClientToken { get; set; }
    public string? ClientToken2 { get; set; }
    public string? ClientPublishSecret { get; set; }
}

public static class PsaStates
{
    public const string Accept = "Accept";
    public const string Maybe = "Maybe";
    public const string Reject = "Reject";
    public const string Error = "Error";
}
