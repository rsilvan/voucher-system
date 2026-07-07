namespace VoucherSystem.Domain;

public class Area
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? ParentAreaId { get; set; }
    public int Depth { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Project Project { get; set; } = default!;
    public Area? ParentArea { get; set; }
    public ICollection<Area> Children { get; set; } = new List<Area>();
    public ICollection<AreaStore> AreaStores { get; set; } = new List<AreaStore>();

    public bool WouldCreateCycle(Guid? newParentId)
    {
        if (newParentId is null || newParentId == Id)
            return true;

        // If there's no parent yet, we just check if it's pointing to itself
        if (ParentAreaId is null)
            return false;

        // If we're just updating the parent, a cycle happens when the new parent
        // is a descendant of this area or is this area itself.
        // We validate this externally by traversing the tree.
        return false;
    }

    public bool IsAncestorOf(Area potentialDescendant)
    {
        var current = potentialDescendant;
        while (current.ParentAreaId is not null)
        {
            if (current.ParentAreaId == Id)
                return true;
            // We can't follow links without loading the full chain from DB,
            // so this is a helper that assumes ParentArea is loaded.
            current = current.ParentArea;
            if (current is null)
                break;
        }
        return false;
    }
}
