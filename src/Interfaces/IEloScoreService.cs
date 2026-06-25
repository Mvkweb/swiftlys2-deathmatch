using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Models;
using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IEloScoreService
{
    Task OnClientConnectAsync(IPlayer player);
    Task OnClientDisconnectAsync(IPlayer player);
    void AwardKillScore(IPlayer attacker, bool isHeadshot);
    void DeductDeathScore(IPlayer victim);
    void ApplyCachedScore(IPlayer player);
    PlayerStats? GetStats(ulong steamId);
    Task ResetStatsAsync(ulong steamId);
}
