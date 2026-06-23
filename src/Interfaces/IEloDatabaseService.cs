using SwiftlyS2_Deathmatch.Models;
using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IEloDatabaseService
{
    Task InitializeAsync();
    Task<PlayerStats> GetPlayerStatsAsync(ulong steamId);
    Task SavePlayerStatsAsync(ulong steamId, PlayerStats stats);
}
