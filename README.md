# SwiftlyS2-Deathmatch

A lightweight, high-performance Deathmatch plugin for Counter-Strike 2, built on the [SwiftlyS2](https://github.com/swiftlys2/swiftlys2) framework.

## Features

*   **Integrated Spawn Editor**: Create and manage map-specific spawn points in-real-time.
*   **Visual Feedback**: Beam-based visualization for spawns while editing.
*   **Warmup Integration**: Automatically toggles warmup mode when editing spawns.
*   **Damage Reports**: Detailed chat feedback on damage exchange (Gave/Took) upon death.
*   **Global Configuration**: Customize chat prefix, armor levels, and buy settings via `config.json`.
*   **Automatic Configuration**: Enforces standard Deathmatch cvars (Buy anywhere, infinite buy time, free armor).
*   **High Performance**: Minimal overhead, leveraging the SwiftlyS2 C# API.

## Commands

### Admin Commands

| Command | Description | Permission |
| :--- | :--- | :--- |
| `!editspawns` | Enter spawn editing mode and visualize current spawns. | `root` |
| `!addspawn <T/CT>` | Add a new spawn point at your current position. | `root` |
| `!remove <id>` | Remove a spawn point by its ID. | `root` |
| `!namespawn <id> <name>` | Assign a custom name to a specific spawn point. | `root` |
| `!gotospawn <id>` | Teleport to a specific spawn point. | `root` |
| `!savespawns` | Save all current spawns to the map configuration file. | `root` |
| `!stopediting` | Exit editing mode and hide visual beams. | `root` |

### Player Commands

| Command | Description |
| :--- | :--- |
| `!stats` | View your current Elo rating, K/D ratio, and playtime. |
| `!rs` | Reset your current session/total Elo statistics (preserves playtime). |
| `!hs` / `!headshot` | Toggle Headshot Only mode (damage dealt only applies on headshots). |
| `!ak` / `!ak47` | Immediately equip an AK-47. |
| `!m4a1` / `!m4a1s` / `!m4s` | Immediately equip an M4A1-S. |
| `!m4` / `!m4a4` | Immediately equip an M4A4. |
| `!awp` | Immediately equip an AWP. |
| `!deagle` | Immediately equip a Desert Eagle. |
| `!aug` | Immediately equip an AUG. |
| `!sg` | Immediately equip an SG 553. |
| `!famas` | Immediately equip a FAMAS. |
| `!galil` | Immediately equip a Galil AR. |
| `!mp9` | Immediately equip an MP9. |
| `!mac10` | Immediately equip a MAC-10. |
| `!mp5` | Immediately equip an MP5-SD. |
| `!glock` | Immediately equip a Glock-18. |
| `!usp` | Immediately equip a USP-S. |

## Installation

1. Ensure you have [SwiftlyS2](https://github.com/swiftlys2/swiftlys2) installed on your server.
2. Download the latest release from the [Releases](https://github.com/Mvk/SwiftlyS2-Deathmatch/releases) page.
3. Extract the contents into your `plugins/` directory.

## Database Configuration

For the Elo system and stats to save, you **must** configure a database connection in Swiftly's global database config.
Open your server's `addons/swiftly/configs/databases.jsonc` file and add the `"deathmatch_elo"` connection under `connections`:

```json
{
    "default_connection": "default",
    "connections": {
        "deathmatch_elo": "sqlite://deathmatch_elo.db"
    }
}
```

## Plugin Configuration

A `config.jsonc` file will automatically generate in `addons/swiftly/configs/plugins/Deathmatch/` when the plugin first runs.
The plugin also automatically enforces standard Deathmatch settings on map load:
*   `mp_buy_anywhere 1`
*   `mp_buytime 9999`
*   `mp_free_armor 2`

**Note:** The map-specific Spawn Editor features and commands (`!editspawns`, `!addspawn`, etc.) are currently **WIP (Work in Progress)**. Spawns will be stored in `addons/swiftly/plugins/Deathmatch/resources/maps/<mapname>.json`.

## Credits

*   **[K4ryuu](https://github.com/K4ryuu)**: Inspiration for the [GitHub Actions Release Workflow](https://github.com/K4ryuu/K4-Guilds-SwiftlyS2).
*   **[a2Labs](https://github.com/a2Labs-cc)**: Architecture inspiration from [SwiftlyS2-Retakes](https://github.com/a2Labs-cc/SwiftlyS2-Retakes).
*   **Author**: Mvk

## License

This project is licensed under the MIT License.
