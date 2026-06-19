using SwiftlyS2.Shared.Players;
using System.Collections.Generic;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IWeaponLoadoutService
{
    void SavePlayerLoadout(IPlayer player);
    void RestorePlayerLoadout(IPlayer player);
    void ClearPlayerLoadout(IPlayer player);
}
