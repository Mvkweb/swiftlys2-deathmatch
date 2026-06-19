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
    private readonly IEloScoreService _eloScoreService;
    private readonly IDeathmatchConfigService _config;
    private Guid _playerSpawnPostHook;
    private Guid _playerDeathPreHook;
    private Guid _playerConnectFullHook;
    private Guid _playerDisconnectHook;
    private bool _isSimulatedKillfeed = false;

    public PlayerEventHandlers(ISwiftlyCore core, ISpawnEvaluatorService spawnEvaluator, IEloScoreService eloScoreService, IDeathmatchConfigService config)
    {
        _core = core;
        _spawnEvaluator = spawnEvaluator;
        _eloScoreService = eloScoreService;
        _config = config;
    }

    public void Register()
    {
        _playerSpawnPostHook = _core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawnPost);
        _playerDeathPreHook = _core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeathPre);
        _playerConnectFullHook = _core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        _playerDisconnectHook = _core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    public void Unregister()
    {
        if (_playerSpawnPostHook != Guid.Empty)
        {
            _core.GameEvent.Unhook(_playerSpawnPostHook);
            _playerSpawnPostHook = Guid.Empty;
        }
        if (_playerDeathPreHook != Guid.Empty)
        {
            _core.GameEvent.Unhook(_playerDeathPreHook);
            _playerDeathPreHook = Guid.Empty;
        }
        if (_playerConnectFullHook != Guid.Empty)
        {
            _core.GameEvent.Unhook(_playerConnectFullHook);
            _playerConnectFullHook = Guid.Empty;
        }
        if (_playerDisconnectHook != Guid.Empty)
        {
            _core.GameEvent.Unhook(_playerDisconnectHook);
            _playerDisconnectHook = Guid.Empty;
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

    private HookResult OnPlayerDeathPre(EventPlayerDeath @event)
    {
        if (_isSimulatedKillfeed) return HookResult.Continue;

        var victim = @event.UserIdPlayer;
        if (victim is null || !victim.IsValid) return HookResult.Continue;

        if (_config.Config.OnlyShowPlayerKillfeed)
        {
            @event.DontBroadcast = true;
            
            var attacker = @event.AttackerPlayer;
            var attackerSlot = attacker?.Slot ?? -1;
            var victimSlot = victim.Slot;

            _isSimulatedKillfeed = true;

            Action<EventPlayerDeath> cloneProps = ev => {
                ev.UserId = @event.UserId;
                ev.Attacker = @event.Attacker;
                ev.Weapon = @event.Weapon;
                ev.Headshot = @event.Headshot;
            };

            if (attackerSlot >= 0)
            {
                _core.GameEvent.FireToPlayer<EventPlayerDeath>(attackerSlot, cloneProps);
            }

            if (victimSlot >= 0 && victimSlot != attackerSlot)
            {
                _core.GameEvent.FireToPlayer<EventPlayerDeath>(victimSlot, cloneProps);
            }

            _isSimulatedKillfeed = false;
        }

        // Respawn immediately — new pawn created, old one dies silently (skips ragdoll entirely)
        _core.Scheduler.DelayBySeconds(0.1f, () =>
        {
             if (victim.IsValid)
             {
                 victim.Respawn();
             }
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        if (player is not null && player.IsValid)
        {
            _ = _eloScoreService.OnClientConnectAsync(player);
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;
        if (player is not null && player.IsValid)
        {
            _ = _eloScoreService.OnClientDisconnectAsync(player);
        }
        return HookResult.Continue;
    }
}
