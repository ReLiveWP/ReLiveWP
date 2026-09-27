namespace ReLiveWP.Services.Support.Models;

public class FwlinkFile
{
    public Dictionary<int, FwlinkEntry> Links { get; set; } = [];
}

public class FwlinkEntry
{
    public string? Target { get; set; }
    public bool Passthrough { get; set; }
    public DateOnly? Added { get; set; }
    public string? Note { get; set; }
}

public record Fwlink(int Id, string Target, bool Passthrough, string? Note);
