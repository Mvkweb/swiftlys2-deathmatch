using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.Interfaces;

namespace SwiftlyS2_Deathmatch.Handlers;

public sealed class MapEventHandlers
{
    private readonly IMapConfigService _mapConfig;
    private readonly ISwiftlyCore _core;

    public MapEventHandlers(IMapConfigService mapConfig, ISwiftlyCore core)
    {
        _mapConfig = mapConfig;
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
        ApplyDeathmatchConvars();
    }

    public void ApplyDeathmatchConvars()
    {
        _core.Engine.ExecuteCommand("mp_buy_anywhere 1");
        _core.Engine.ExecuteCommand("mp_buytime 9999");
        _core.Engine.ExecuteCommand("mp_buy_during_radio_chat_time 9999");
        _core.Engine.ExecuteCommand("mp_free_armor 2");
    }
}
