using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReLiveWP.Backend.Identity.Data;

public class LiveDPoPKey 
{
    [Key]
    public required string Id { get; set; }
    public required string Key { get; set; }
}
