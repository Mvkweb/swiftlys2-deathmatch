using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SwiftlyS2.Shared;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Models;
using SwiftlyS2_Deathmatch.Logging;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class DeathmatchConfigService : IDeathmatchConfigService
{
    private readonly ISwiftlyCore _core;
    private const string ConfigFileName = "config.jsonc";
    private const string SectionName = "deathmatch";

    public DeathmatchConfig Config { get; private set; } = new();

    public DeathmatchConfigService(ISwiftlyCore core)
    {
        _core = core;
    }

    public void LoadOrCreate()
    {
        try
        {
            var configPath = _core.Configuration.GetConfigPath(ConfigFileName);
            var defaultJsonc = @"// SwiftlyS2 Deathmatch Configuration
{
  ""deathmatch"": {
    // General Settings
    ""enableBuyAnywhere"": true,
    ""buyTime"": 600000,
    ""freeArmor"": 1,
    ""chatPrefix"": ""[green]Deathmatch[default]"",
    ""enableDamageReports"": true,

    // Health & Ammo
    ""giveMediShotOnKill"": false,
    ""removeMediShots"": true,
    ""healthOnKill"": 20,
    ""maxHealth"": 100,
    ""refillAmmoOnKill"": true,
    ""onlyShowPlayerKillfeed"": true,

    // Bot Loadout System
    ""enableBotsLoadout"": true,
    // What weapons should T Bots get?
    ""tBotsWeapons"": [
      ""weapon_ak47""
    ],
    // What weapons should CT Bots get?
    ""ctBotsWeapons"": [
      ""weapon_m4a1_silencer""
    ],

    // Elo System
    ""enableEloSystem"": true,
    ""eloOnKill"": 2,
    ""eloOnHeadshot"": 3,
    ""eloOnDeath"": -2,
    
    // Database connection string name from database.jsonc
    ""databaseType"": ""sqlite"",
    ""databaseConnection"": ""deathmatch_elo""
  }
}";

            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, defaultJsonc);
            }

            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions 
            { 
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                AllowTrailingCommas = true
            };

            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (doc.RootElement.TryGetProperty(SectionName, out var section))
            {
                var config = JsonSerializer.Deserialize<DeathmatchConfig>(section.GetRawText(), options);
                if (config is not null)
                {
                    Config = config;
                }

                // Check if any fields are missing by comparing reserialized string length roughly
                // Or rather, we just won't rewrite unless a new update explicitly requires it. 
                // We'll reserialize and check if the loaded JSON is missing keys that the default has.
                var defaultDoc = JsonDocument.Parse(defaultJsonc, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
                var defaultSection = defaultDoc.RootElement.GetProperty(SectionName);
                
                bool needsUpdate = false;
                foreach (var prop in defaultSection.EnumerateObject())
                {
                    if (!section.TryGetProperty(prop.Name, out _))
                    {
                        needsUpdate = true;
                        break;
                    }
                }

                if (needsUpdate)
                {
                    var newJson = JsonSerializer.Serialize(new { deathmatch = Config }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    File.WriteAllText(configPath, newJson);
                }
            }
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Deathmatch: Failed to load config.");
        }
    }

    public void ApplyToConvars()
    {
        // Force Game Mode to Deathmatch
        _core.Engine.ExecuteCommand("game_type 1");
        _core.Engine.ExecuteCommand("game_mode 2");
        _core.Engine.ExecuteCommand("exec gamemode_deathmatch");

        _core.Engine.ExecuteCommand($"mp_buy_anywhere {(Config.EnableBuyAnywhere ? 1 : 0)}");
        _core.Engine.ExecuteCommand($"mp_buytime {Config.BuyTime}");
        _core.Engine.ExecuteCommand("mp_buy_during_immunity 0");
        _core.Engine.ExecuteCommand($"mp_free_armor {Config.FreeArmor}");
        
        // Force Deathmatch rules
        _core.Engine.ExecuteCommand("mp_respawn_on_death_ct 1");
        _core.Engine.ExecuteCommand("mp_respawn_on_death_t 1");
        _core.Engine.ExecuteCommand("mp_ignore_round_win_conditions 1");
        _core.Engine.ExecuteCommand("mp_halftime 0");
        _core.Engine.ExecuteCommand("mp_match_can_clinch 0");
        _core.Engine.ExecuteCommand("mp_roundtime 60");
        _core.Engine.ExecuteCommand("mp_roundtime_defuse 60");
        _core.Engine.ExecuteCommand("mp_roundtime_hostage 60");
        _core.Engine.ExecuteCommand("mp_timelimit 60");
        _core.Engine.ExecuteCommand("mp_maxrounds 9999");
        _core.Engine.ExecuteCommand("mp_freezetime 0");

        // Fix Buy Menu in Deathmatch
        _core.Engine.ExecuteCommand("sv_buy_status_override 0");
        _core.Engine.ExecuteCommand("mp_buy_allow_guns 255");
        
        // Disable Deathmatch drops and random weapons
        _core.Engine.ExecuteCommand("mp_death_drop_gun 0");
        _core.Engine.ExecuteCommand("mp_death_drop_defuser 0");
        _core.Engine.ExecuteCommand("mp_death_drop_grenade 0");
        _core.Engine.ExecuteCommand("mp_drop_knife_enable 0");

        // Clean up UI & Annoying elements
        _core.Engine.ExecuteCommand("sv_disable_radar 1");
        _core.Engine.ExecuteCommand("mp_display_kill_assists 0");
        _core.Engine.ExecuteCommand("mp_dm_bonus_length_max 0");
        _core.Engine.ExecuteCommand("mp_dm_bonus_length_min 0");
        _core.Engine.ExecuteCommand("mp_dm_bonus_percent 0");
        _core.Engine.ExecuteCommand("sv_gameinstructor_disable 1");

        // Disable native healthshots on 3-kill streak
        _core.Engine.ExecuteCommand("mp_tdm_healthshot_killcount 9999");
        _core.Engine.ExecuteCommand("mp_death_drop_healthshot 0");

        // Restore default deathmatch points so scoreboard works
        _core.Engine.ExecuteCommand("mp_dm_kill_base_score 10");

        // Disable chickens natively
        _core.Engine.ExecuteCommand("sv_disable_chickens 1");

        // Ensure warmup is completely killed
        _core.Engine.ExecuteCommand("mp_warmup_end");
    }
}
