using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class EloScoreService : IEloScoreService
{
    private readonly ISwiftlyCore _core;
    private readonly IDeathmatchConfigService _config;
    private readonly IEloDatabaseService _db;
    
    private readonly ConcurrentDictionary<ulong, int> _playerScores = new();

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

        var score = await _db.GetPlayerScoreAsync(steamId);
        _playerScores[steamId] = score;

        // Apply immediately if already spawned
        _core.Scheduler.NextTick(() => ApplyCachedScore(player));
    }

    public async Task OnClientDisconnectAsync(IPlayer player)
    {
        if (!_config.Config.EnableEloSystem || player.IsFakeClient || player.SteamID == 0) return;

        var steamId = player.SteamID;
        if (steamId != 0 && _playerScores.TryGetValue(steamId, out var score))
        {
            await _db.SavePlayerScoreAsync(steamId, score);
            _playerScores.TryRemove(steamId, out _);
        }
    }

    public void ApplyCachedScore(IPlayer player)
    {
        if (!_config.Config.EnableEloSystem) return;

        if (!player.IsFakeClient && player.SteamID != 0 && _playerScores.TryGetValue(player.SteamID, out var score))
        {
            UpdatePlayerControllerScore(player, score);
        }
    }

    public void AwardKillScore(IPlayer attacker, bool isHeadshot)
    {
        if (!_config.Config.EnableEloSystem || attacker.IsFakeClient || attacker.SteamID == 0) return;

        var steamId = attacker.SteamID;
        if (steamId == 0) return;

        var currentScore = _playerScores.GetOrAdd(steamId, 0);
        var delta = isHeadshot ? _config.Config.EloOnHeadshot : _config.Config.EloOnKill;
        var newScore = currentScore + delta;
        
        _playerScores[steamId] = newScore;
        UpdatePlayerControllerScore(attacker, newScore);
    }

    public void DeductDeathScore(IPlayer victim)
    {
        if (!_config.Config.EnableEloSystem || victim.IsFakeClient || victim.SteamID == 0) return;

        var steamId = victim.SteamID;
        if (steamId == 0) return;

        var currentScore = _playerScores.GetOrAdd(steamId, 0);
        var newScore = currentScore + _config.Config.EloOnDeath;
        
        _playerScores[steamId] = newScore;
        UpdatePlayerControllerScore(victim, newScore);
    }

    private void UpdatePlayerControllerScore(IPlayer player, int newScore)
    {
        if (player.Controller is not null)
        {
            player.Controller.Score = newScore;
        }
    }
}
