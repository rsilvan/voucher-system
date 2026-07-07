using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class AuditLogWriter : IAuditLogWriter
{
    private readonly VoucherSystemDbContext _db;
    private readonly List<AuditLog> _buffer = new();

    public AuditLogWriter(VoucherSystemDbContext db) => _db = db;

    public void Write(Guid accountId, Guid? projectId, Guid? actorId, string action,
        string entityType, string entityId, object? metadata = null)
    {
        _buffer.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = accountId,
            ProjectId = projectId,
            ActorUserId = actorId,
            Action = action,
            ResourceType = entityType,
            ResourceId = entityId,
            MetadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : "{}",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task SaveAsync()
    {
        if (_buffer.Count == 0) return;
        _db.AuditLogs.AddRange(_buffer);
        await _db.SaveChangesAsync();
        _buffer.Clear();
    }
}

public class AuditLogReader : IAuditLogReader
{
    private readonly VoucherSystemDbContext _db;

    public AuditLogReader(VoucherSystemDbContext db) => _db = db;

    public async Task<(List<AuditLog> Items, int TotalCount)> GetLogsAsync(
        Guid organizationId, Guid? projectId, string? action, string? entityType,
        int skip, int take)
    {
        var query = _db.AuditLogs.Where(l => l.OrganizationId == organizationId);

        if (projectId.HasValue) query = query.Where(l => l.ProjectId == projectId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(l => l.Action == action);
        if (!string.IsNullOrEmpty(entityType)) query = query.Where(l => l.ResourceType == entityType);

        var total = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query);
        var items = await query.OrderByDescending(l => l.CreatedAt).Skip(skip).Take(take).ToListAsync();

        return (items, total);
    }
}
