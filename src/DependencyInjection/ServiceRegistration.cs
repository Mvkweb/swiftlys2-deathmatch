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
        services.AddSingleton<ISpawnEvaluatorService, SpawnEvaluatorService>();
        services.AddSingleton<IMessageSuppressionService, MessageSuppressionService>();
        services.AddSingleton<IEloDatabaseService, EloDatabaseService>();
        services.AddSingleton<IEloScoreService, EloScoreService>();
        services.AddSingleton<IWeaponLoadoutService, WeaponLoadoutService>();
        services.AddSingleton<IHeadshotModeService, HeadshotModeService>();
        
        services.AddSingleton<CommandHandlers>();
        services.AddSingleton<MapEventHandlers>();
        services.AddSingleton<PlayerEventHandlers>();
        
        return services;
    }
}
