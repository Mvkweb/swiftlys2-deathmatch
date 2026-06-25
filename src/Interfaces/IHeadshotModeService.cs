using System.Collections.Generic;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IHeadshotModeService
{
    bool ToggleHeadshotMode(ulong steamId);
    bool IsHeadshotModeEnabled(ulong steamId);
    void RemovePlayer(ulong steamId);
}
