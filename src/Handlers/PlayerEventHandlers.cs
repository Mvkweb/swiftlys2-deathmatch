using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Interfaces;
using System;
using System.Linq;

namespace SwiftlyS2_Deathmatch.Handlers;

public sealed class PlayerEventHandlers
{
    private readonly ISwiftlyCore _core;
    private readonly ISpawnEvaluatorService _spawnEvaluator;
    private Guid _playerSpawnPostHook;

    public PlayerEventHandlers(ISwiftlyCore core, ISpawnEvaluatorService spawnEvaluator)
    {
        _core = core;
        _spawnEvaluator = spawnEvaluator;
    }

    public void Register()
    {
        _playerSpawnPostHook = _core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawnPost);
    }

    public void Unregister()
    {
        if (_playerSpawnPostHook != Guid.Empty)
        {
            _core.GameEvent.Unhook(_playerSpawnPostHook);
            _playerSpawnPostHook = Guid.Empty;
        }
    }

    private HookResult OnPlayerSpawnPost(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;
        if (player is null || !player.IsValid) return HookResult.Continue;

        var team = (Team)player.Controller.TeamNum;
        
        var bestSpawn = _spawnEvaluator.GetBestSpawn(player, team);
        
        if (bestSpawn is not null)
        {
            // Need to delay the teleport slightly after spawn to ensure the engine doesn't override it
            _core.Scheduler.NextTick(() =>
            {
                if (player is not null && player.IsValid && player.PlayerPawn is not null)
                {
                    player.Teleport(bestSpawn.Position, bestSpawn.Angle, Vector.Zero);
                }
            });
        }

        return HookResult.Continue;
    }
}
