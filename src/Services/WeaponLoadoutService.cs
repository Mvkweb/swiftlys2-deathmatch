using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class WeaponLoadoutService : IWeaponLoadoutService
{
    private readonly ISwiftlyCore _core;
    // Maps SteamID/Slot to a list of weapon DesignerNames
    private readonly Dictionary<ulong, List<string>> _savedLoadouts = new();

    public WeaponLoadoutService(ISwiftlyCore core)
    {
        _core = core;
    }

    private ulong GetPlayerKey(IPlayer player)
    {
        return player.SteamID != 0 ? player.SteamID : (0xF000000000000000UL | (uint)player.Slot);
    }

    public void SavePlayerLoadout(IPlayer player)
    {
        if (player is null || !player.IsValid || player.PlayerPawn is null) return;
        
        var weaponServices = player.PlayerPawn.WeaponServices;
        if (weaponServices is null) return;

        var key = GetPlayerKey(player);
        var weaponsToSave = new List<string>();

        foreach (var handle in weaponServices.MyWeapons)
        {
            var wpn = handle.Value;
            if (wpn is not null && !string.IsNullOrEmpty(wpn.DesignerName))
            {
                // Ignore knife, c4, healthshots, and generic items that shouldn't be restocked implicitly
                var name = wpn.DesignerName.ToLower();
                if (name == "weapon_knife" || name == "weapon_c4" || name == "weapon_healthshot" || name == "weapon_taser")
                {
                    continue;
                }
                
                // Grenades are optional, but usually deathmatch doesn't restock grenades unless bought.
                // If they bought it and died with it, they get it back.
                weaponsToSave.Add(wpn.DesignerName);
            }
        }

        _savedLoadouts[key] = weaponsToSave;
    }

    public void RestorePlayerLoadout(IPlayer player)
    {
        if (player is null || !player.IsValid || player.PlayerPawn is null) return;

        var key = GetPlayerKey(player);
        if (!_savedLoadouts.TryGetValue(key, out var savedWeapons) || savedWeapons.Count == 0) return;

        var itemServices = player.PlayerPawn.ItemServices;
        if (itemServices is null) return;

        // Remove the default weapons they just spawned with (e.g., glock/usp)
        var weaponServices = player.PlayerPawn.WeaponServices;
        if (weaponServices is not null)
        {
            // We iterate safely by copying to list
            foreach (var handle in weaponServices.MyWeapons.ToList())
            {
                var wpn = handle.Value;
                if (wpn is not null && !string.IsNullOrEmpty(wpn.DesignerName))
                {
                    var name = wpn.DesignerName.ToLower();
                    if (name != "weapon_knife" && name != "weapon_c4")
                    {
                        weaponServices.RemoveWeapon(wpn);
                    }
                }
            }
        }

        // Give them their saved loadout
        foreach (var wpnName in savedWeapons)
        {
            itemServices.GiveItem(wpnName);
        }
    }

    public void ClearPlayerLoadout(IPlayer player)
    {
        if (player is null) return;
        _savedLoadouts.Remove(GetPlayerKey(player));
    }
}
