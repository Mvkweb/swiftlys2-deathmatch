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
    private const string ConfigFileName = "config.json";
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
            _core.Configuration.InitializeJsonWithModel<DeathmatchConfig>(ConfigFileName, SectionName);
            
            var configPath = _core.Configuration.GetConfigPath(ConfigFileName);
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(SectionName, out var section))
                {
                    var config = JsonSerializer.Deserialize<DeathmatchConfig>(section.GetRawText(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (config is not null)
                    {
                        Config = config;
                    }
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
        _core.Engine.ExecuteCommand($"mp_free_armor {Config.FreeArmor}");
        
        // Force Deathmatch rules
        _core.Engine.ExecuteCommand("mp_respawn_on_death_ct 1");
        _core.Engine.ExecuteCommand("mp_respawn_on_death_t 1");
        _core.Engine.ExecuteCommand("mp_ignore_round_win_conditions 1");
        _core.Engine.ExecuteCommand("mp_halftime 0");
        _core.Engine.ExecuteCommand("mp_match_can_clinch 0");
        _core.Engine.ExecuteCommand("mp_roundtime 9999");
        _core.Engine.ExecuteCommand("mp_maxrounds 9999");
        _core.Engine.ExecuteCommand("mp_warmuptime 0");

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

        // Try to disable native chat point spam
        _core.Engine.ExecuteCommand("mp_dm_kill_base_score 0");

        // Disable native healthshots on 3-kill streak
        _core.Engine.ExecuteCommand("mp_tdm_healthshot_killcount 9999");
        _core.Engine.ExecuteCommand("mp_death_drop_healthshot 0");

        // Disable chickens natively
        _core.Engine.ExecuteCommand("sv_disable_chickens 1");

        // Ensure warmup is completely killed
        _core.Engine.ExecuteCommand("mp_warmup_end");
    }
}
