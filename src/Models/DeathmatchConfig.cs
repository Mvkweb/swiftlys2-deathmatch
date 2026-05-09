namespace SwiftlyS2_Deathmatch.Models;

public sealed class DeathmatchConfig
{
    public bool EnableBuyAnywhere { get; set; } = true;
    public int BuyTime { get; set; } = 9999;
    public int FreeArmor { get; set; } = 2; // 0=none, 1=Kevlar, 2=Kevlar+Helmet
    public string ChatPrefix { get; set; } = "{green}Deathmatch{default}";
    public bool EnableDamageReports { get; set; } = true;
}
