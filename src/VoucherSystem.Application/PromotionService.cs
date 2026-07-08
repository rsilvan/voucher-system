using System.Text.Json;
using VoucherSystem.Contracts.Promotions;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _repo;
    private readonly IAuditLogWriter _audit;

    public PromotionService(IPromotionRepository repo, IAuditLogWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<PromotionResponse> CreateAsync(Guid organizationId, Guid projectId, CreatePromotionRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Promotion name is required.");

        var now = DateTimeOffset.UtcNow;
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            Status = nameof(PromotionStatus.Draft),
            Priority = request.Priority,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Metadata = request.Metadata,
            CreatedBy = userId,
            CreatedAt = now
        };

        await _repo.AddAsync(promotion);

        // Create discount definitions
        foreach (var defReq in request.DiscountDefinitions)
        {
            var def = new DiscountDefinition
            {
                Id = Guid.NewGuid(),
                PromotionId = promotion.Id,
                OrganizationId = organizationId,
                ProjectId = projectId,
                DiscountType = defReq.DiscountType,
                ValueType = defReq.ValueType,
                Value = defReq.Value,
                MaxDiscount = defReq.MaxDiscount,
                MinPurchase = defReq.MinPurchase,
                ApplyTo = defReq.ApplyTo,
                ItemScope = defReq.ItemScope is not null ? JsonSerializer.Serialize(defReq.ItemScope) : null,
                BuyXQuantity = defReq.BuyXQuantity,
                GetYQuantity = defReq.GetYQuantity,
                GetYDiscountPercent = defReq.GetYDiscountPercent,
                CreatedBy = userId,
                CreatedAt = now
            };

            foreach (var tierReq in defReq.Tiers)
            {
                def.Tiers.Add(new DiscountTier
                {
                    Id = Guid.NewGuid(),
                    DiscountDefinitionId = def.Id,
                    OrganizationId = organizationId,
                    ProjectId = projectId,
                    FromValue = tierReq.FromValue,
                    ToValue = tierReq.ToValue,
                    DiscountValue = tierReq.DiscountValue,
                    DiscountType = tierReq.DiscountType,
                    CreatedAt = now
                });
            }

            await _repo.AddDiscountDefinitionAsync(def);
        }

        _audit.Write(organizationId, projectId, userId, "promotion.created", "Promotion", promotion.Id.ToString());
        return await GetByIdAsync(organizationId, projectId, promotion.Id) ?? MapToResponse(promotion)!;
    }

    public async Task<PromotionResponse?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id)
    {
        var promotion = await _repo.GetByIdAsync(organizationId, projectId, id);
        if (promotion is null) return null;

        var defs = await _repo.GetDiscountDefinitionsAsync(promotion.Id);
        return MapToResponse(promotion, defs);
    }

    public async Task<PromotionListResponse> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status)
    {
        var items = await _repo.GetListAsync(organizationId, projectId, page, pageSize, status);
        var total = await _repo.GetCountAsync(organizationId, projectId, status);

        return new PromotionListResponse
        {
            Items = items.Select(p => new PromotionSummaryResponse
            {
                Id = p.Id,
                Name = p.Name,
                Type = p.Type,
                Status = p.Status,
                Priority = p.Priority,
                CreatedAt = p.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PromotionResponse?> UpdateAsync(Guid organizationId, Guid projectId, Guid id, UpdatePromotionRequest request, Guid userId)
    {
        var promotion = await _repo.GetByIdAsync(organizationId, projectId, id);
        if (promotion is null) return null;

        if (request.Name is not null) promotion.Name = request.Name.Trim();
        if (request.Description is not null) promotion.Description = request.Description.Trim();
        if (request.Priority.HasValue) promotion.Priority = request.Priority.Value;
        if (request.StartAt is not null) promotion.StartAt = request.StartAt;
        if (request.EndAt is not null) promotion.EndAt = request.EndAt;
        if (request.Metadata is not null) promotion.Metadata = request.Metadata;

        promotion.UpdatedAt = DateTimeOffset.UtcNow;
        promotion.UpdatedBy = userId;
        await _repo.UpdateAsync(promotion);

        _audit.Write(organizationId, projectId, userId, "promotion.updated", "Promotion", promotion.Id.ToString());
        return await GetByIdAsync(organizationId, projectId, id);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id)
    {
        var promotion = await _repo.GetByIdAsync(organizationId, projectId, id);
        if (promotion is null) return false;

        promotion.Status = nameof(PromotionStatus.Archived);
        promotion.ArchivedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(promotion);
        return true;
    }

    public async Task<PromotionActionResponse> ExecuteActionAsync(Guid organizationId, Guid projectId, Guid id, string action, PromotionActionRequest request, Guid userId)
    {
        var promotion = await _repo.GetByIdAsync(organizationId, projectId, id)
            ?? throw new ArgumentException("Promotion not found.");

        var validTransitions = GetValidTransitions(promotion.Status);
        if (!validTransitions.Contains(action))
            throw new InvalidOperationException($"Cannot transition from '{promotion.Status}' to '{action}'.");

        promotion.Status = action switch
        {
            "activate" => nameof(PromotionStatus.Active),
            "pause" => nameof(PromotionStatus.Paused),
            "expire" => nameof(PromotionStatus.Expired),
            "archive" => nameof(PromotionStatus.Archived),
            _ => throw new ArgumentException($"Unknown action '{action}'.")
        };

        promotion.UpdatedAt = DateTimeOffset.UtcNow;
        promotion.UpdatedBy = userId;
        await _repo.UpdateAsync(promotion);

        _audit.Write(organizationId, projectId, userId, $"promotion.{action}", "Promotion", promotion.Id.ToString());

        return new PromotionActionResponse
        {
            OperationId = promotion.Id.ToString(),
            Status = promotion.Status,
            Events = new List<string> { $"promotion.{action}" }
        };
    }

    public async Task<PromotionPreviewResponse> PreviewAsync(Guid organizationId, Guid projectId, PromotionPreviewRequest request, Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var promotions = await _repo.GetActivePromotionsAsync(organizationId, projectId);

        var totalDiscount = 0m;
        var discounts = new List<CalculatedDiscountItem>();

        foreach (var promo in promotions.OrderBy(p => p.Priority))
        {
            var defs = await _repo.GetDiscountDefinitionsAsync(promo.Id);
            foreach (var def in defs)
            {
                var discountAmount = CalculateDiscount(def, request);
                if (discountAmount > 0)
                {
                    totalDiscount += discountAmount;
                    discounts.Add(new CalculatedDiscountItem
                    {
                        PromotionId = promo.Id,
                        PromotionName = promo.Name,
                        Type = def.DiscountType,
                        DiscountAmount = discountAmount,
                        Description = $"{def.DiscountType} {def.Value}{GetSuffix(def.DiscountType)}"
                    });
                }
            }
        }

        // Save preview
        var preview = new PromotionPreview
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            OrderContext = JsonSerializer.Serialize(request),
            CalculatedDiscounts = JsonSerializer.Serialize(discounts),
            TotalDiscount = totalDiscount,
            FinalTotal = request.OrderTotal - totalDiscount,
            Currency = request.Currency,
            CreatedBy = userId,
            CreatedAt = now,
            ExpiresAt = now.AddHours(1)
        };

        await _repo.AddPreviewAsync(preview);

        return new PromotionPreviewResponse
        {
            OriginalTotal = request.OrderTotal,
            TotalDiscount = totalDiscount,
            FinalTotal = request.OrderTotal - totalDiscount,
            Currency = request.Currency,
            Discounts = discounts
        };
    }

    private static decimal CalculateDiscount(DiscountDefinition def, PromotionPreviewRequest request)
    {
        if (def.MinPurchase.HasValue && request.OrderTotal < def.MinPurchase.Value)
            return 0;

        return def.DiscountType switch
        {
            "Percentage" => request.OrderTotal * def.Value / 100m,
            "FixedAmount" => def.Value,
            "FreeShipping" => 0, // flat rate, depends on order context
            "BuyXGetY" => def.GetYDiscountPercent.HasValue
                ? request.Items.Take(def.GetYQuantity ?? 1).Sum(i => i.Price * i.Quantity) * (def.GetYDiscountPercent.Value / 100m)
                : 0,
            _ => 0
        };
    }

    private static string GetSuffix(string type) => type switch
    {
        "Percentage" => "%",
        "FixedAmount" => "",
        _ => ""
    };

    private static HashSet<string> GetValidTransitions(string status) => status switch
    {
        "Draft" => new() { "activate", "archive" },
        "Active" => new() { "pause", "expire" },
        "Paused" => new() { "activate", "expire" },
        "Expired" => new() { "archive" },
        "Archived" => new(),
        _ => new()
    };

    private static PromotionResponse MapToResponse(Promotion p, List<DiscountDefinition>? defs = null) => new()
    {
        Id = p.Id,
        OrganizationId = p.OrganizationId,
        ProjectId = p.ProjectId,
        CampaignId = p.CampaignId,
        Name = p.Name,
        Description = p.Description,
        Type = p.Type,
        Status = p.Status,
        Priority = p.Priority,
        StartAt = p.StartAt,
        EndAt = p.EndAt,
        Metadata = p.Metadata,
        CreatedBy = p.CreatedBy,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        RowVersion = p.RowVersion,
        DiscountDefinitions = defs?.Select(d => new DiscountDefinitionResponse
        {
            Id = d.Id,
            DiscountType = d.DiscountType,
            ValueType = d.ValueType,
            Value = d.Value,
            MaxDiscount = d.MaxDiscount,
            MinPurchase = d.MinPurchase,
            ApplyTo = d.ApplyTo,
            ItemScope = d.ItemScope,
            BuyXQuantity = d.BuyXQuantity,
            GetYQuantity = d.GetYQuantity,
            GetYDiscountPercent = d.GetYDiscountPercent,
            Tiers = d.Tiers.Select(t => new DiscountTierResponse
            {
                Id = t.Id,
                FromValue = t.FromValue,
                ToValue = t.ToValue,
                DiscountValue = t.DiscountValue,
                DiscountType = t.DiscountType
            }).ToList()
        }).ToList() ?? new()
    };
}
