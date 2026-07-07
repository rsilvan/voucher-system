using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VoucherSystem.Application;

namespace VoucherSystem.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();
        services.AddScoped<IProjectAccessRepository, ProjectAccessRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IGeoLocationRepository, GeoLocationRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddSingleton<IPermissionCache>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
            return new RedisPermissionCache(redisConn);
        });
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IProjectContextCache>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
            return new RedisProjectContextCache(redisConn);
        });
        return services;
    }
}
