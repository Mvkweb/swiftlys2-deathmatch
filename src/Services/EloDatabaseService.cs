using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Logging;
using SwiftlyS2_Deathmatch.Models;
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

            // Safely try to add new columns if they don't exist (SQLite will error if they do, so we catch)
            var columns = new[] {
                "kills INT NOT NULL DEFAULT 0",
                "deaths INT NOT NULL DEFAULT 0",
                "playtime INT NOT NULL DEFAULT 0",
                "peak_score INT NOT NULL DEFAULT 0"
            };

            foreach (var col in columns)
            {
                try
                {
                    var colName = col.Split(' ')[0];
                    await connection.ExecuteAsync($"ALTER TABLE deathmatch_elo ADD COLUMN {col};");
                    if (colName == "peak_score")
                    {
                        // Initialize peak_score to current score for existing players
                        await connection.ExecuteAsync("UPDATE deathmatch_elo SET peak_score = score WHERE peak_score = 0;");
                    }
                }
                catch { } // Ignore if column already exists
            }
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
            var dbName = _config.Config.DatabaseConnection.Replace("sqlite://", "");
            if (!dbName.EndsWith(".db")) dbName += ".db";
            
            var path = Path.Combine(_core.PluginDataDirectory, dbName);
            return new SqliteConnection($"Data Source={path}");
        }
        
        return _core.Database.GetConnection(_config.Config.DatabaseConnection);
    }

    public async Task<PlayerStats> GetPlayerStatsAsync(ulong steamId)
    {
        try
        {
            using var connection = GetConnection();
            var query = "SELECT score as Score, peak_score as PeakScore, kills as TotalKills, deaths as TotalDeaths, playtime as TotalPlaytime FROM deathmatch_elo WHERE steam_id = @SteamId";
            var result = await connection.QueryFirstOrDefaultAsync<PlayerStats>(query, new { SteamId = steamId });
            return result ?? new PlayerStats { Score = 0, PeakScore = 0, TotalKills = 0, TotalDeaths = 0, TotalPlaytime = 0 };
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, $"Failed to get Elo stats for {steamId}.");
            return new PlayerStats { Score = 0, PeakScore = 0, TotalKills = 0, TotalDeaths = 0, TotalPlaytime = 0 };
        }
    }

    public async Task SavePlayerStatsAsync(ulong steamId, PlayerStats stats)
    {
        try
        {
            using var connection = GetConnection();
            
            if (_config.Config.DatabaseType == "sqlite")
            {
                var query = @"
                    INSERT INTO deathmatch_elo (steam_id, score, peak_score, kills, deaths, playtime) 
                    VALUES (@SteamId, @Score, @PeakScore, @Kills, @Deaths, @Playtime)
                    ON CONFLICT(steam_id) DO UPDATE SET 
                        score = excluded.score,
                        peak_score = excluded.peak_score,
                        kills = excluded.kills,
                        deaths = excluded.deaths,
                        playtime = excluded.playtime;";
                await connection.ExecuteAsync(query, new { 
                    SteamId = steamId, 
                    Score = stats.Score,
                    PeakScore = stats.PeakScore,
                    Kills = stats.TotalKills,
                    Deaths = stats.TotalDeaths,
                    Playtime = stats.CurrentPlaytime
                });
            }
            else
            {
                var query = @"
                    INSERT INTO deathmatch_elo (steam_id, score, peak_score, kills, deaths, playtime) 
                    VALUES (@SteamId, @Score, @PeakScore, @Kills, @Deaths, @Playtime)
                    ON DUPLICATE KEY UPDATE 
                        score = VALUES(score),
                        peak_score = VALUES(peak_score),
                        kills = VALUES(kills),
                        deaths = VALUES(deaths),
                        playtime = VALUES(playtime);";
                await connection.ExecuteAsync(query, new { 
                    SteamId = steamId, 
                    Score = stats.Score,
                    PeakScore = stats.PeakScore,
                    Kills = stats.TotalKills,
                    Deaths = stats.TotalDeaths,
                    Playtime = stats.CurrentPlaytime
                });
            }
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Failed to save player stats.");
        }
    }
}
