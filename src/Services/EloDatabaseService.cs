using Dapper;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Logging;
using System;
using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class EloDatabaseService : IEloDatabaseService
{
    private readonly ISwiftlyCore _core;
    private readonly IDeathmatchConfigService _config;

    public EloDatabaseService(ISwiftlyCore core, IDeathmatchConfigService config)
    {
        _core = core;
        _config = config;
    }

    public async Task InitializeAsync()
    {
        if (!_config.Config.EnableEloSystem) return;

        try
        {
            using var connection = _core.Database.GetConnection(_config.Config.DatabaseConnection);
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS deathmatch_elo (
                    steam_id INTEGER PRIMARY KEY,
                    score INTEGER NOT NULL DEFAULT 0
                );");
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Failed to initialize Elo database table.");
        }
    }

    public async Task<int> GetPlayerScoreAsync(ulong steamId)
    {
        try
        {
            using var connection = _core.Database.GetConnection(_config.Config.DatabaseConnection);
            var query = "SELECT score FROM deathmatch_elo WHERE steam_id = @SteamId";
            var result = await connection.QueryFirstOrDefaultAsync<int?>(query, new { SteamId = steamId });
            return result ?? 0;
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, $"Failed to get Elo score for {steamId}.");
            return 0;
        }
    }

    public async Task SavePlayerScoreAsync(ulong steamId, int score)
    {
        try
        {
            using var connection = _core.Database.GetConnection(_config.Config.DatabaseConnection);
            var query = @"
                INSERT INTO deathmatch_elo (steam_id, score) 
                VALUES (@SteamId, @Score)
                ON CONFLICT(steam_id) DO UPDATE SET score = excluded.score;";
            
            // Check if it's MySQL instead of SQLite. SQLite uses ON CONFLICT.
            // Wait, SQLite supports ON CONFLICT since version 3.24 (2018).
            // We'll use a safer approach for MySQL compatibility if needed later, but user requested SQLite.
            // If MySQL is used later, they might use INSERT ... ON DUPLICATE KEY UPDATE.
            // For now we assume SQLite.
            await connection.ExecuteAsync(query, new { SteamId = steamId, Score = score });
        }
        catch (Exception)
        {
            // Fallback for MySQL if SQLite syntax fails
            try 
            {
                using var connection = _core.Database.GetConnection(_config.Config.DatabaseConnection);
                var fallbackQuery = @"
                    INSERT INTO deathmatch_elo (steam_id, score) 
                    VALUES (@SteamId, @Score)
                    ON DUPLICATE KEY UPDATE score = VALUES(score);";
                await connection.ExecuteAsync(fallbackQuery, new { SteamId = steamId, Score = score });
            }
            catch (Exception)
            {
                // Optionally log fallback exception
            }
        }
    }
}
