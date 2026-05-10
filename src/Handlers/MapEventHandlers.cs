using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2_Deathmatch.Interfaces;
using System;

namespace SwiftlyS2_Deathmatch.Handlers;

public sealed class MapEventHandlers
{
    private readonly IMapConfigService _mapConfig;
    private readonly IDeathmatchConfigService _config;
    private readonly ISwiftlyCore _core;
    private Guid _roundStartHook;

    public MapEventHandlers(IMapConfigService mapConfig, IDeathmatchConfigService config, ISwiftlyCore core)
    {
        _mapConfig = mapConfig;
        _config = config;
        _core = core;
    }

    public void Register()
    {
        _core.Event.OnMapLoad += OnMapLoad;
        _roundStartHook = _core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
    }

    public void Unregister()
    {
        _core.Event.OnMapLoad -= OnMapLoad;
        if (_roundStartHook != Guid.Empty)
        {
            _core.GameEvent.Unhook(_roundStartHook);
            _roundStartHook = Guid.Empty;
        }
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _mapConfig.Load(@event.MapName);
        _config.ApplyToConvars();
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        // Enforce critical cvars again at round start in case server configs override them
        _core.Engine.ExecuteCommand("mp_warmup_end");
        _core.Engine.ExecuteCommand($"mp_buytime {_config.Config.BuyTime}");
        _core.Engine.ExecuteCommand($"mp_buy_anywhere {(_config.Config.EnableBuyAnywhere ? 1 : 0)}");
        
        return HookResult.Continue;
    }
}


