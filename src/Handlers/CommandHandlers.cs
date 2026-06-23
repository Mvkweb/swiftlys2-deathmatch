using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2_Deathmatch.Interfaces;

namespace SwiftlyS2_Deathmatch.Handlers;

public sealed class CommandHandlers
{
    private readonly IMapConfigService _mapConfig;
    private readonly ISpawnVisualizationService _spawnViz;
    private readonly ISwiftlyCore _core;
    private readonly IEloScoreService _eloScoreService;
    private readonly List<Guid> _commandGuids = new();
    private Guid _clientCmdHook;

    public CommandHandlers(IMapConfigService mapConfig, ISpawnVisualizationService spawnViz, ISwiftlyCore core, IEloScoreService eloScoreService)
    {
        _mapConfig = mapConfig;
        _spawnViz = spawnViz;
        _core = core;
        _eloScoreService = eloScoreService;
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
        _commandGuids.Add(_core.Command.RegisterCommand("stats", ShowStats));

        _clientCmdHook = _core.Command.HookClientCommand(OnClientCommand);
    }

    public void Unregister()
    {
        foreach (var guid in _commandGuids)
        {
            _core.Command.UnregisterCommand(guid);
        }
        _commandGuids.Clear();

        if (_clientCmdHook != Guid.Empty)
        {
            _core.Command.UnhookClientCommand(_clientCmdHook);
            _clientCmdHook = Guid.Empty;
        }
    }

    private void EditSpawns(ICommandContext context)
    {
        _spawnViz.ShowSpawns();
        _core.Engine.ExecuteCommand("sv_cheats 1");
        _core.Engine.ExecuteCommand("bot_zombie 1");
        _core.Engine.ExecuteCommand("bot_stop 1");
        context.Reply("Spawn editing mode enabled. Spawns are now visible and bots are frozen.");
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
        _core.Engine.ExecuteCommand("sv_cheats 0");
        _core.Engine.ExecuteCommand("bot_zombie 0");
        _core.Engine.ExecuteCommand("bot_stop 0");
        context.Reply("Spawn editing mode disabled. Beams hidden and bots are active.");
    }

    private void ShowStats(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid) return;

        var stats = _eloScoreService.GetStats(player.SteamID);
        if (stats is null)
        {
            context.Reply("[grey]• [white]No stats available yet. Start playing to earn Elo!");
            return;
        }

        double kdr = stats.TotalDeaths > 0 ? (double)stats.TotalKills / stats.TotalDeaths : stats.TotalKills;
        int hours = stats.CurrentPlaytime / 3600;
        int minutes = (stats.CurrentPlaytime % 3600) / 60;

        string sessionKills = stats.SessionKills > 0 ? $"[[green]+{stats.SessionKills}[white]]" : "[[grey]0[white]]";
        string sessionDeaths = stats.SessionDeaths > 0 ? $"[[lightred]-{stats.SessionDeaths}[white]]" : "[[grey]0[white]]";

        player.SendMessage(MessageType.Chat, " ");
        player.SendMessage(MessageType.Chat, "[grey]• [gold]Your Statistics:");
        player.SendMessage(MessageType.Chat, $"[grey]• [white]Elo Rating: [gold]{stats.Score} [white](Peak: [gold]{stats.PeakScore}[white])");
        player.SendMessage(MessageType.Chat, $"[grey]• [white]Kills: [green]{stats.TotalKills} [white]{sessionKills}");
        player.SendMessage(MessageType.Chat, $"[grey]• [white]Deaths: [lightred]{stats.TotalDeaths} [white]{sessionDeaths}");
        player.SendMessage(MessageType.Chat, $"[grey]• [white]KDR: [green]{kdr:F2}");
        player.SendMessage(MessageType.Chat, $"[grey]• [white]Playtime: [magenta]{hours}h {minutes}m");
        player.SendMessage(MessageType.Chat, " ");
    }

    public HookResult OnClientCommand(int playerId, string commandLine)
    {
        if (commandLine.Trim().StartsWith("drop", StringComparison.OrdinalIgnoreCase))
        {
            var player = _core.PlayerManager.GetPlayer(playerId);
            if (player != null && player.IsValid)
            {
                player.SendMessage(SwiftlyS2.Shared.Players.MessageType.Chat, "[green][Deathmatch][white] Weapon drops are disabled!");
                player.SendMessage(SwiftlyS2.Shared.Players.MessageType.Chat, "[green]Available Commands:[white] !guns, !settings (Placeholder)");
            }
            return HookResult.Stop;
        }
        return HookResult.Continue;
    }
}
