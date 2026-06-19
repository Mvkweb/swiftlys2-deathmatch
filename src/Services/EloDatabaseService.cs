using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Logging;
using System;
using System.Data;
using System.IO;
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

        if (_config.Config.DatabaseType == "sqlite")
        {
            var path = Path.Combine(_core.PluginDataDirectory, "deathmatch_elo.db");
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }
        }

        try
        {
            using var connection = GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                // SqliteConnection OpenAsync doesn't exist on IDbConnection interface generically,
                // so we just cast if it's sqlite, or rely on Dapper to auto-open if we don't open.
                // Actually Dapper auto-opens the connection!
            }
            
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS deathmatch_elo (
                    steam_id BIGINT PRIMARY KEY,
                    score INT NOT NULL DEFAULT 0
                );");
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Failed to initialize Elo database table.");
        }
    }

    private IDbConnection GetConnection()
    {
        if (_config.Config.DatabaseType == "sqlite")
        {
            var path = Path.Combine(_core.PluginDataDirectory, "deathmatch_elo.db");
            return new SqliteConnection($"Data Source={path}");
        }
        else if (_config.Config.DatabaseType == "mysql")
        {
            return _core.Database.GetConnection(_config.Config.DatabaseConnection);
        }

        throw new InvalidOperationException($"Invalid database type: {_config.Config.DatabaseType}");
    }

    public async Task<int> GetPlayerScoreAsync(ulong steamId)
    {
        try
        {
            using var connection = GetConnection();
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
            using var connection = GetConnection();
            
            if (_config.Config.DatabaseType == "sqlite")
            {
                var query = @"
                    INSERT INTO deathmatch_elo (steam_id, score) 
                    VALUES (@SteamId, @Score)
                    ON CONFLICT(steam_id) DO UPDATE SET score = excluded.score;";
                await connection.ExecuteAsync(query, new { SteamId = steamId, Score = score });
            }
            else
            {
                var query = @"
                    INSERT INTO deathmatch_elo (steam_id, score) 
                    VALUES (@SteamId, @Score)
                    ON DUPLICATE KEY UPDATE score = VALUES(score);";
                await connection.ExecuteAsync(query, new { SteamId = steamId, Score = score });
            }
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Failed to save player score.");
        }
    }
}
