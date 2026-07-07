using System.Text.Json;
using StackExchange.Redis;
using VoucherSystem.Application;

namespace VoucherSystem.Infrastructure;

/// <summary>
/// Redis-backed implementation of IProjectContextCache.
/// Follows the same pattern as RedisPermissionCache.
/// TTL defaults to 30 minutes. On cache miss the caller falls back to DB.
/// </summary>
public class RedisProjectContextCache : IProjectContextCache
{
    private readonly ConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public RedisProjectContextCache(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
    }

    public async Task<ProjectContextCacheEntry?> GetAsync(Guid organizationId, Guid projectId)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(organizationId, projectId);
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<ProjectContextCacheEntry>(cached.ToString());
        }
        return null;
    }

    public async Task SetAsync(Guid organizationId, Guid projectId, ProjectContextCacheEntry entry)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(organizationId, projectId);
        var data = JsonSerializer.Serialize(entry);
        await db.StringSetAsync(key, data, CacheDuration);
    }

    public async Task InvalidateAsync(Guid organizationId, Guid projectId)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(organizationId, projectId);
        await db.KeyDeleteAsync(key);
    }

    private static string BuildKey(Guid organizationId, Guid projectId)
        => $"projctx:{organizationId:N}:{projectId:N}";
}
