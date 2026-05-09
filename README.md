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

| Command | Description | Permission |
| :--- | :--- | :--- |
| `!editspawns` | Enter spawn editing mode and visualize current spawns. | `root` |
| `!addspawn <T/CT>` | Add a new spawn point at your current position. | `root` |
| `!remove <id>` | Remove a spawn point by its ID. | `root` |
| `!namespawn <id> <name>` | Assign a custom name to a specific spawn point. | `root` |
| `!gotospawn <id>` | Teleport to a specific spawn point. | `root` |
| `!savespawns` | Save all current spawns to the map configuration file. | `root` |
| `!stopediting` | Exit editing mode and hide visual beams. | `root` |

## Installation

1. Ensure you have [SwiftlyS2](https://github.com/swiftlys2/swiftlys2) installed on your server.
2. Download the latest release from the [Releases](https://github.com/Mvk/SwiftlyS2-Deathmatch/releases) page.
3. Extract the contents into your `plugins/` directory.
4. Spawns are stored in `resources/maps/<mapname>.json`.

## Configuration

The plugin automatically applies the following settings on map load:
*   `mp_buy_anywhere 1`
*   `mp_buytime 9999`
*   `mp_free_armor 2`

## Credits

*   **[K4ryuu](https://github.com/K4ryuu)**: Inspiration for the [GitHub Actions Release Workflow](https://github.com/K4ryuu/K4-Guilds-SwiftlyS2).
*   **[a2Labs](https://github.com/a2Labs-cc)**: Architecture inspiration from [SwiftlyS2-Retakes](https://github.com/a2Labs-cc/SwiftlyS2-Retakes).
*   **Author**: Mvk

## License

This project is licensed under the MIT License.
