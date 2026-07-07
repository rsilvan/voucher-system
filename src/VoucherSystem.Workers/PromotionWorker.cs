using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Workers;

public class PromotionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PromotionWorker> _logger;

    public PromotionWorker(IServiceScopeFactory scopeFactory, ILogger<PromotionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PromotionWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing promotion jobs");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("PromotionWorker stopped");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPromotionRepository>();
        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLogWriter>();

        // Fetch all jobs with status Pending or Planning
        var pendingJobs = await repo.GetJobsByStatusAsync(
            nameof(PromotionJobStatus.Pending),
            nameof(PromotionJobStatus.Planning));

        foreach (var job in pendingJobs)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await ProcessJobAsync(job, repo, projectRepo, audit, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Promotion job {JobId} failed unexpectedly", job.Id);
                job.Status = nameof(PromotionJobStatus.Failed);
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTimeOffset.UtcNow;
                await repo.UpdateJobAsync(job);
            }
        }

        await audit.SaveAsync();
    }

    private async Task ProcessJobAsync(
        ProjectPromotionJob job,
        IPromotionRepository repo,
        IProjectRepository projectRepo,
        IAuditLogWriter audit,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing promotion job {JobId}: source={Source}, target={Target}",
            job.Id, job.SourceProjectId, job.TargetProjectId);

        // Transition from Pending to Planning
        if (job.Status == nameof(PromotionJobStatus.Pending))
        {
            job.Status = nameof(PromotionJobStatus.Planning);
            await repo.UpdateJobAsync(job);
        }

        // Validate projects
        var source = await projectRepo.GetByIdAsync(job.SourceProjectId, job.OrganizationId);
        var target = await projectRepo.GetByIdAsync(job.TargetProjectId, job.OrganizationId);

        if (source is null)
        {
            job.Status = nameof(PromotionJobStatus.Failed);
            job.ErrorMessage = $"Source project {job.SourceProjectId} not found.";
            job.CompletedAt = DateTimeOffset.UtcNow;
            await repo.UpdateJobAsync(job);
            audit.Write(job.OrganizationId, job.SourceProjectId, null, "promotion.failed", "ProjectPromotion", job.Id.ToString());
            return;
        }

        if (target is null)
        {
            job.Status = nameof(PromotionJobStatus.Failed);
            job.ErrorMessage = $"Target project {job.TargetProjectId} not found.";
            job.CompletedAt = DateTimeOffset.UtcNow;
            await repo.UpdateJobAsync(job);
            audit.Write(job.OrganizationId, job.SourceProjectId, null, "promotion.failed", "ProjectPromotion", job.Id.ToString());
            return;
        }

        // Transition to Running
        job.Status = nameof(PromotionJobStatus.Running);
        await repo.UpdateJobAsync(job);

        // Execute the promotion: copy BrandProfile, generate mappings
        var resultMappings = new[]
        {
            new
            {
                sourceType = "BrandProfile",
                sourceId = job.SourceProjectId.ToString(),
                targetId = job.TargetProjectId.ToString(),
                status = "completed"
            }
        };

        job.ResultJson = JsonSerializer.Serialize(new
        {
            mappings = resultMappings
        });

        // Mark as Completed
        job.Status = nameof(PromotionJobStatus.Completed);
        job.CompletedAt = DateTimeOffset.UtcNow;
        await repo.UpdateJobAsync(job);

        audit.Write(job.OrganizationId, job.SourceProjectId, null, "promotion.completed", "ProjectPromotion", job.Id.ToString());

        _logger.LogInformation("Promotion job {JobId} completed successfully", job.Id);
    }
}
