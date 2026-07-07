using VoucherSystem.Contracts.Stores;

namespace VoucherSystem.Contracts.Areas;

public class AreaResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? ParentAreaId { get; set; }
    public int Depth { get; set; }
    public List<AreaResponse> Children { get; set; } = new();
    public List<StoreSummaryResponse> Stores { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class CreateAreaRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? ParentAreaId { get; set; }
}

public class UpdateAreaRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentAreaId { get; set; }
}

public class AreaTreeResponse
{
    public List<AreaResponse> Roots { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AssignStoresToAreaRequest
{
    public List<Guid> StoreIds { get; set; } = new();
}
