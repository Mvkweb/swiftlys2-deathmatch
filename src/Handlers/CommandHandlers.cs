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
    private readonly IHeadshotModeService _headshotMode;
    private readonly List<Guid> _commandGuids = new();
    private Guid _clientCmdHook;

    public CommandHandlers(IMapConfigService mapConfig, ISpawnVisualizationService spawnViz, ISwiftlyCore core, IEloScoreService eloScoreService, IHeadshotModeService headshotMode)
    {
        _mapConfig = mapConfig;
        _spawnViz = spawnViz;
        _core = core;
        _eloScoreService = eloScoreService;
        _headshotMode = headshotMode;
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
        _commandGuids.Add(_core.Command.RegisterCommand("rs", ResetStats));
        _commandGuids.Add(_core.Command.RegisterCommand("hs", ToggleHeadshotMode));
        _commandGuids.Add(_core.Command.RegisterCommand("headshot", ToggleHeadshotMode));

        // Weapon Allocators
        RegisterWeaponCommand("ak", "weapon_ak47");
        RegisterWeaponCommand("ak47", "weapon_ak47");
        RegisterWeaponCommand("m4", "weapon_m4a1");
        RegisterWeaponCommand("m4a4", "weapon_m4a1");
        RegisterWeaponCommand("m4a1", "weapon_m4a1_silencer");
        RegisterWeaponCommand("m4a1s", "weapon_m4a1_silencer");
        RegisterWeaponCommand("m4s", "weapon_m4a1_silencer");
        RegisterWeaponCommand("awp", "weapon_awp");
        RegisterWeaponCommand("deagle", "weapon_deagle");
        RegisterWeaponCommand("aug", "weapon_aug");
        RegisterWeaponCommand("sg", "weapon_sg556");
        RegisterWeaponCommand("famas", "weapon_famas");
        RegisterWeaponCommand("galil", "weapon_galilar");
        RegisterWeaponCommand("mp9", "weapon_mp9");
        RegisterWeaponCommand("mac10", "weapon_mac10");
        RegisterWeaponCommand("mp5", "weapon_mp5sd");
        RegisterWeaponCommand("glock", "weapon_glock");
        RegisterWeaponCommand("usp", "weapon_usp_silencer");

        _clientCmdHook = _core.Command.HookClientCommand(OnClientCommand);
    }

    private void RegisterWeaponCommand(string commandName, string weaponDesignerName)
    {
        _commandGuids.Add(_core.Command.RegisterCommand(commandName, (ctx) => GiveWeapon(ctx, weaponDesignerName)));
    }

    private void GiveWeapon(ICommandContext context, string weaponDesignerName)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid) return;

        var pawn = player.PlayerPawn;
        if (pawn is null || !pawn.IsValid) return;

        var itemServices = pawn.ItemServices;
        if (itemServices is null) return;
        
        var weaponServices = pawn.WeaponServices;
        if (weaponServices is null) return;

        bool isRequestingSecondary = IsSecondaryWeapon(weaponDesignerName);

        foreach (var handle in weaponServices.MyWeapons.ToList())
        {
            var wpn = handle.Value;
            if (wpn is not null && !string.IsNullOrEmpty(wpn.DesignerName))
            {
                var name = wpn.DesignerName.ToLower();
                if (name == "weapon_knife" || name == "weapon_c4" || name == "weapon_taser" || name == "weapon_healthshot" || name.Contains("grenade") || name.Contains("flashbang") || name.Contains("molotov") || name.Contains("decoy"))
                    continue;

                bool isExistingSecondary = IsSecondaryWeapon(name);
                
                if (isRequestingSecondary == isExistingSecondary)
                {
                    weaponServices.RemoveWeapon(wpn);
                }
            }
        }

        // Give the item, the persistency engine will automatically save it upon death
        itemServices.GiveItem(weaponDesignerName);
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        player.SendMessage(MessageType.Chat, localizer["dm.equipped", weaponDesignerName.Replace("weapon_", "")]);
    }

    private bool IsSecondaryWeapon(string weaponName)
    {
        var secondaries = new HashSet<string> { "weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_glock", "weapon_tec9", "weapon_hkp2000", "weapon_usp_silencer", "weapon_p250", "weapon_cz75a", "weapon_revolver" };
        return secondaries.Contains(weaponName.ToLower());
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
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        _spawnViz.ShowSpawns();
        _core.Engine.ExecuteCommand("sv_cheats 1");
        _core.Engine.ExecuteCommand("bot_zombie 1");
        _core.Engine.ExecuteCommand("bot_stop 1");
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        context.Reply(localizer["dm.edit_mode_enabled"]);
    }

    private void AddSpawn(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        if (!_spawnViz.IsVisible)
        {
            context.Reply(localizer["dm.edit_require_mode"]);
            return;
        }

        if (context.Args.Length < 1)
        {
            context.Reply(localizer["dm.edit_add_usage"]);
            return;
        }

        var teamStr = context.Args[0].ToLower();
        Team team;
        if (teamStr == "t") team = Team.T;
        else if (teamStr == "ct") team = Team.CT;
        else
        {
            context.Reply(localizer["dm.edit_add_invalid_team"]);
            return;
        }

        var pawn = player.PlayerPawn;
        if (pawn is null || !pawn.IsValid)
        {
             context.Reply(localizer["dm.edit_pawn_not_found"]);
             return;
        }

        if (pawn.AbsOrigin is null || pawn.AbsRotation is null)
        {
            context.Reply(localizer["dm.edit_pos_not_found"]);
            return;
        }

        var id = _mapConfig.AddSpawn(pawn.AbsOrigin.Value, pawn.AbsRotation.Value, team);
        _spawnViz.ShowSpawns();
        context.Reply(localizer["dm.edit_add_success", id, team]);
    }

    private void RemoveSpawn(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        if (!_spawnViz.IsVisible)
        {
            context.Reply(localizer["dm.edit_require_mode"]);
            return;
        }

        if (context.Args.Length < 1)
        {
            context.Reply(localizer["dm.edit_remove_usage"]);
            return;
        }

        if (!int.TryParse(context.Args[0], out var id))
        {
            context.Reply(localizer["dm.edit_invalid_id"]);
            return;
        }

        if (_mapConfig.RemoveSpawn(id))
        {
            _spawnViz.ShowSpawns();
            context.Reply(localizer["dm.edit_remove_success", id]);
        }
        else
        {
            context.Reply(localizer["dm.edit_spawn_not_found", id]);
        }
    }

    private void NameSpawn(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        if (!_spawnViz.IsVisible)
        {
            context.Reply(localizer["dm.edit_require_mode"]);
            return;
        }

        if (context.Args.Length < 2)
        {
            context.Reply(localizer["dm.edit_name_usage"]);
            return;
        }

        if (!int.TryParse(context.Args[0], out var id))
        {
            context.Reply(localizer["dm.edit_invalid_id"]);
            return;
        }

        var name = context.Args[1];
        if (_mapConfig.NameSpawn(id, name))
        {
            context.Reply(localizer["dm.edit_name_success", id, name]);
        }
        else
        {
            context.Reply(localizer["dm.edit_spawn_not_found", id]);
        }
    }

    private void GotoSpawn(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        if (context.Args.Length < 1)
        {
            context.Reply(localizer["dm.edit_goto_usage"]);
            return;
        }

        if (!int.TryParse(context.Args[0], out var id))
        {
            context.Reply(localizer["dm.edit_invalid_id"]);
            return;
        }

        var spawn = _mapConfig.Spawns.FirstOrDefault(s => s.Id == id);
        if (spawn is null)
        {
            context.Reply(localizer["dm.edit_spawn_not_found", id]);
            return;
        }

        player.Teleport(spawn.Position, spawn.Angle, Vector.Zero);
        context.Reply(localizer["dm.edit_goto_success", id]);
    }

    private void SaveSpawns(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        if (string.IsNullOrEmpty(_mapConfig.LoadedMapName))
        {
             context.Reply(localizer["dm.edit_save_no_map"]);
             return;
        }
        _mapConfig.Save(_mapConfig.LoadedMapName);
        context.Reply(localizer["dm.edit_save_success"]);
    }

    private void StopEditing(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid)
        {
            context.Reply("You must be a player to execute this command.");
            return;
        }

        _spawnViz.HideSpawns();
        _core.Engine.ExecuteCommand("sv_cheats 0");
        _core.Engine.ExecuteCommand("bot_zombie 0");
        _core.Engine.ExecuteCommand("bot_stop 0");
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        context.Reply(localizer["dm.edit_mode_disabled"]);
    }

    private void ShowStats(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid) return;

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var stats = _eloScoreService.GetStats(player.SteamID);
        if (stats is null)
        {
            context.Reply(localizer["dm.stats_not_available"]);
            return;
        }

        double kdr = stats.TotalDeaths > 0 ? (double)stats.TotalKills / stats.TotalDeaths : stats.TotalKills;
        int hours = stats.CurrentPlaytime / 3600;
        int minutes = (stats.CurrentPlaytime % 3600) / 60;

        string sessionKills = stats.SessionKills > 0 ? $"[[green]+{stats.SessionKills}[white]]" : "[[grey]0[white]]";
        string sessionDeaths = stats.SessionDeaths > 0 ? $"[[lightred]-{stats.SessionDeaths}[white]]" : "[[grey]0[white]]";

        player.SendMessage(MessageType.Chat, " ");
        player.SendMessage(MessageType.Chat, localizer["dm.stats_title"]);
        player.SendMessage(MessageType.Chat, localizer["dm.stats_elo", stats.Score, stats.PeakScore]);
        player.SendMessage(MessageType.Chat, localizer["dm.stats_kills", stats.TotalKills, sessionKills]);
        player.SendMessage(MessageType.Chat, localizer["dm.stats_deaths", stats.TotalDeaths, sessionDeaths]);
        player.SendMessage(MessageType.Chat, localizer["dm.stats_kdr", kdr.ToString("F2")]);
        player.SendMessage(MessageType.Chat, localizer["dm.stats_playtime", hours, minutes]);
        player.SendMessage(MessageType.Chat, " ");
    }

    private void ResetStats(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid) return;

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        _ = _eloScoreService.ResetStatsAsync(player.SteamID);
        context.Reply(localizer["dm.stats_reset"]);
    }

    private void ToggleHeadshotMode(ICommandContext context)
    {
        var player = context.Sender;
        if (player is null || !player.IsValid) return;

        bool isEnabled = _headshotMode.ToggleHeadshotMode(player.SteamID);
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        if (isEnabled)
        {
            player.SendMessage(MessageType.Chat, " ");
            player.SendMessage(MessageType.Chat, localizer["dm.hs_enabled"]);
            player.SendMessage(MessageType.Chat, localizer["dm.hs_enabled_desc"]);
            player.SendMessage(MessageType.Chat, " ");
        }
        else
        {
            player.SendMessage(MessageType.Chat, " ");
            player.SendMessage(MessageType.Chat, localizer["dm.hs_disabled"]);
            player.SendMessage(MessageType.Chat, localizer["dm.hs_disabled_desc"]);
            player.SendMessage(MessageType.Chat, " ");
        }
    }

    public HookResult OnClientCommand(int playerId, string commandLine)
    {
        if (commandLine.Trim().StartsWith("drop", StringComparison.OrdinalIgnoreCase))
        {
            var player = _core.PlayerManager.GetPlayer(playerId);
            if (player != null && player.IsValid)
            {
                var localizer = _core.Translation.GetPlayerLocalizer(player);
                player.SendMessage(SwiftlyS2.Shared.Players.MessageType.Chat, localizer["dm.drops_disabled"]);
                player.SendMessage(SwiftlyS2.Shared.Players.MessageType.Chat, localizer["dm.available_commands"]);
            }
            return HookResult.Stop;
        }
        return HookResult.Continue;
    }
}
