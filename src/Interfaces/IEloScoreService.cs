using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IEloScoreService
{
    Task OnClientConnectAsync(IPlayer player);
    Task OnClientDisconnectAsync(IPlayer player);
    void AwardKillScore(IPlayer attacker, bool isHeadshot);
    void DeductDeathScore(IPlayer victim);
    void ApplyCachedScore(IPlayer player);
}
