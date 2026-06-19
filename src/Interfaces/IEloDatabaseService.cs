using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IEloDatabaseService
{
    Task InitializeAsync();
    Task<int> GetPlayerScoreAsync(ulong steamId);
    Task SavePlayerScoreAsync(ulong steamId, int score);
}
