# Cleared Ratte | Nuclear Option

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
into account.

If the tower would refuse you — an arrestor deck and no tailhook, or a strip
the game considers too short — you still get the approach picture, and the
report says so: *guidance only, tower will not clear you*. You asked to be
shown the approach; whether it clears you is its business.

Carrier decks are handled throughout. The deck is led by its own velocity, so
gates, needles, range and time-to-go all point at where the deck will be when
you arrive rather than where it is now — which is most of a mile out of date
from a long final. Sunk carriers drop out of the cycle.

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
PAPI that reads white-white-red-red on path, and two lines of numbers — slant
range and time to go, then speed and how far off the reference speed you are.
Deviation is deliberately not written out: the needles and the PAPI already
say it, and saying it twice is what makes a HUD read as a debug overlay.

Everything shifts green → amber → red with deviation. Instrument lines are
snapped to whole device pixels so hairlines stay hairlines; world geometry is
left unsnapped, because quantising something that moves every frame trades a
little softness for shimmer. Gates fade with distance and the touchdown frame
stays bright, so the corridor recedes into the aim point instead of every line
competing at the same weight.

`DisplayScale` sizes the cluster and its text; `TunnelGates` sets how many
gates the tunnel is built from.

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

`1. General`:

| Setting | Default | Description |
| --- | --- | --- |
| `Enable mod` | `true` | Master switch for the whole mod. |

`2. Airbase selection` — picking the base:

| Setting | Default | Description |
| --- | --- | --- |
| `Select airbase key` | `L` | Cycles friendly airbases nearest-first; one press past the last turns guidance off. Accepts `JoystickButton0`–`19` too. |
| `Auto-select on gear down` | `true` | Lowering the gear selects the nearest friendly airbase automatically. |

`3. Approach display` — what gets drawn:

| Setting | Default | Description |
| --- | --- | --- |
| `Glidepath tunnel` | `true` | Fly-through gates down the glidepath, with runway outline and rails, replacing the native glideslope line. |
| `Landing instruments` | `true` | Localizer and glidepath scales, virtual PAPI and data block at the velocity vector. |
| `Instrument size` | `1.0` | Size of the landing instruments, `0.4`–`2.0`. Below 1 is tighter and further out of the way. |
| `Tunnel gate count` | `9` | How many gates the tunnel is built from, `3`–`14`. Fewer is cleaner, more reaches further out. |
| `Glideslope line with gear up` | `true` | With `Glidepath tunnel` off: draw the native line on selection instead of only with the gear down. |

Turn both display options off to keep the stock glideslope line and use Cleared
Ratte purely for picking the base.

`4. HUD declutter` — what gets cleared away while a base is selected:

| Setting | Default | Description |
| --- | --- | --- |
| `Declutter during approach` | `true` | Master switch. Clears the HUD on selection instead of waiting for the gear-down hiding on short final. |
| `Hide unit markers` | `true` | HUD unit markers, objective pointer and hit markers. |
| `Hide airbase label` | `true` | The floating airbase name and range marker for the selected base. |
| `Hide target markers` | `true` | Target designator, off-screen target arrow and target label. |
| `Hide weapon reticle` | `false` | The weapon reticle in the middle of the HUD. The weapon UI stops updating while hidden. |
| `Hide weapon status panel` | `false` | The weapon status panel in the top right corner. |
| `Hide threat list` | `false` | The threat list. |

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
