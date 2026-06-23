using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Logging;
using SwiftlyS2_Deathmatch.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class EloScoreService : IEloScoreService
{
    private readonly ISwiftlyCore _core;
    private readonly IDeathmatchConfigService _config;
    private readonly IEloDatabaseService _db;
    
    private readonly ConcurrentDictionary<ulong, PlayerStats> _playerStats = new();

    public EloScoreService(ISwiftlyCore core, IDeathmatchConfigService config, IEloDatabaseService db)
    {
        _core = core;
        _config = config;
        _db = db;
    }

    public async Task OnClientConnectAsync(IPlayer player)
    {
        if (!_config.Config.EnableEloSystem || player.IsFakeClient || player.SteamID == 0) return;

        var steamId = player.SteamID;
        if (steamId == 0) return;

        var stats = await _db.GetPlayerStatsAsync(steamId);
        stats.ConnectTime = DateTime.UtcNow;
        stats.SessionKills = 0;
        stats.SessionDeaths = 0;
        _playerStats[steamId] = stats;

        // Apply immediately if already spawned
        _core.Scheduler.NextTick(() => ApplyCachedScore(player));
    }

    public async Task OnClientDisconnectAsync(IPlayer player)
    {
        if (!_config.Config.EnableEloSystem || player.IsFakeClient || player.SteamID == 0) return;

        var steamId = player.SteamID;
        if (steamId != 0 && _playerStats.TryGetValue(steamId, out var stats))
        {
            stats.TotalPlaytime += (int)(DateTime.UtcNow - stats.ConnectTime).TotalSeconds;
            stats.ConnectTime = DateTime.UtcNow; // prevent double counting if saved again
            await _db.SavePlayerStatsAsync(steamId, stats);
            _playerStats.TryRemove(steamId, out _);
        }
    }

    public void ApplyCachedScore(IPlayer player)
    {
        if (!_config.Config.EnableEloSystem) return;

        if (!player.IsFakeClient && player.SteamID != 0 && _playerStats.TryGetValue(player.SteamID, out var stats))
        {
            UpdatePlayerControllerScore(player, stats.Score);
        }
    }

    public void AwardKillScore(IPlayer attacker, bool isHeadshot)
    {
        if (!_config.Config.EnableEloSystem || attacker.IsFakeClient || attacker.SteamID == 0) return;

        var steamId = attacker.SteamID;
        if (steamId == 0 || !_playerStats.TryGetValue(steamId, out var stats)) return;

        var delta = isHeadshot ? _config.Config.EloOnHeadshot : _config.Config.EloOnKill;
        stats.Score += delta;
        if (stats.Score > stats.PeakScore) stats.PeakScore = stats.Score;
        
        stats.TotalKills++;
        stats.SessionKills++;
        
        UpdatePlayerControllerScore(attacker, stats.Score);
        _ = _db.SavePlayerStatsAsync(steamId, stats);
    }

    public void DeductDeathScore(IPlayer victim)
    {
        if (!_config.Config.EnableEloSystem || victim.IsFakeClient || victim.SteamID == 0) return;

        var steamId = victim.SteamID;
        if (steamId == 0 || !_playerStats.TryGetValue(steamId, out var stats)) return;

        stats.Score += _config.Config.EloOnDeath;
        if (stats.Score > stats.PeakScore) stats.PeakScore = stats.Score;
        
        stats.TotalDeaths++;
        stats.SessionDeaths++;
        
        UpdatePlayerControllerScore(victim, stats.Score);
        _ = _db.SavePlayerStatsAsync(steamId, stats);
    }

    private void UpdatePlayerControllerScore(IPlayer player, int newScore)
    {
        _core.Scheduler.NextTick(() => {
            if (player.Controller is not null)
            {
                player.Controller.Score = newScore;
            }
        });
    }

    public PlayerStats? GetStats(ulong steamId)
    {
        return _playerStats.TryGetValue(steamId, out var stats) ? stats : null;
    }
}
