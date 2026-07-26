# Cleared Ratte

Deliberate landing guidance for
[Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/).

The game only draws its landing glideslope when you happen to fly within 5 km
of a friendly base. Cleared Ratte lets you nominate the base you actually
intend to land at, and the game's own approach overlay then draws the
glideslope and runway markers to it from any distance.

Extracted from [MKMods](https://github.com/struckl/MKModsNO) into its own
plugin so you can run the landing guidance without the rest of that mod.

## How it works

Three ways to pick your landing base, whichever suits your setup:

- **Lower the gear.** Gear down away from the ground reads as intent to land,
  so the nearest friendly airbase with a usable runway is selected
  automatically. No keybind, no menu.
- **Press the select key** (default `L`). First press picks the nearest base,
  each further press cycles outwards, and one press past the last base turns
  guidance off again. Also accepts `JoystickButton0`–`19`, so it can live on
  a HOTAS.
- **Click an airbase icon on the maximized map.** Those clicks are normally
  dead once you are airborne (the native handler only serves spawn selection).

Whichever way you pick, the chosen base is confirmed in the action report
along with the runway the tower assigned you, and highlighted on the map. The
runway is chosen with the same query the game uses for its own auto-landing,
so aircraft weight, landing speed, runway length and tailhook are all taken
into account — carriers included.

Lower the gear and the native glideslope line appears, all the way from the
other side of the map if you want it.

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) into the
   Nuclear Option game folder and launch the game once so it generates its
   folders.
2. Copy the `ClearedRatte` folder from the release into
   `Nuclear Option/BepInEx/plugins/`, so you end up with:

   ```
   BepInEx/plugins/ClearedRatte/ClearedRatte.dll
   ```

3. Launch the game. Config is written to
   `BepInEx/config/dev.sewerlabs.clearedratte.cfg`.

## Configuration

All settings live under the `Approach Assist` section and are editable in-game
with [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager).

| Setting | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Select a friendly airbase for landing guidance at any range. |
| `AutoSelectOnGearDown` | `true` | Lowering the gear selects the nearest friendly airbase automatically. |
| `SelectKey` | `L` | Cycles friendly airbases nearest-first; one press past the last turns guidance off. Accepts `JoystickButton0`–`19` too. |

## Building

Requires the .NET SDK and a local Nuclear Option install (the game assemblies
are referenced directly).

```bash
dotnet build ClearedRatte.sln -c Release
```

The default game directory is `Y:\SteamLibrary\steamapps\common\Nuclear Option`.
Override it with:

```bash
dotnet build ClearedRatte.sln -c Release -p:GameDirectory="D:\Steam\steamapps\common\Nuclear Option"
```

A successful build deploys the DLL straight into
`<GameDirectory>/BepInEx/plugins/ClearedRatte`.

## Credits

Approach assist code by Lukas Struckl. Built on
[BepInEx](https://github.com/BepInEx/BepInEx) and
[HarmonyX](https://github.com/BepInEx/HarmonyX). Originally part of
[MKMods](https://github.com/struckl/MKModsNO), a fork of
[mkualquiera/nuclear_option_modding](https://github.com/mkualquiera/nuclear_option_modding).
