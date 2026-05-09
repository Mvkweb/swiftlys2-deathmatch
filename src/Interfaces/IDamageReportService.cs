using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IDamageReportService
{
    HookResult OnPlayerHurt(EventPlayerHurt @event);
    HookResult OnPlayerDeath(EventPlayerDeath @event);
}
