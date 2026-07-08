using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VoucherSystem.Contracts.Vouchers;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _repo;
    private readonly IAuditLogWriter _audit;

    public VoucherService(IVoucherRepository repo, IAuditLogWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<VoucherResponse> CreateAsync(Guid organizationId, Guid projectId, CreateVoucherRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Voucher code is required.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _repo.CodeExistsAsync(organizationId, projectId, code))
            throw new ArgumentException("A voucher with this code already exists.");

        var masked = code.Length > 4 ? new string('*', code.Length - 4) + code[^4..] : code;

        var now = DateTimeOffset.UtcNow;
        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Code = code,
            CodeMasked = masked,
            Type = request.Type,
            Status = nameof(VoucherStatus.Active),
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxDiscount = request.MaxDiscount,
            MinPurchase = request.MinPurchase,
            Currency = request.Currency,
            ApplyTo = request.ApplyTo,
            MaxRedemptions = request.MaxRedemptions,
            MaxRedemptionsPerCustomer = request.MaxRedemptionsPerCustomer,
            HolderId = request.HolderId,
            StartAt = request.StartAt,
            ExpiresAt = request.ExpiresAt,
            Metadata = request.Metadata,
            CreatedBy = userId,
            CreatedAt = now
        };

        await _repo.AddAsync(voucher);

        await _repo.AddLifecycleEventAsync(new VoucherLifecycleEvent
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            OrganizationId = organizationId,
            ProjectId = projectId,
            EventType = "created",
            ActorId = userId,
            CreatedAt = now
        });

        _audit.Write(organizationId, projectId, userId, "voucher.created", "Voucher", voucher.Id.ToString());
        return MapToResponse(voucher);
    }

    public async Task<VoucherResponse?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id)
    {
        var voucher = await _repo.GetByIdAsync(organizationId, projectId, id);
        return voucher is null ? null : MapToResponse(voucher);
    }

    public async Task<VoucherResponse?> GetByCodeAsync(Guid organizationId, Guid projectId, string code)
    {
        var voucher = await _repo.GetByCodeAsync(organizationId, projectId, code.Trim().ToUpperInvariant());
        return voucher is null ? null : MapToResponse(voucher);
    }

    public async Task<VoucherListResponse> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status)
    {
        var items = await _repo.GetListAsync(organizationId, projectId, page, pageSize, status);
        var total = await _repo.GetCountAsync(organizationId, projectId, status);

        return new VoucherListResponse
        {
            Items = items.Select(v => new VoucherSummaryResponse
            {
                Id = v.Id,
                Code = v.CodeMasked ?? v.Code,
                Type = v.Type,
                Status = v.Status,
                DiscountType = v.DiscountType,
                DiscountValue = v.DiscountValue,
                RedemptionCount = v.RedemptionCount,
                ExpiresAt = v.ExpiresAt,
                CreatedAt = v.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<VoucherResponse?> UpdateAsync(Guid organizationId, Guid projectId, Guid id, UpdateVoucherRequest request, Guid userId)
    {
        var voucher = await _repo.GetByIdAsync(organizationId, projectId, id);
        if (voucher is null) return null;

        if (request.DiscountType is not null) voucher.DiscountType = request.DiscountType;
        if (request.DiscountValue.HasValue) voucher.DiscountValue = request.DiscountValue;
        if (request.MaxDiscount.HasValue) voucher.MaxDiscount = request.MaxDiscount;
        if (request.MinPurchase.HasValue) voucher.MinPurchase = request.MinPurchase;
        if (request.Currency is not null) voucher.Currency = request.Currency;
        if (request.ApplyTo is not null) voucher.ApplyTo = request.ApplyTo;
        if (request.MaxRedemptions.HasValue) voucher.MaxRedemptions = request.MaxRedemptions;
        if (request.HolderId is not null) voucher.HolderId = request.HolderId;
        if (request.Metadata is not null) voucher.Metadata = request.Metadata;

        voucher.UpdatedAt = DateTimeOffset.UtcNow;
        voucher.UpdatedBy = userId;
        await _repo.UpdateAsync(voucher);

        _audit.Write(organizationId, projectId, userId, "voucher.updated", "Voucher", voucher.Id.ToString());
        return MapToResponse(voucher);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id)
    {
        var voucher = await _repo.GetByIdAsync(organizationId, projectId, id);
        if (voucher is null) return false;

        voucher.Status = nameof(VoucherStatus.PendingDeletion);
        voucher.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(voucher);
        return true;
    }

    public async Task<VoucherActionResponse> ExecuteActionAsync(Guid organizationId, Guid projectId, Guid id, string action, VoucherActionRequest request, Guid userId)
    {
        var voucher = await _repo.GetByIdAsync(organizationId, projectId, id)
            ?? throw new ArgumentException("Voucher not found.");

        var valid = action switch
        {
            "disable" when voucher.Status == nameof(VoucherStatus.Active) => true,
            "enable" when voucher.Status == nameof(VoucherStatus.Disabled) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException($"Cannot '{action}' voucher in status '{voucher.Status}'.");

        if (action == "disable")
        {
            voucher.Status = nameof(VoucherStatus.Disabled);
            voucher.DisabledAt = DateTimeOffset.UtcNow;
            voucher.DisabledBy = userId;
        }
        else if (action == "enable")
        {
            voucher.Status = nameof(VoucherStatus.Active);
            voucher.DisabledAt = null;
            voucher.DisabledBy = null;
        }

        voucher.UpdatedAt = DateTimeOffset.UtcNow;
        voucher.UpdatedBy = userId;
        await _repo.UpdateAsync(voucher);

        await _repo.AddLifecycleEventAsync(new VoucherLifecycleEvent
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            OrganizationId = organizationId,
            ProjectId = projectId,
            EventType = action == "disable" ? "disabled" : "activated",
            ActorId = userId,
            Description = request.Reason,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _audit.Write(organizationId, projectId, userId, $"voucher.{action}", "Voucher", voucher.Id.ToString());

        return new VoucherActionResponse
        {
            OperationId = voucher.Id.ToString(),
            Status = voucher.Status,
            Events = new List<string> { $"voucher.{action}" }
        };
    }

    public async Task<VoucherBatchResponse> CreateBatchAsync(Guid organizationId, Guid projectId, CreateVoucherBatchRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Batch name is required.");
        if (request.Count <= 0 || request.Count > 10000)
            throw new ArgumentException("Count must be between 1 and 10000.");

        var now = DateTimeOffset.UtcNow;
        var batch = new VoucherBatch
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Status = nameof(VoucherBatchStatus.Generating),
            Count = request.Count,
            PatternConfig = JsonSerializer.Serialize(new
            {
                request.Prefix, request.Suffix, request.Charset,
                request.CodeLength, request.Type, request.DiscountType,
                request.DiscountValue, request.Currency, request.ExpiresAt
            }),
            CreatedBy = userId,
            CreatedAt = now
        };

        await _repo.AddBatchAsync(batch);

        // Generate vouchers synchronously (for async: offload to worker)
        var generated = new List<Voucher>();
        var charset = request.Charset ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (int i = 0; i < request.Count; i++)
        {
            var code = GenerateCode(request.Prefix, request.Suffix, charset, request.CodeLength);
            var masked = code.Length > 4 ? new string('*', code.Length - 4) + code[^4..] : code;
            generated.Add(new Voucher
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProjectId = projectId,
                BatchId = batch.Id,
                Code = code,
                CodeMasked = masked,
                Type = request.Type,
                Status = nameof(VoucherStatus.Active),
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                Currency = request.Currency,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata,
                CreatedBy = userId,
                CreatedAt = now
            });
        }

        await _repo.AddBatchAsync(generated);

        batch.Status = nameof(VoucherBatchStatus.Completed);
        batch.GeneratedCount = generated.Count;
        batch.CompletedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateBatchAsync(batch);

        _audit.Write(organizationId, projectId, userId, "voucher.batch_generated", "VoucherBatch", batch.Id.ToString());

        return new VoucherBatchResponse
        {
            Id = batch.Id,
            Name = batch.Name,
            Status = batch.Status,
            Count = batch.Count,
            GeneratedCount = batch.GeneratedCount,
            CreatedAt = batch.CreatedAt,
            CompletedAt = batch.CompletedAt
        };
    }

    public async Task<List<VoucherBatchResponse>> GetBatchesAsync(Guid organizationId, Guid projectId)
    {
        var batches = await _repo.GetBatchesAsync(organizationId, projectId);
        return batches.Select(b => new VoucherBatchResponse
        {
            Id = b.Id,
            Name = b.Name,
            Status = b.Status,
            Count = b.Count,
            GeneratedCount = b.GeneratedCount,
            ErrorMessage = b.ErrorMessage,
            CreatedAt = b.CreatedAt,
            CompletedAt = b.CompletedAt
        }).ToList();
    }

    public async Task<VoucherPatternResponse> CreatePatternAsync(Guid organizationId, Guid projectId, CreateVoucherPatternRequest request, Guid userId)
    {
        var pattern = new VoucherPattern
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Prefix = request.Prefix,
            Suffix = request.Suffix,
            Charset = request.Charset,
            Length = request.Length,
            Pattern = request.Pattern,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repo.AddPatternAsync(pattern);

        return new VoucherPatternResponse
        {
            Id = pattern.Id,
            Name = pattern.Name,
            Description = pattern.Description,
            Prefix = pattern.Prefix,
            Suffix = pattern.Suffix,
            Charset = pattern.Charset,
            Length = pattern.Length,
            Pattern = pattern.Pattern,
            CreatedAt = pattern.CreatedAt
        };
    }

    public async Task<List<VoucherPatternResponse>> GetPatternsAsync(Guid organizationId, Guid projectId)
    {
        var patterns = await _repo.GetPatternsAsync(organizationId, projectId);
        return patterns.Select(p => new VoucherPatternResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Prefix = p.Prefix,
            Suffix = p.Suffix,
            Charset = p.Charset,
            Length = p.Length,
            Pattern = p.Pattern,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<VoucherRedemptionResponse> RedeemAsync(Guid organizationId, Guid projectId, RedeemVoucherRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Voucher code is required.");

        var code = request.Code.Trim().ToUpperInvariant();
        var voucher = await _repo.GetByCodeAsync(organizationId, projectId, code)
            ?? throw new ArgumentException("Voucher not found or not active in this project.");

        if (voucher.Status != nameof(VoucherStatus.Active))
            throw new InvalidOperationException($"Voucher is '{voucher.Status}' and cannot be redeemed.");

        if (voucher.ExpiresAt.HasValue && voucher.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Voucher has expired.");

        if (voucher.MaxRedemptions.HasValue && voucher.RedemptionCount >= voucher.MaxRedemptions)
            throw new InvalidOperationException("Voucher has reached its maximum redemption limit.");

        // Check idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _repo.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existing is not null)
            {
                return new VoucherRedemptionResponse
                {
                    Id = existing.Id,
                    VoucherId = existing.VoucherId,
                    Code = voucher.CodeMasked ?? voucher.Code,
                    CustomerId = existing.CustomerId,
                    OrderId = existing.OrderId,
                    Amount = existing.Amount,
                    Status = existing.Status,
                    CreatedAt = existing.CreatedAt
                };
            }
        }

        var now = DateTimeOffset.UtcNow;
        var redemption = new VoucherRedemption
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            OrganizationId = organizationId,
            ProjectId = projectId,
            CustomerId = request.CustomerId,
            OrderId = request.OrderId,
            Amount = request.Amount ?? voucher.DiscountValue,
            Currency = request.Currency ?? voucher.Currency,
            Status = "Completed",
            IdempotencyKey = request.IdempotencyKey,
            Metadata = request.Metadata,
            RedeemedBy = userId,
            CreatedAt = now
        };

        await _repo.AddRedemptionAsync(redemption);

        voucher.RedemptionCount++;
        voucher.UpdatedAt = now;
        voucher.UpdatedBy = userId;
        await _repo.UpdateAsync(voucher);

        await _repo.AddLifecycleEventAsync(new VoucherLifecycleEvent
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            OrganizationId = organizationId,
            ProjectId = projectId,
            EventType = "redeemed",
            ActorId = userId,
            Description = $"Redeemed {redemption.Amount?.ToString("F2")} {redemption.Currency}",
            CreatedAt = now
        });

        _audit.Write(organizationId, projectId, userId, "voucher.redeemed", "Voucher", voucher.Id.ToString(),
            new { redemptionId = redemption.Id, amount = redemption.Amount, currency = redemption.Currency });

        return new VoucherRedemptionResponse
        {
            Id = redemption.Id,
            VoucherId = voucher.Id,
            Code = voucher.CodeMasked ?? voucher.Code,
            CustomerId = redemption.CustomerId,
            OrderId = redemption.OrderId,
            Amount = redemption.Amount,
            Status = redemption.Status,
            CreatedAt = redemption.CreatedAt
        };
    }

    private static string GenerateCode(string? prefix, string? suffix, string charset, int length)
    {
        var chars = charset.ToCharArray();
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix)) sb.Append(prefix);
        for (int i = 0; i < length; i++)
            sb.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
        if (!string.IsNullOrEmpty(suffix)) sb.Append(suffix);
        return sb.ToString();
    }

    private static VoucherResponse MapToResponse(Voucher v) => new()
    {
        Id = v.Id,
        OrganizationId = v.OrganizationId,
        ProjectId = v.ProjectId,
        CampaignId = v.CampaignId,
        Code = v.Code,
        CodeMasked = v.CodeMasked,
        Type = v.Type,
        Status = v.Status,
        DiscountType = v.DiscountType,
        DiscountValue = v.DiscountValue,
        MaxDiscount = v.MaxDiscount,
        MinPurchase = v.MinPurchase,
        Currency = v.Currency,
        ApplyTo = v.ApplyTo,
        RedemptionCount = v.RedemptionCount,
        MaxRedemptions = v.MaxRedemptions,
        HolderId = v.HolderId,
        ExpiresAt = v.ExpiresAt,
        Metadata = v.Metadata,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt,
        RowVersion = v.RowVersion
    };
}
