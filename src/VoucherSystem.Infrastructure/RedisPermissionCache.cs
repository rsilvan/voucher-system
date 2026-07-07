using System.Text.Json;
using StackExchange.Redis;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class RedisPermissionCache : IPermissionCache
{
    private readonly ConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public RedisPermissionCache(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
    }

    public async Task<List<string>> GetPermissionsAsync(Guid roleId)
    {
        var db = _redis.GetDatabase();
        var key = $"perms:{roleId}";
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<List<string>>(cached.ToString()) ?? new();
        }
        return new();
    }

    public async Task SetPermissionsAsync(Guid roleId, List<Permission> permissions)
    {
        var db = _redis.GetDatabase();
        var key = $"perms:{roleId}";
        var data = JsonSerializer.Serialize(permissions.Select(p => p.Key).ToList());
        await db.StringSetAsync(key, data, CacheDuration);
    }

    public async Task InvalidateRoleAsync(Guid roleId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"perms:{roleId}");
    }
}
