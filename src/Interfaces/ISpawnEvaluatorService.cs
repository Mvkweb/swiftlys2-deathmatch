using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Models;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface ISpawnEvaluatorService
{
    Spawn? GetBestSpawn(IPlayer player, Team team);
}
