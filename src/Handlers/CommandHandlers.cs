using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2_Deathmatch.Interfaces;

namespace SwiftlyS2_Deathmatch.Handlers;

public sealed class CommandHandlers
{
    private readonly IMapConfigService _mapConfig;
    private readonly ISpawnVisualizationService _spawnViz;
    private readonly ISwiftlyCore _core;
    private readonly List<Guid> _commandGuids = new();

    public CommandHandlers(IMapConfigService mapConfig, ISpawnVisualizationService spawnViz, ISwiftlyCore core)
    {
        _mapConfig = mapConfig;
        _spawnViz = spawnViz;
        _core = core;
    }

    public void Register()
    {
        _commandGuids.Add(_core.Command.RegisterCommand("editspawns", EditSpawns, permission: "root"));
        _commandGuids.Add(_core.Command.RegisterCommand("addspawn", AddSpawn, permission: "root"));
        _commandGuids.Add(_core.Command.RegisterCommand("remove", RemoveSpawn, permission: "root"));
        _commandGuids.Add(_core.Command.RegisterCommand("namespawn", NameSpawn, permission: "root"));
        _commandGuids.Add(_core.Command.RegisterCommand("gotospawn", GotoSpawn, permission: "root"));
        _commandGuids.Add(_core.Command.RegisterCommand("savespawns", SaveSpawns, permission: "root"));
        _commandGuids.Add(_core.Command.RegisterCommand("stopediting", StopEditing, permission: "root"));
    }

    public void Unregister()
    {
        foreach (var guid in _commandGuids)
        {
            _core.Command.UnregisterCommand(guid);
        }
        _commandGuids.Clear();
    }

    private void EditSpawns(ICommandContext context)
    {
        _spawnViz.ShowSpawns();
        _core.Engine.ExecuteCommand("mp_warmup_pausetimer 1");
        _core.Engine.ExecuteCommand("mp_warmuptime 999999");
        _core.Engine.ExecuteCommand("mp_warmup_start");
        context.Reply("Spawn editing mode enabled. Spawns are now visible and warmup started.");
    }

    private void AddSpawn(ICommandContext context)
    {
        if (!_spawnViz.IsVisible)
        {
            context.Reply("You must be in spawn edit mode. Use !editspawns first.");
            return;
        }

        if (context.Args.Length < 1)
        {
            context.Reply("Usage: !addspawn <T/CT>");
            return;
        }

        var teamStr = context.Args[0].ToLower();
        Team team;
        if (teamStr == "t") team = Team.T;
        else if (teamStr == "ct") team = Team.CT;
        else
        {
            context.Reply("Invalid team. Use T or CT.");
            return;
        }

        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to add a spawn.");
            return;
        }

        var pawn = player.PlayerPawn;
        if (pawn is null || !pawn.IsValid)
        {
             context.Reply("Player pawn not found.");
             return;
        }

        if (pawn.AbsOrigin is null || pawn.AbsRotation is null)
        {
            context.Reply("Could not determine player position or rotation.");
            return;
        }

        var id = _mapConfig.AddSpawn(pawn.AbsOrigin.Value, pawn.AbsRotation.Value, team);
        _spawnViz.ShowSpawns();
        context.Reply($"Added spawn {id} for team {team}.");
    }

    private void RemoveSpawn(ICommandContext context)
    {
        if (!_spawnViz.IsVisible)
        {
            context.Reply("You must be in spawn edit mode. Use !editspawns first.");
            return;
        }

        if (context.Args.Length < 1)
        {
            context.Reply("Usage: !remove <id>");
            return;
        }

        if (!int.TryParse(context.Args[0], out var id))
        {
            context.Reply("Invalid spawn ID.");
            return;
        }

        if (_mapConfig.RemoveSpawn(id))
        {
            _spawnViz.ShowSpawns();
            context.Reply($"Removed spawn {id}.");
        }
        else
        {
            context.Reply($"Spawn {id} not found.");
        }
    }

    private void NameSpawn(ICommandContext context)
    {
        if (!_spawnViz.IsVisible)
        {
            context.Reply("You must be in spawn edit mode. Use !editspawns first.");
            return;
        }

        if (context.Args.Length < 2)
        {
            context.Reply("Usage: !namespawn <id> <name>");
            return;
        }

        if (!int.TryParse(context.Args[0], out var id))
        {
            context.Reply("Invalid spawn ID.");
            return;
        }

        var name = context.Args[1];
        if (_mapConfig.NameSpawn(id, name))
        {
            context.Reply($"Named spawn {id} to '{name}'.");
        }
        else
        {
            context.Reply($"Spawn {id} not found.");
        }
    }

    private void GotoSpawn(ICommandContext context)
    {
        if (context.Args.Length < 1)
        {
            context.Reply("Usage: !gotospawn <id>");
            return;
        }

        if (!int.TryParse(context.Args[0], out var id))
        {
            context.Reply("Invalid spawn ID.");
            return;
        }

        var spawn = _mapConfig.Spawns.FirstOrDefault(s => s.Id == id);
        if (spawn is null)
        {
            context.Reply($"Spawn {id} not found.");
            return;
        }

        var player = context.Sender;
        if (player is null || !player.IsValid) return;

        player.Teleport(spawn.Position, spawn.Angle, Vector.Zero);
        context.Reply($"Teleported to spawn {id}.");
    }

    private void SaveSpawns(ICommandContext context)
    {
        if (string.IsNullOrEmpty(_mapConfig.LoadedMapName))
        {
             context.Reply("No map config loaded.");
             return;
        }
        _mapConfig.Save(_mapConfig.LoadedMapName);
        context.Reply("Spawns saved to map config file.");
    }

    private void StopEditing(ICommandContext context)
    {
        _spawnViz.HideSpawns();
        _core.Engine.ExecuteCommand("mp_warmup_pausetimer 0");
        _core.Engine.ExecuteCommand("mp_warmup_end");
        context.Reply("Spawn editing mode disabled. Beams hidden and warmup ended.");
    }
}
