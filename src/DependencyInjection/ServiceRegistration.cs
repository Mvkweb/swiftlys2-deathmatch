using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Services;
using SwiftlyS2_Deathmatch.Handlers;

namespace SwiftlyS2_Deathmatch.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddDeathmatchServices(this IServiceCollection services)
    {
        services.AddSingleton<IDeathmatchConfigService, DeathmatchConfigService>();
        services.AddSingleton<IMapConfigService, MapConfigService>();
        services.AddSingleton<ISpawnVisualizationService, SpawnVisualizationService>();
        services.AddSingleton<IDamageReportService, DamageReportService>();
        
        services.AddSingleton<CommandHandlers>();
        services.AddSingleton<MapEventHandlers>();
        
        return services;
    }
}
