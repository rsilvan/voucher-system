namespace VoucherSystem.Contracts.Promotions;

public class PromotionResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Priority { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Metadata { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public List<DiscountDefinitionResponse> DiscountDefinitions { get; set; } = new();
}

public class CreatePromotionRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = "Automatic";
    public int Priority { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Metadata { get; set; }
    public List<CreateDiscountDefinitionRequest> DiscountDefinitions { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}

public class UpdatePromotionRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int? Priority { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Metadata { get; set; }
    public uint RowVersion { get; set; }
}

public class PromotionListResponse
{
    public List<PromotionSummaryResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class PromotionSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ─── Discount Definition ───

public class DiscountDefinitionResponse
{
    public Guid Id { get; set; }
    public string DiscountType { get; set; } = default!;
    public string ValueType { get; set; } = default!;
    public decimal Value { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }
    public string ApplyTo { get; set; } = default!;
    public string? ItemScope { get; set; }
    public int? BuyXQuantity { get; set; }
    public int? GetYQuantity { get; set; }
    public decimal? GetYDiscountPercent { get; set; }
    public List<DiscountTierResponse> Tiers { get; set; } = new();
}

public class CreateDiscountDefinitionRequest
{
    public string DiscountType { get; set; } = "Percentage";
    public string ValueType { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }
    public string ApplyTo { get; set; } = "Order";
    public string? ItemScope { get; set; }
    public int? BuyXQuantity { get; set; }
    public int? GetYQuantity { get; set; }
    public decimal? GetYDiscountPercent { get; set; }
    public List<CreateDiscountTierRequest> Tiers { get; set; } = new();
}

public class DiscountTierResponse
{
    public Guid Id { get; set; }
    public decimal FromValue { get; set; }
    public decimal? ToValue { get; set; }
    public decimal DiscountValue { get; set; }
    public string DiscountType { get; set; } = default!;
}

public class CreateDiscountTierRequest
{
    public decimal FromValue { get; set; }
    public decimal? ToValue { get; set; }
    public decimal DiscountValue { get; set; }
    public string DiscountType { get; set; } = "Percentage";
}

// ─── Promotion Action ───

public class PromotionActionRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? Metadata { get; set; }
}

public class PromotionActionResponse
{
    public string OperationId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public List<string> Events { get; set; } = new();
}

// ─── Promotion Preview ───

public class PromotionPreviewRequest
{
    public string? OrderContext { get; set; } // JSON
    public decimal OrderTotal { get; set; }
    public string Currency { get; set; } = "BRL";
    public List<PromotionPreviewItem> Items { get; set; } = new();
}

public class PromotionPreviewItem
{
    public string Sku { get; set; } = default!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Category { get; set; }
}

public class PromotionPreviewResponse
{
    public decimal OriginalTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalTotal { get; set; }
    public string Currency { get; set; } = default!;
    public List<CalculatedDiscountItem> Discounts { get; set; } = new();
}

public class CalculatedDiscountItem
{
    public Guid PromotionId { get; set; }
    public string PromotionName { get; set; } = default!;
    public string Type { get; set; } = default!;
    public decimal DiscountAmount { get; set; }
    public string? Description { get; set; }
}
