using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2_Deathmatch.Interfaces;
using System.Collections.Generic;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class DamageReportService : IDamageReportService
{
    private readonly ISwiftlyCore _core;
    private readonly IDeathmatchConfigService _config;

    // AttackerSteamId -> VictimSteamId -> DamageDealt
    private readonly Dictionary<ulong, Dictionary<ulong, int>> _damageMatrix = new();

    public DamageReportService(ISwiftlyCore core, IDeathmatchConfigService config)
    {
        _core = core;
        _config = config;
    }

    private ulong GetPlayerKey(IPlayer? player)
    {
        if (player is null || !player.IsValid) return 0;
        return player.SteamID != 0 ? player.SteamID : (0xF000000000000000UL | (uint)player.Slot);
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        if (!_config.Config.EnableDamageReports) return HookResult.Continue;

        var victim = @event.UserIdPlayer;
        var attacker = @event.AttackerPlayer;
        var damage = @event.ActualDmgHealth;
        
        var attackerKey = GetPlayerKey(attacker);
        var victimKey = GetPlayerKey(victim);

        if (attackerKey == 0 || victimKey == 0 || attackerKey == victimKey) return HookResult.Continue;

        if (!_damageMatrix.ContainsKey(attackerKey))
            _damageMatrix[attackerKey] = new Dictionary<ulong, int>();

        if (!_damageMatrix[attackerKey].ContainsKey(victimKey))
            _damageMatrix[attackerKey][victimKey] = 0;

        _damageMatrix[attackerKey][victimKey] += damage;

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var victim = @event.UserIdPlayer;
        var attacker = @event.AttackerPlayer;
        var weapon = @event.Weapon;

        if (!_config.Config.EnableDamageReports)
        {
             ClearPlayerData(GetPlayerKey(victim));
             return HookResult.Continue;
        }

        var victimKey = GetPlayerKey(victim);
        var attackerKey = GetPlayerKey(attacker);

        if (victimKey != 0 && victim is not null && victim.IsValid)
        {
            var prefix = _config.Config.ChatPrefix;
            
            if (attackerKey != 0 && attackerKey != victimKey && attacker is not null && attacker.IsValid)
            {
                var damageToAttacker = 0;
                if (_damageMatrix.TryGetValue(victimKey, out var victimDealt) && victimDealt.TryGetValue(attackerKey, out var dmgTo))
                    damageToAttacker = dmgTo;

                var damageFromAttacker = 0;
                if (_damageMatrix.TryGetValue(attackerKey, out var attackerDealt) && attackerDealt.TryGetValue(victimKey, out var dmgFrom))
                    damageFromAttacker = dmgFrom;

                var weaponName = weapon.Replace("weapon_", "");
                victim.SendMessage(MessageType.Chat, $"{prefix} You were killed by {{green}}{attacker.Controller.PlayerName}{{default}} with {{lightred}}{weaponName}{{default}}.");
                victim.SendMessage(MessageType.Chat, $"{prefix} {{lightred}}Took: {damageFromAttacker} damage{{default}} | {{green}}Gave: {damageToAttacker} damage{{default}}");
            }
        }

        ClearPlayerData(victimKey);
        return HookResult.Continue;
    }

    private void ClearPlayerData(ulong steamId)
    {
        if (steamId == 0) return;
        
        // Clear outgoing damage from this player
        _damageMatrix.Remove(steamId);

        // Clear incoming damage to this player from others
        foreach (var dict in _damageMatrix.Values)
        {
            dict.Remove(steamId);
        }
    }
}
