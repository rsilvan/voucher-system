using System.Text.Json;
using CPromotions = VoucherSystem.Contracts.Promotions;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _repo;
    private readonly IProjectRepository _projectRepo;
    private readonly IAuditLogWriter _audit;

    public PromotionService(IPromotionRepository repo, IProjectRepository projectRepo, IAuditLogWriter audit)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _audit = audit;
    }

    public async Task<CPromotions.PromotionPlanResponse> GetPlanAsync(Guid organizationId, Guid sourceProjectId, Guid targetProjectId)
    {
        var source = await _projectRepo.GetByIdAsync(sourceProjectId, organizationId);
        var target = await _projectRepo.GetByIdAsync(targetProjectId, organizationId);

        if (source is null || target is null)
            throw new KeyNotFoundException("Source or target project not found.");

        if (sourceProjectId == targetProjectId)
            throw new ArgumentException("Source and target projects must be different.");

        var items = new List<CPromotions.PromotionPlanItemResponse>
        {
            new()
            {
                ResourceType = "BrandProfile",
                SourceName = source.Name,
                SourceId = sourceProjectId.ToString(),
                Action = "create",
                Detail = "BrandProfile will be copied from source to target.",
            }
        };

        return new CPromotions.PromotionPlanResponse
        {
            Items = items,
            HasConflicts = false,
            HasUnsupported = false,
        };
    }

    public async Task<CPromotions.PromotionJobResponse> CreatePromotionAsync(Guid organizationId, Guid sourceProjectId, CPromotions.CreatePromotionRequest request, Guid actorId)
    {
        // Check idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _repo.GetByIdempotencyKeyAsync(request.IdempotencyKey, organizationId);
            if (existing is not null)
                return MapJob(existing);
        }

        // Validate projects
        var source = await _projectRepo.GetByIdAsync(sourceProjectId, organizationId);
        var target = await _projectRepo.GetByIdAsync(request.TargetProjectId, organizationId);
        if (source is null || target is null)
            throw new KeyNotFoundException("Source or target project not found.");

        var job = new ProjectPromotionJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SourceProjectId = sourceProjectId,
            TargetProjectId = request.TargetProjectId,
            Status = nameof(PromotionJobStatus.Pending),
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Quick plan
        var plan = await GetPlanAsync(organizationId, sourceProjectId, request.TargetProjectId);
        job.PlanJson = JsonSerializer.Serialize(plan);
        job.Status = nameof(PromotionJobStatus.Planning);

        await _repo.AddJobAsync(job);

        _audit.Write(organizationId, sourceProjectId, actorId, "promotion.created", "ProjectPromotion", job.Id.ToString());

        return MapJob(job);
    }

    public async Task<CPromotions.PromotionJobResponse?> GetJobAsync(Guid organizationId, Guid jobId)
    {
        var job = await _repo.GetJobByIdAsync(jobId, organizationId);
        return job is null ? null : MapJob(job);
    }

    public async Task<List<CPromotions.PromotionJobResponse>> GetJobsAsync(Guid organizationId, Guid projectId)
    {
        var jobs = await _repo.GetJobsByProjectAsync(projectId, organizationId);
        return jobs.Select(MapJob).ToList();
    }

    public async Task<CPromotions.PromotionDiffResponse> GetPromotionDiffAsync(Guid organizationId, Guid jobId)
    {
        var job = await _repo.GetJobByIdAsync(jobId, organizationId);
        if (job is null)
            throw new KeyNotFoundException("Promotion job not found.");

        var items = new List<CPromotions.PromotionDiffItem>();

        // Parse PlanJson to extract planned items
        if (!string.IsNullOrEmpty(job.PlanJson))
        {
            try
            {
                var plan = JsonSerializer.Deserialize<CPromotions.PromotionPlanResponse>(job.PlanJson);
                if (plan?.Items is not null)
                {
                    foreach (var item in plan.Items)
                    {
                        items.Add(new CPromotions.PromotionDiffItem
                        {
                            ResourceType = item.ResourceType,
                            Action = item.Action,
                            SourceId = item.SourceId,
                            SourceName = item.SourceName,
                            Detail = item.Detail,
                            Status = "planned"
                        });
                    }
                }
            }
            catch (JsonException)
            {
                // If plan can't be deserialized, add it as raw
                items.Add(new CPromotions.PromotionDiffItem
                {
                    ResourceType = "Plan",
                    Action = "unknown",
                    Status = "planned"
                });
            }
        }

        // Parse ResultJson to determine which items completed
        if (!string.IsNullOrEmpty(job.ResultJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(job.ResultJson);
                if (doc.RootElement.TryGetProperty("mappings", out var mappings) && mappings.ValueKind == JsonValueKind.Array)
                {
                    foreach (var mapping in mappings.EnumerateArray())
                    {
                        var sourceType = mapping.TryGetProperty("sourceType", out var st) ? st.GetString() : null;
                        var sourceId = mapping.TryGetProperty("sourceId", out var sid) ? sid.GetString() : null;
                        var targetId = mapping.TryGetProperty("targetId", out var tid) ? tid.GetString() : null;
                        var status = mapping.TryGetProperty("status", out var s) ? s.GetString() : "completed";

                        // Check if this item was already in the plan
                        var existing = items.FirstOrDefault(i => i.ResourceType == sourceType && i.SourceId == sourceId);
                        if (existing is not null)
                        {
                            existing.TargetId = targetId;
                            existing.Status = status ?? "completed";
                        }
                        else
                        {
                            items.Add(new CPromotions.PromotionDiffItem
                            {
                                ResourceType = sourceType ?? "unknown",
                                Action = "promote",
                                SourceId = sourceId,
                                TargetId = targetId,
                                Status = status ?? "completed"
                            });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed result
            }
        }

        return new CPromotions.PromotionDiffResponse
        {
            JobId = job.Id,
            Status = job.Status,
            Items = items
        };
    }

    public async Task<bool> CancelJobAsync(Guid organizationId, Guid jobId)
    {
        var job = await _repo.GetJobByIdAsync(jobId, organizationId);
        if (job is null) return false;

        if (job.Status is nameof(PromotionJobStatus.Completed) or nameof(PromotionJobStatus.Cancelled))
            return false;

        job.Status = nameof(PromotionJobStatus.Cancelled);
        await _repo.UpdateJobAsync(job);
        return true;
    }

    private static CPromotions.PromotionJobResponse MapJob(ProjectPromotionJob job) => new()
    {
        Id = job.Id,
        SourceProjectId = job.SourceProjectId,
        TargetProjectId = job.TargetProjectId,
        Status = job.Status,
        PlanJson = job.PlanJson,
        ResultJson = job.ResultJson,
        ErrorMessage = job.ErrorMessage,
        CreatedAt = job.CreatedAt,
        CompletedAt = job.CompletedAt,
    };
}
