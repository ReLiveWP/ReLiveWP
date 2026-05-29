namespace ReLiveWP.Services.Exchange.Data.Entities;

// Base for all EAS item types. EF Core maps this hierarchy to a single Items table (TPH)
// with an ItemClass discriminator column.
public abstract class Item
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public Folder Collection { get; set; } = null!;
    public string CollectionId { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

public class TaskItem : Item
{

}

public class EmailItem : Item
{

}
