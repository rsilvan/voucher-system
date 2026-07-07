namespace VoucherSystem.Application;

public interface IAuditLogWriter
{
    void Write(Guid accountId, Guid? projectId, Guid? actorId, string action,
        string entityType, string entityId, object? metadata = null);
    Task SaveAsync();
}

public interface IAuditLogReader
{
    Task<(List<Domain.AuditLog> Items, int TotalCount)> GetLogsAsync(
        Guid organizationId, Guid? projectId, string? action, string? entityType,
        int skip, int take);
}
