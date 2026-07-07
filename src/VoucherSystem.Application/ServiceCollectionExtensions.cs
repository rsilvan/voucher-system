using Microsoft.Extensions.DependencyInjection;

namespace VoucherSystem.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IProjectAccessService, ProjectAccessService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IGeoLocationService, GeoLocationService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IAreaService, AreaService>();
        return services;
    }
}
