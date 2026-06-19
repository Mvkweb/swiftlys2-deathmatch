using SwiftlyS2_Deathmatch.Models;

namespace SwiftlyS2_Deathmatch.Interfaces;

public interface IDeathmatchConfigService
{
    DeathmatchConfig Config { get; }
    void LoadOrCreate();
    void ApplyToConvars();
}
