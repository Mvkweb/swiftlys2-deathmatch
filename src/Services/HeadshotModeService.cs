using SwiftlyS2_Deathmatch.Interfaces;
using System.Collections.Generic;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class HeadshotModeService : IHeadshotModeService
{
    private readonly HashSet<ulong> _headshotModePlayers = new();

    public bool ToggleHeadshotMode(ulong steamId)
    {
        if (_headshotModePlayers.Contains(steamId))
        {
            _headshotModePlayers.Remove(steamId);
            return false;
        }
        
        _headshotModePlayers.Add(steamId);
        return true;
    }

    public bool IsHeadshotModeEnabled(ulong steamId)
    {
        return _headshotModePlayers.Contains(steamId);
    }

    public void RemovePlayer(ulong steamId)
    {
        _headshotModePlayers.Remove(steamId);
    }
}
