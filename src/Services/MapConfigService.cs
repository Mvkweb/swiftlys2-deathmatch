using System.Text.Json;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Models;
using SwiftlyS2_Deathmatch.Logging;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class MapConfigService : IMapConfigService
{
    private readonly ISwiftlyCore _core;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private List<Spawn> _spawns = new();
    public List<Spawn> Spawns => _spawns;
    public string? LoadedMapName { get; private set; }

    public MapConfigService(ISwiftlyCore core)
    {
        _core = core;
    }

    public void Clear()
    {
        _spawns = new();
        LoadedMapName = null;
    }

    public void Load(string mapName)
    {
        Clear();
        if (string.IsNullOrWhiteSpace(mapName)) return;

        var mapPath = Path.Combine(_core.PluginPath, "resources", "maps", $"{mapName}.json");
        try
        {
            if (!File.Exists(mapPath))
            {
                _core.Logger.LogPluginWarning("Deathmatch: Map config not found: {Path}", mapPath);
                return;
            }

            var json = File.ReadAllText(mapPath);
            var config = JsonSerializer.Deserialize<MapConfig>(json, _jsonOptions);
            if (config is not null)
            {
                _spawns = config.Spawns;
                LoadedMapName = mapName;
                _core.Logger.LogPluginInformation("Deathmatch: Loaded {Count} spawns for map {Map}", _spawns.Count, mapName);
            }
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Deathmatch: Failed to load map config for {Map}", mapName);
        }
    }

    public void Save(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName)) return;

        var mapsDir = Path.Combine(_core.PluginPath, "resources", "maps");
        if (!Directory.Exists(mapsDir)) Directory.CreateDirectory(mapsDir);

        var mapPath = Path.Combine(mapsDir, $"{mapName}.json");
        try
        {
            var config = new MapConfig { Spawns = _spawns };
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(mapPath, json);
            _core.Logger.LogPluginInformation("Deathmatch: Saved {Count} spawns for map {Map}", _spawns.Count, mapName);
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Deathmatch: Failed to save map config for {Map}", mapName);
        }
    }

    public int AddSpawn(Vector position, QAngle angle, Team team)
    {
        var maxId = _spawns.Count > 0 ? _spawns.Max(s => s.Id) : 0;
        var newId = maxId + 1;

        var spawn = new Spawn
        {
            Id = newId,
            Vector = $"{position.X} {position.Y} {position.Z}",
            QAngle = $"{angle.Pitch} {angle.Yaw} {angle.Roll}",
            Team = team
        };

        _spawns.Add(spawn);
        return newId;
    }

    public bool RemoveSpawn(int id)
    {
        var spawn = _spawns.FirstOrDefault(s => s.Id == id);
        if (spawn is null) return false;
        _spawns.Remove(spawn);
        return true;
    }

    public bool NameSpawn(int id, string name)
    {
        var spawn = _spawns.FirstOrDefault(s => s.Id == id);
        if (spawn is null) return false;
        spawn.Name = name;
        return true;
    }
}
