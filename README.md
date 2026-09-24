# ![Beyond Nations](./.github/media/banner.png)
Beyond Nations, previously called the Open Source Game project, started as a collaborative effort between Preponderous Software and the Fairfield Programming Association. It aims to create an open-source game that can serve as a reference for aspiring developers.

## Quick Start

The only prerequisite is the [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or newer. There is no editor to install, no account to create and no licence to obtain.

```bash
git clone https://github.com/Preponderous-Software/beyond-nations.git
cd beyond-nations
dotnet run --project src/BeyondNations.Desktop
```

That builds the game and opens it. To run the tests instead:

```bash
dotnet test
```

On Linux the game needs an OpenGL 3.3 capable driver and the usual X11 or Wayland libraries, both of which a desktop install already has.

For detailed contribution guidelines, see the [Contributing Guide](./docs/CONTRIBUTING.md). For how the code is laid out, see the [Architecture Note](./docs/ARCHITECTURE.md).

## Project Status & CI
[![CI](https://github.com/Preponderous-Software/beyond-nations/actions/workflows/ci.yml/badge.svg)](https://github.com/Preponderous-Software/beyond-nations/actions/workflows/ci.yml)

This project uses continuous integration to ensure code quality and stability. All pull requests are automatically tested. For more information about the CI pipeline and how to run checks locally, see the [CI/CD Documentation](./docs/CI-CD.md).

## Game Concept
In this game, you'll be able to embark on an adventure in a procedurally generated world filled with resources, settlements and pawns.

You'll be able to collect resources and use them to found settlements, build structures or craft items. You will have the option to join an existing nation or create your own. There will be differences between nations and they will be able to form relations with other nations, whether those be positive or negative. Interacting with the world will be a key activity of the game. Most entities will be interactable. Pawns will have different traits and will be able to interact with each other, the player & the world.

As a leader, you'll be able to manage your nation's relations with other nations, manage your nation's resources, and manage your nation's settlements. As a citizen, you'll be able to contribute to your nation's economy by collecting resources, crafting items and participating in trade.

## Game Features
The following features are planned for the game:
- Procedurally generated world
- Settlements
- Nations
- Pawns
- Resources
- Crafting
- Trade
- Relations
- Quests
- Story
- Combat
- Player customization
- Modding support


## Controls
The following controls are available in the game:
---
| Key | Action |
| --- | --- |
| `W` / `Up Arrow` | Move forward |
| `A` / `Left Arrow` | Turn left |
| `S` / `Down Arrow` | Move backward |
| `D` / `Right Arrow` | Turn right |
| `E` | Interact with nearest entity |
| `Space` | Jump |
| `Left Shift` | Sprint |
| `N` | Create a new nation |
| `J` | Join a random nation |
| `L` | Leave your current nation |
| `F` | Found a settlement |
| `P` | Plant a sapling |
| `H` | Teleport to home settlement |
| `B` | Build stall |
| `I` | Toggle the inventory display (shown by default) |
| `Insert` | Toggle auto-walk |
| `V` | Switch between the third-person and first-person view |
| `Page Up` | Increase render distance |
| `Page Down` | Decrease render distance |
| `Escape` | Pause / unpause; back out of a menu; quit from the main menu |
| `F1` | Toggle debug mode (and the debug menu) |
| `F2` | Generate nearby land *(debug mode only)* |
| `F3` | Spawn a new pawn *(debug mode only)* |
| `F4` | Spawn money *(debug mode only)* |
| `F5` | Spawn wood *(debug mode only)* |
| `F6` | Teleport all pawns to you *(debug mode only)* |
| `F12` | Take screenshot |

*(debug mode only)* keys do nothing until debug mode has been enabled with `F1`.

## Game Systems
A number of systems combine to create the gameplay experience. Some are already implemented and some are still planned; the [Systems Document](./docs/SYSTEMS.md) lists each one with its current status.

## Tech Stack
- [C#](https://docs.microsoft.com/en-us/dotnet/csharp/) on [.NET 8](https://dotnet.microsoft.com/): the whole game, simulation and host alike.
- [Silk.NET](https://github.com/dotnet/Silk.NET): windowing, OpenGL and input, maintained under the `dotnet` organisation.
- [Dear ImGui](https://github.com/ImGuiNET/ImGui.NET): the menus, the heads-up display and the info boxes.
- [FontStashSharp](https://github.com/FontStashSharp/FontStashSharp): the glyph atlas behind world-space nametags.
- [Git](https://git-scm.com/) and [GitHub](https://github.com/): version control, review and continuous integration.

The game does not use a commercial engine. Everything it draws is a coloured primitive generated in code, so the renderer, camera, input and user interface are written in the repository rather than supplied by a vendor. The reasoning is recorded in the [Engine Migration Rationale](./docs/ENGINE-MIGRATION-RATIONALE.md).

Third-party assets and their licences are listed in [THIRD-PARTY.md](./docs/THIRD-PARTY.md).

## Inspirations
### Medieval Factions
Medieval Factions is a Minecraft plugin that enables players to create their own nation, claim territory, and form relations with other nations. The plugin is limited because it is designed for a game that is not created for this kind of gameplay. The objective of our project is to develop a game specifically designed for this kind of gameplay.

### Mount & Blade: Warband
Mount & Blade: Warband is a medieval action role-playing game that features a sandbox gameplay style. The game allows players to create their own character and choose which faction to join. Players can also create their own faction and conquer territories. The game features a variety of gameplay elements such as combat, diplomacy, and trade. The game is a great source of inspiration for our project because it features many of the gameplay elements that we want to implement in our game.

### Minecraft
Minecraft is a sandbox video game that allows players to explore a procedurally generated world, gather resources, craft items, and build structures. The game features a variety of gameplay elements such as combat, exploration, and resource gathering. The game is a great source of inspiration for our project because it features many of the gameplay elements that we want to implement in our game.

## Usage reporting
Usage reporting is on by default: each time the game starts, Beyond Nations sends its name (`beyond-nations`) and its version in one `startup` event to [trace](https://github.com/Stephenson-Software/trace) at `https://trace.danielstephenson.dev`. Nothing about you, your machine, your IP address, your world or anything you do in the game is sent. The report goes out on a background thread, never holds up the game, and is dropped silently if the service cannot be reached.

The first run creates a `settings.json` in the game's data directory (`~/.local/share/BeyondNations/` on Linux, `%AppData%\BeyondNations\` on Windows) and prints a one-line notice naming it. To turn reporting off, any one of these is enough:

- `"usageReporting": {"enabled": false}` in that `settings.json`:

  ```json
  {
      "usageReporting": {
          "enabled": false
      }
  }
  ```

- the command-line switch `--no-usage-reporting`, for one run
- the environment variable `TRACE_USAGE_REPORTING=off` (also `false`, `0`, `no`), which turns off every program that reports to trace
- the environment variable `DO_NOT_TRACK=1` (see [consoledonottrack.com](https://consoledonottrack.com))

The environment variables win over everything else. When reporting is off, the game says so at startup with the reason. The defaults the game ships with, including the reporting key, are in `src/BeyondNations.Desktop/usage-reporting.json`.

Details: https://github.com/Stephenson-Software/trace#usage-reporting

## Contributing
To get started contributing to this project, please read the [Contributing Guide](./docs/CONTRIBUTING.md).

## Authors and acknowledgment
### Software Development
Name | Main Contributions
------------ | -------------
Daniel McCoy Stephenson | Creator
Pasarus | Participated in brainstorming sessions & participated in PR reviews

### Other
Name | Main Contributions
------------ | -------------
William McGonagle | Initially reached out to Preponderous Software to collaborate on a unity project, participated in brainstorming sessions
Phil Garner | Participated in brainstorming sessions & provided feedback on economic mechanics
Nathan Gates | Playtested the game & participated in brainstorming sessions
Ezekiel Martinez | Participated in brainstorming sessions
Sshinx | Contributed custom 3D models including trees, saplings & rocks

## 📄 License

This project is licensed under the **Preponderous Non-Commercial License (Preponderous-NC)**.  
It is free to use, modify, and self-host for **non-commercial** purposes, but **commercial use requires a separate license**.

> **Disclaimer:** *Preponderous Software is not a legal entity.*  
> All rights to works published under this license are reserved by the copyright holder, **Daniel McCoy Stephenson**.

Full license text:  
[https://github.com/Preponderous-Software/preponderous-nc-license/blob/main/LICENSE.md](https://github.com/Preponderous-Software/preponderous-nc-license/blob/main/LICENSE.md)
