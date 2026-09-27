using System.ComponentModel.DataAnnotations;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class LiveDPoPKey
{
    [Key]
    public required string Id { get; set; }
    public required string Key { get; set; }
}
