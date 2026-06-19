namespace SwiftlyS2_Deathmatch.Models;

public sealed class DeathmatchConfig
{
    public bool EnableBuyAnywhere { get; set; } = true;
    public int BuyTime { get; set; } = 600000;
    public int FreeArmor { get; set; } = 2; // 0=none, 1=Kevlar, 2=Kevlar+Helmet
    public string ChatPrefix { get; set; } = "[green]Deathmatch[default]";
    public bool EnableDamageReports { get; set; } = true;
    public bool GiveMediShotOnKill { get; set; } = false;
    public bool RemoveMediShots { get; set; } = true;
    public bool OnlyShowPlayerKillfeed { get; set; } = false;

    public int HealthOnKill { get; set; } = 20;
    public int MaxHealth { get; set; } = 100;
    public bool RefillAmmoOnKill { get; set; } = true;

    public bool EnableEloSystem { get; set; } = true;
    public int EloOnKill { get; set; } = 2;
    public int EloOnHeadshot { get; set; } = 3;
    public int EloOnDeath { get; set; } = -2;
    public string DatabaseType { get; set; } = "sqlite";
    public string DatabaseConnection { get; set; } = "deathmatch_elo";
}
