using System;

namespace SwiftlyS2_Deathmatch.Models;

public class PlayerStats
{
    // Database stored values
    public int Score { get; set; }
    public int PeakScore { get; set; }
    public int TotalKills { get; set; }
    public int TotalDeaths { get; set; }
    public int TotalPlaytime { get; set; } // in seconds

    // In-memory session tracking
    public int SessionKills { get; set; }
    public int SessionDeaths { get; set; }
    public DateTime ConnectTime { get; set; } = DateTime.UtcNow;

    public int CurrentPlaytime => TotalPlaytime + (int)(DateTime.UtcNow - ConnectTime).TotalSeconds;
}
