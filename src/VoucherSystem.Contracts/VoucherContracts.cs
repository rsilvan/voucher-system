namespace VoucherSystem.Contracts.Vouchers;

public class VoucherResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }
    public string Code { get; set; } = default!;
    public string CodeMasked { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }
    public string? Currency { get; set; }
    public string? ApplyTo { get; set; }
    public int RedemptionCount { get; set; }
    public int? MaxRedemptions { get; set; }
    public string? HolderId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
}

public class CreateVoucherRequest
{
    public string Code { get; set; } = default!;
    public string Type { get; set; } = "Unique";
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }
    public string? Currency { get; set; }
    public string? ApplyTo { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public string? HolderId { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Metadata { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class UpdateVoucherRequest
{
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }
    public string? Currency { get; set; }
    public string? ApplyTo { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public string? HolderId { get; set; }
    public string? Metadata { get; set; }
    public uint RowVersion { get; set; }
}

public class VoucherListResponse
{
    public List<VoucherSummaryResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class VoucherSummaryResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public int RedemptionCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ─── Voucher Batch ───

public class VoucherBatchResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Count { get; set; }
    public int GeneratedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class CreateVoucherBatchRequest
{
    public string Name { get; set; } = default!;
    public int Count { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string Charset { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public int CodeLength { get; set; } = 10;
    public string Type { get; set; } = "Unique";
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public string? Currency { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Metadata { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class VoucherBatchListResponse
{
    public List<VoucherBatchResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

// ─── Voucher Action ───

public class VoucherActionRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? Metadata { get; set; }
}

public class VoucherActionResponse
{
    public string OperationId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public List<string> Events { get; set; } = new();
}

// ─── Voucher Pattern ───

public class VoucherPatternResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string Charset { get; set; } = default!;
    public int Length { get; set; }
    public string? Pattern { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateVoucherPatternRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string Charset { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public int Length { get; set; } = 10;
    public string? Pattern { get; set; }
}

// ─── Voucher Redemption ───

public class RedeemVoucherRequest
{
    public string Code { get; set; } = default!;
    public string? CustomerId { get; set; }
    public string? OrderId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Metadata { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class VoucherRedemptionResponse
{
    public Guid Id { get; set; }
    public Guid VoucherId { get; set; }
    public string Code { get; set; } = default!;
    public string? CustomerId { get; set; }
    public string? OrderId { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
