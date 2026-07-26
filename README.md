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
- **Pick the base on the maximized map.** Click its icon, or put the map cursor
  over it and press your map `Select` bind (mouse or controller). Clicking the
  same base again turns guidance off.

  The game blanks every airbase icon the moment you are sitting in an aircraft
  — invisible and unclickable, because the native handler only serves spawn
  selection — so Cleared Ratte keeps them drawn and clickable while you fly.

Whichever way you pick, the chosen base is confirmed in the action report
along with the runway the tower assigned you, and highlighted on the map. The
runway is chosen with the same query the game uses for its own auto-landing,
so aircraft weight, landing speed, runway length and tailhook are all taken
into account — carriers included.

The approach picture appears immediately on selection, gear up or down, all the
way from the other side of the map if you want it.

## The approach picture

Instead of the game's single glideslope line, Cleared Ratte draws the approach
the way a modern HUD would. Every line is a clone of one of the game's own
overlay graphics, so it inherits the HUD material, canvas and scaling rather
than looking bolted on.

**Highway in the sky.** A tunnel of gates sitting on the 3.4° glidepath,
spaced geometrically so perspective spreads them evenly at any range, with
rails along the bottom corners and a heavier gate at the touchdown point. The
runway outline is drawn from any distance. On path the gates nest around your
velocity vector; off path the stack visibly skews, long before a needle would
move. Carrier decks are handled — gate positions lead the deck by its own
velocity, using the same aimpoint math the game uses.

**Deviation cluster.** Hung off the velocity vector: an ILS localizer scale
(±2.5°) and glidepath scale (±0.7°) with needles you fly toward, a virtual
PAPI that reads white-white-red-red on path, and a data block with slant
range, time to go, actual vs. required glidepath angle, course deviation, and
your speed against the reference speed the tower cleared you at.

Everything shifts green → amber → red with deviation.

## Declutter

The game ships an unfinished landing mode — `CombatHUD.landingMode` and
`HUDUnitMarker.SetLandingMode()` exist but nothing ever switches them on. The
only decluttering you actually get is unit markers hiding when the gear comes
down, which is why it feels like it happens at landing clearance, and why it
happens far too late to help you find the runway.

Cleared Ratte clears the combat clutter the moment you select a base, and puts
back exactly what it took away when you turn guidance off. By default that is
the unit markers, objective pointer and hit markers, the target designator,
off-screen target arrow and target label, and the floating airbase name and
range marker — the approach picture already shows you where the base is. The
weapon reticle, weapon status panel and threat list can be added, but are left
alone by default — being shot at on final is worth knowing about.

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

Settings are editable in-game with
[ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager).

`Approach Assist` — picking the base:

| Setting | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Select a friendly airbase for landing guidance at any range. |
| `AutoSelectOnGearDown` | `true` | Lowering the gear selects the nearest friendly airbase automatically. |
| `SelectKey` | `L` | Cycles friendly airbases nearest-first; one press past the last turns guidance off. Accepts `JoystickButton0`–`19` too. |

`Approach Display` — what gets drawn:

| Setting | Default | Description |
| --- | --- | --- |
| `HighwayInTheSky` | `true` | Fly-through gates down the glidepath, with runway outline and rails, replacing the native glideslope line. |
| `ApproachInstruments` | `true` | Localizer and glidepath scales, virtual PAPI and data block at the velocity vector. |
| `GlideslopeWithGearUp` | `true` | With `HighwayInTheSky` off: draw the native line on selection instead of only with the gear down. |

Turn both display options off to keep the stock glideslope line and use Cleared
Ratte purely for picking the base.

`Approach Declutter` — what gets cleared away while a base is selected:

| Setting | Default | Description |
| --- | --- | --- |
| `DeclutterOnSelect` | `true` | Master switch. Clears the HUD on selection instead of waiting for the gear-down hiding on short final. |
| `HideUnitMarkers` | `true` | HUD unit markers, objective pointer and hit markers. |
| `HideAirbaseLabel` | `true` | The floating airbase name and range marker for the selected base. |
| `HideTargetMarkers` | `true` | Target designator, off-screen target arrow and target label. |
| `HideWeaponUI` | `false` | The weapon reticle in the middle of the HUD. The weapon UI stops updating while hidden. |
| `HideWeaponStatus` | `false` | The weapon status panel in the top right corner. |
| `HideThreatList` | `false` | The threat list. |

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
