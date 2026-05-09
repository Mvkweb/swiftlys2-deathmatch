using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.DependencyInjection;
using SwiftlyS2_Deathmatch.Handlers;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Logging;

namespace SwiftlyS2_Deathmatch;

[PluginMetadata(Id = "Deathmatch", Version = "1.0.0", Name = "SwiftlyS2 Deathmatch", Author = "Mvk", Description = "Deathmatch plugin with spawn editor.")]
public sealed class SwiftlyS2_Deathmatch : BasePlugin
{
    private IServiceProvider? _serviceProvider;
    private CommandHandlers? _commandHandlers;
    private MapEventHandlers? _mapEventHandlers;

    public SwiftlyS2_Deathmatch(ISwiftlyCore core) : base(core)
    {
        _serviceProvider = ServiceProviderFactory.CreateServiceProvider(Core, Core.Logger);
    }

    public override void Load(bool hotReload)
    {
        if (_serviceProvider is null) return;

        _commandHandlers = _serviceProvider.GetRequiredService<CommandHandlers>();
        _mapEventHandlers = _serviceProvider.GetRequiredService<MapEventHandlers>();

        _commandHandlers.Register();
        _mapEventHandlers.Register();

        // Initial load if map is already loaded
        var mapName = (Core.Engine.GlobalVars.MapName.Value ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(mapName))
        {
            var mapConfig = _serviceProvider.GetRequiredService<IMapConfigService>();
            mapConfig.Load(mapName);
            _mapEventHandlers.ApplyDeathmatchConvars();
        }

        Core.Logger.LogPluginInformation("Deathmatch: Plugin loaded successfully.");
    }

    public override void Unload()
    {
        _commandHandlers?.Unregister();
        _mapEventHandlers?.Unregister();

        _commandHandlers = null;
        _mapEventHandlers = null;

        ServiceProviderFactory.DisposeServiceProvider(_serviceProvider);
        _serviceProvider = null;
    }
}
