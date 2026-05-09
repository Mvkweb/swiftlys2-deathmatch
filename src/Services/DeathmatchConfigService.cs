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
        _core.Engine.ExecuteCommand($"mp_buy_anywhere {(Config.EnableBuyAnywhere ? 1 : 0)}");
        _core.Engine.ExecuteCommand($"mp_buytime {Config.BuyTime}");
        _core.Engine.ExecuteCommand($"mp_buy_during_radio_chat_time {Config.BuyTime}");
        _core.Engine.ExecuteCommand($"mp_free_armor {Config.FreeArmor}");
    }
}
