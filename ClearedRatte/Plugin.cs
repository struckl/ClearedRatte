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
    internal static ConfigEntry<KeyboardShortcut> SelectKey;

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
        GlideslopeWithGearUp = Config.Bind(
            "Approach Display", "GlideslopeWithGearUp", true,
            "With HighwayInTheSky off: draw the native glideslope line as soon as an airbase is selected, instead of only with the gear down.");
        SelectKey = Config.Bind(
            "Approach Assist", "SelectKey", new KeyboardShortcut(KeyCode.L),
            "Optional: cycles friendly airbases nearest-first; one press past the last turns guidance off. Also accepts JoystickButton0-19 for HOTAS/controller. Clicking an airbase icon on the maximized map works too.");
    }
}
