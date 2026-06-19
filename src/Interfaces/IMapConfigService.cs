using SwiftlyS2_Deathmatch.Models;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IMapConfigService
{
    List<Spawn> Spawns { get; }
    string? LoadedMapName { get; }
    void Load(string mapName);
    void Save(string mapName);
    int AddSpawn(Vector position, QAngle angle, Team team);
    bool RemoveSpawn(int id);
    bool NameSpawn(int id, string name);
    void Clear();
}
