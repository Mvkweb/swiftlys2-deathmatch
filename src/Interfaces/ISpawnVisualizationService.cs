namespace SwiftlyS2_Deathmatch.Interfaces;

public interface ISpawnVisualizationService
{
    void ShowSpawns();
    void HideSpawns();
    bool IsVisible { get; }
}
