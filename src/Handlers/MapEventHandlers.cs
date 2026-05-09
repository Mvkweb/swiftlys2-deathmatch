using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.Interfaces;

namespace SwiftlyS2_Deathmatch.Handlers;

public sealed class MapEventHandlers
{
    private readonly IMapConfigService _mapConfig;
    private readonly IDeathmatchConfigService _config;
    private readonly ISwiftlyCore _core;

    public MapEventHandlers(IMapConfigService mapConfig, IDeathmatchConfigService config, ISwiftlyCore core)
    {
        _mapConfig = mapConfig;
        _config = config;
        _core = core;
    }

    public void Register()
    {
        _core.Event.OnMapLoad += OnMapLoad;
    }

    public void Unregister()
    {
        _core.Event.OnMapLoad -= OnMapLoad;
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _mapConfig.Load(@event.MapName);
        _config.ApplyToConvars();
    }
}

