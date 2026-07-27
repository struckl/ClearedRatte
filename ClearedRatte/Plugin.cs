using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ClearedRatte;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "dev.sewerlabs.clearedratte";
    public const string Name = "Cleared Ratte";
    public const string Version = "1.0.1";

    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> AutoSelectOnGearDown;
    internal static ConfigEntry<bool> HighwayInTheSky;
    internal static ConfigEntry<bool> ApproachInstruments;
    internal static ConfigEntry<bool> GlideslopeWithGearUp;
    internal static ConfigEntry<float> DisplayScale;
    internal static ConfigEntry<int> TunnelGates;
    internal static ConfigEntry<KeyboardShortcut> SelectKey;
    internal static ConfigEntry<bool> DeclutterOnSelect;
    internal static ConfigEntry<bool> HideUnitMarkers;
    internal static ConfigEntry<bool> HideTargetMarkers;
    internal static ConfigEntry<bool> HideWeaponUI;
    internal static ConfigEntry<bool> HideWeaponStatus;
    internal static ConfigEntry<bool> HideThreatList;
    internal static ConfigEntry<bool> HideAirbaseLabel;

    private void Awake()
    {
        Logger = base.Logger;

        BindConfig();

        new Harmony(Guid).PatchAll();
        Logger.LogInfo($"{Name} {Version} loaded.");
    }

    private void Update()
    {
        ApproachAssist.Tick();
    }

    private void BindConfig()
    {
        Enabled = Config.Bind(
            "1. General", "Enable mod", true,
            "Master switch for the whole mod. On: you can select a friendly airbase "
            + "and get landing guidance -- a glidepath tunnel, ILS-style landing "
            + "instruments and a decluttered HUD (each set up in the sections below). "
            + "Off: the mod does nothing.");

        SelectKey = Config.Bind(
            "2. Airbase selection", "Select airbase key", new KeyboardShortcut(KeyCode.L),
            "Press to cycle through friendly airbases, nearest first; one press past "
            + "the last one turns guidance off again. You can also click an airbase "
            + "icon on the maximized map instead. For a HOTAS or controller, set the "
            + "value to JoystickButton0 through JoystickButton19.");
        AutoSelectOnGearDown = Config.Bind(
            "2. Airbase selection", "Auto-select on gear down", true,
            "Lowering the landing gear automatically selects the nearest friendly "
            + "airbase, so you get guidance without pressing anything.");

        HighwayInTheSky = Config.Bind(
            "3. Approach display", "Glidepath tunnel", true,
            "Show the approach as a series of gates you fly through down to the "
            + "runway ('highway in the sky'), plus a runway outline. Off: only the "
            + "game's normal single glideslope line is shown.");
        ApproachInstruments = Config.Bind(
            "3. Approach display", "Landing instruments", true,
            "Show ILS-style scales at the velocity vector: left/right of the runway "
            + "centerline, above/below the glidepath, plus approach lights (a virtual "
            + "PAPI) and a small block with the numbers.");
        DisplayScale = Config.Bind(
            "3. Approach display", "Instrument size", 1f,
            new ConfigDescription(
                "Size of the landing instruments. 1 = normal; smaller values are more "
                + "compact and further out of the way.",
                new AcceptableValueRange<float>(0.4f, 2f)));
        TunnelGates = Config.Bind(
            "3. Approach display", "Tunnel gate count", 9,
            new ConfigDescription(
                "How many gates the glidepath tunnel is built from. Fewer = cleaner "
                + "picture, more = the tunnel reaches further out from the runway.",
                new AcceptableValueRange<int>(3, 14)));
        GlideslopeWithGearUp = Config.Bind(
            "3. Approach display", "Glideslope line with gear up", true,
            "Only matters when 'Glidepath tunnel' is off: show the game's glideslope "
            + "line as soon as an airbase is selected, instead of only once the gear "
            + "is down.");

        DeclutterOnSelect = Config.Bind(
            "4. HUD declutter", "Declutter during approach", true,
            "Hide combat clutter from the HUD while landing guidance is active. What "
            + "exactly gets hidden is chosen below; everything comes back the moment "
            + "guidance is turned off. Off: this mod never hides anything (the game "
            + "still does its own gear-down hiding on short final).");
        HideUnitMarkers = Config.Bind(
            "4. HUD declutter", "Hide unit markers", true,
            "Hide the markers over friendly and enemy units, the objective pointer "
            + "and hit markers.");
        HideAirbaseLabel = Config.Bind(
            "4. HUD declutter", "Hide airbase label", true,
            "Hide the floating name and distance tag on the selected airbase. The "
            + "approach display already shows you where the runway is.");
        HideTargetMarkers = Config.Bind(
            "4. HUD declutter", "Hide target markers", true,
            "Hide the target designator box, the arrow pointing to an off-screen "
            + "target, and the target's label.");
        HideWeaponUI = Config.Bind(
            "4. HUD declutter", "Hide weapon reticle", false,
            "Hide the aiming reticle in the middle of the HUD. Note: the reticle "
            + "stops updating while it is hidden.");
        HideWeaponStatus = Config.Bind(
            "4. HUD declutter", "Hide weapon status panel", false,
            "Hide the weapon status panel in the top right corner.");
        HideThreatList = Config.Bind(
            "4. HUD declutter", "Hide threat list", false,
            "Hide the list of threats. Off by default: knowing you are being shot at "
            + "on final is usually worth the clutter.");
    }
}
