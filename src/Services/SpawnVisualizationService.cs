using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Models;
using SwiftlyS2_Deathmatch.Logging;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class SpawnVisualizationService : ISpawnVisualizationService
{
    private readonly ISwiftlyCore _core;
    private readonly IMapConfigService _mapConfig;
    private readonly List<uint> _beamEntityIndices = new();

    public bool IsVisible { get; private set; }

    public SpawnVisualizationService(ISwiftlyCore core, IMapConfigService mapConfig)
    {
        _core = core;
        _mapConfig = mapConfig;
    }

    public void ShowSpawns()
    {
        HideSpawns();
        foreach (var spawn in _mapConfig.Spawns)
        {
            CreateBeam(spawn);
        }
        IsVisible = true;
    }

    public void HideSpawns()
    {
        foreach (var idx in _beamEntityIndices)
        {
            var beam = _core.EntitySystem.GetEntityByIndex<CBeam>(idx);
            if (beam is not null && beam.IsValid)
            {
                beam.Despawn();
            }
        }
        _beamEntityIndices.Clear();
        IsVisible = false;
    }

    private void CreateBeam(Spawn spawn)
    {
        var start = spawn.Position;
        var color = spawn.Team == Team.CT
            ? new Color(0, 128, 255, 255)
            : new Color(255, 140, 0, 255);

        try
        {
            var beam = _core.EntitySystem.CreateEntityByDesignerName<CBeam>("beam");
            if (beam is null) return;

            beam.StartFrame = 0;
            beam.FrameRate = 0;
            beam.LifeState = 1;
            beam.Width = 5.0f;
            beam.EndWidth = 5.0f;
            beam.Amplitude = 0;
            beam.Speed = 50;
            beam.BeamFlags = 0;
            beam.BeamType = BeamType_t.BEAM_HOSE;
            beam.FadeLength = 10.0f;
            beam.Render = color;
            beam.TurnedOff = false;

            beam.EndPos.X = start.X;
            beam.EndPos.Y = start.Y;
            beam.EndPos.Z = start.Z + 100.0f;

            beam.Teleport(start, new QAngle(0, 0, 0), Vector.Zero);
            beam.DispatchSpawn();

            beam.LifeStateUpdated();
            beam.StartFrameUpdated();
            beam.FrameRateUpdated();
            beam.WidthUpdated();
            beam.EndWidthUpdated();
            beam.AmplitudeUpdated();
            beam.SpeedUpdated();
            beam.BeamFlagsUpdated();
            beam.BeamTypeUpdated();
            beam.FadeLengthUpdated();
            beam.TurnedOffUpdated();
            beam.EndPosUpdated();
            beam.RenderUpdated();

            _beamEntityIndices.Add(beam.Index);
        }
        catch (Exception ex)
        {
            _core.Logger.LogPluginError(ex, "Deathmatch: Failed to create beam for spawn {Id}", spawn.Id);
        }
    }
}
