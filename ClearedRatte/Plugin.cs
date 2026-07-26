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
    public const string Version = "1.0.0";

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
            "Approach Assist", "Enabled", true,
            "Select a friendly airbase for landing guidance at any range.");
        AutoSelectOnGearDown = Config.Bind(
            "Approach Assist", "AutoSelectOnGearDown", true,
            "Lowering the gear automatically selects the nearest friendly airbase (no keybind needed).");
        HighwayInTheSky = Config.Bind(
            "Approach Display", "HighwayInTheSky", true,
            "Fly-through gates down the glidepath, with the runway outline and rails, instead of the single native glideslope line.");
        ApproachInstruments = Config.Bind(
            "Approach Display", "ApproachInstruments", true,
            "ILS-style localizer and glidepath scales, a virtual PAPI and a data block, drawn at the velocity vector.");
        DisplayScale = Config.Bind(
            "Approach Display", "DisplayScale", 1f,
            new ConfigDescription(
                "Size of the deviation cluster and its readout. Below 1 is tighter and further out of the way.",
                new AcceptableValueRange<float>(0.4f, 2f)));
        TunnelGates = Config.Bind(
            "Approach Display", "TunnelGates", 9,
            new ConfigDescription(
                "How many gates the tunnel is built from. Fewer is cleaner, more reaches further out.",
                new AcceptableValueRange<int>(3, 14)));
        GlideslopeWithGearUp = Config.Bind(
            "Approach Display", "GlideslopeWithGearUp", true,
            "With HighwayInTheSky off: draw the native glideslope line as soon as an airbase is selected, instead of only with the gear down.");
        SelectKey = Config.Bind(
            "Approach Assist", "SelectKey", new KeyboardShortcut(KeyCode.L),
            "Optional: cycles friendly airbases nearest-first; one press past the last turns guidance off. Also accepts JoystickButton0-19 for HOTAS/controller. Clicking an airbase icon on the maximized map works too.");

        DeclutterOnSelect = Config.Bind(
            "Approach Declutter", "DeclutterOnSelect", true,
            "Clear combat clutter off the HUD as soon as an airbase is selected, instead of waiting for the gear-down hiding on short final. Everything comes back when guidance is turned off.");
        HideUnitMarkers = Config.Bind(
            "Approach Declutter", "HideUnitMarkers", true,
            "Hide the HUD unit markers, objective pointer and hit markers.");
        HideAirbaseLabel = Config.Bind(
            "Approach Declutter", "HideAirbaseLabel", true,
            "Hide the floating airbase name and range marker for the selected base. The approach picture already shows you where it is.");
        HideTargetMarkers = Config.Bind(
            "Approach Declutter", "HideTargetMarkers", true,
            "Hide the target designator, off-screen target arrow and target label.");
        HideWeaponUI = Config.Bind(
            "Approach Declutter", "HideWeaponUI", false,
            "Hide the weapon reticle in the middle of the HUD. Note that the weapon UI stops updating while it is hidden.");
        HideWeaponStatus = Config.Bind(
            "Approach Declutter", "HideWeaponStatus", false,
            "Hide the weapon status panel in the top right corner.");
        HideThreatList = Config.Bind(
            "Approach Declutter", "HideThreatList", false,
            "Hide the threat list. Off by default: being shot at on final is worth knowing about.");
    }
}
