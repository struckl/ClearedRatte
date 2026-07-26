using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ClearedRatte;

/// <summary>
/// Approach assist: pick a friendly airbase — click its map icon, press the
/// map's Select bind over it, or cycle with the keybind — and the game's native
/// AirbaseOverlay draws the landing glideslope for it at any range, gear up or
/// down, instead of only inside the 5 km auto-trigger radius.
/// </summary>
internal static class ApproachAssist
{
    private static Aircraft aircraft;
    private static Airbase chosenAirbase;

    // A "Select" bind on the left mouse button reaches us twice in one frame
    // (icon click + the map's own select pass); collapse those into one action.
    private static Airbase lastRequest;
    private static int lastRequestFrame = -1;

    public static Airbase ChosenAirbase => chosenAirbase;
    public static Aircraft Aircraft => aircraft;

    public static void SetAircraft(Aircraft newAircraft)
    {
        if (aircraft != null)
            aircraft.onSetGear -= OnSetGear;
        aircraft = newAircraft;
        chosenAirbase = null;
        if (aircraft != null)
            aircraft.onSetGear += OnSetGear;
    }

    /// <summary>Gear down anywhere = intent to land: pick the nearest base, no button needed.</summary>
    private static void OnSetGear(Aircraft.OnSetGear e)
    {
        if (!Plugin.Enabled.Value || !Plugin.AutoSelectOnGearDown.Value)
            return;
        if (e.gearState != LandingGear.GearState.Extending
            && e.gearState != LandingGear.GearState.LockedExtended)
            return;
        if (chosenAirbase != null || !HasLiveAircraft() || aircraft.NetworkHQ == null
            || aircraft.radarAlt < 5f)
            return;

        List<Airbase> bases = GetFriendlyLandingBases();
        if (bases.Count > 0)
            Select(bases[0]);
    }

    public static void Tick()
    {
        if (!Plugin.Enabled.Value)
            return;

        // The game blanks the airbase icons the moment you are in an aircraft,
        // which is exactly when we need them: keep them drawn and clickable.
        MapAccess.KeepAirbaseIconsUsable(HasLiveAircraft());

        if (!Plugin.SelectKey.Value.IsDown())
            return;
        if (aircraft == null || aircraft.disabled || aircraft.NetworkHQ == null)
            return;
        CycleAirbase();
    }

    /// <summary>Key press: nearest base first, then cycle outwards, then off.</summary>
    private static void CycleAirbase()
    {
        List<Airbase> bases = GetFriendlyLandingBases();
        if (bases.Count == 0)
        {
            Clear(report: false);
            Report("No friendly airbase with a runway available");
            return;
        }

        int next = chosenAirbase != null ? bases.IndexOf(chosenAirbase) + 1 : 0;
        if (next >= bases.Count)
        {
            Clear();
            return;
        }

        Select(bases[next]);
    }

    /// <summary>Map input: same base again turns guidance off, anything else switches to it.</summary>
    public static void RequestSelect(Airbase airbase)
    {
        if (airbase == null)
            return;
        if (lastRequest == airbase && lastRequestFrame == Time.frameCount)
            return;
        lastRequest = airbase;
        lastRequestFrame = Time.frameCount;

        if (chosenAirbase == airbase)
            Clear();
        else
            Select(airbase);
    }

    public static void Select(Airbase airbase)
    {
        Airbase previous = chosenAirbase;
        chosenAirbase = airbase;

        string runway = "";
        Airbase.Runway.RunwayUsage? usage = RequestLanding(airbase);
        if (usage.HasValue)
            runway = ", runway " + usage.Value.GetName();
        Report($"Approach: {airbase.SavedAirbase.DisplayName}{runway}");

        MapAccess.HighlightAirbase(previous, airbase);
        // Don't wait for the overlay's 2 s pass — show the new glideslope now.
        ApproachAssistOverlayPatch.ApplyNow();
    }

    public static void Clear(bool report = true)
    {
        Airbase previous = chosenAirbase;
        chosenAirbase = null;
        MapAccess.HighlightAirbase(previous, null);
        if (report)
            Report("Approach guidance off");
    }

    private static List<Airbase> GetFriendlyLandingBases()
    {
        var bases = new List<Airbase>();
        foreach (Airbase airbase in aircraft.NetworkHQ.GetAirbases())
        {
            if (airbase != null && airbase.GetLandingRunway() != null)
                bases.Add(airbase);
        }
        Vector3 position = aircraft.transform.position;
        bases.Sort((a, b) =>
            (a.center.position - position).sqrMagnitude.CompareTo(
                (b.center.position - position).sqrMagnitude));
        return bases;
    }

    /// <summary>Same runway query the native overlay builds for auto-landing.</summary>
    public static Airbase.Runway.RunwayUsage? RequestLanding(Airbase airbase)
    {
        AircraftParameters parameters = aircraft.GetAircraftParameters();
        float landingSpeed = Mathf.Sqrt(
            aircraft.GetMass() / aircraft.definition.aircraftInfo.maxWeight) * parameters.takeoffSpeed;
        RunwayQuery query = new RunwayQuery
        {
            RunwayType = RunwayQueryType.Any,
            MinSize = parameters.takeoffDistance,
            TailHook = aircraft.weaponManager.HasTailHook(),
            LandingSpeed = landingSpeed,
        };
        return airbase.RequestLanding(aircraft, query);
    }

    public static bool HasLiveAircraft()
    {
        return aircraft != null && !aircraft.disabled;
    }

    public static bool IsReadyToForce()
    {
        return Plugin.Enabled.Value && chosenAirbase != null
            && HasLiveAircraft() && aircraft.pilots.Length > 0
            && aircraft.pilots[0].flightInfo.HasTakenOff;
    }

    private static void Report(string text)
    {
        SceneSingleton<AircraftActionsReport>.i?.ReportText(text, 6f);
    }
}

/// <summary>
/// The airbase map icons live in a private lookup on DynamicMap, and the game
/// disables their images (invisible, and no raycast target, so no clicks) as
/// soon as the local player is flying. Everything needed to undo that lives here.
/// </summary>
internal static class MapAccess
{
    private static readonly FieldInfo AirbaseIconsField =
        AccessTools.Field(typeof(DynamicMap), "airbaseIconLookup");

    private static Dictionary<Airbase, AirbaseMapIcon> GetIcons(DynamicMap map)
    {
        if (map == null || AirbaseIconsField == null)
            return null;
        return AirbaseIconsField.GetValue(map) as Dictionary<Airbase, AirbaseMapIcon>;
    }

    /// <summary>Re-show and re-arm the airbase icons while the big map is open in flight.</summary>
    public static void KeepAirbaseIconsUsable(bool inFlight)
    {
        if (!inFlight || !DynamicMap.mapMaximized)
            return;
        Dictionary<Airbase, AirbaseMapIcon> icons = GetIcons(SceneSingleton<DynamicMap>.i);
        if (icons == null)
            return;
        foreach (AirbaseMapIcon icon in icons.Values)
        {
            if (icon == null || icon.iconImage == null)
                continue;
            icon.iconImage.enabled = true;
            // SelectIcon() clears this, which would make the chosen base unclickable.
            icon.iconImage.raycastTarget = true;
        }
    }

    /// <summary>Move the map highlight without disturbing the player's other selections.</summary>
    public static void HighlightAirbase(Airbase previous, Airbase current)
    {
        DynamicMap map = SceneSingleton<DynamicMap>.i;
        Dictionary<Airbase, AirbaseMapIcon> icons = GetIcons(map);
        if (icons == null)
            return;

        if (previous != null && previous != current
            && icons.TryGetValue(previous, out AirbaseMapIcon previousIcon) && previousIcon != null)
        {
            previousIcon.DeselectIcon();
            map.selectedIcons.Remove(previousIcon);
        }

        if (current != null && icons.TryGetValue(current, out AirbaseMapIcon icon) && icon != null)
        {
            icon.SelectIcon();
            if (!map.selectedIcons.Contains(icon))
                map.selectedIcons.Add(icon);
        }
    }

    /// <summary>Nearest airbase icon to a screen point, or null if none is within reach.</summary>
    public static AirbaseMapIcon NearestAirbaseIcon(DynamicMap map, Vector3 screenPoint,
        float maxSquareDistance, out float squareDistance)
    {
        squareDistance = maxSquareDistance;
        AirbaseMapIcon nearest = null;
        Dictionary<Airbase, AirbaseMapIcon> icons = GetIcons(map);
        if (icons == null)
            return null;

        foreach (AirbaseMapIcon icon in icons.Values)
        {
            if (icon == null || icon.iconImage == null || !icon.gameObject.activeInHierarchy
                || icon.airbase == null)
                continue;
            float distance = FastMath.SquareDistance(screenPoint, icon.iconImage.transform.position);
            if (distance < squareDistance)
            {
                squareDistance = distance;
                nearest = icon;
            }
        }
        return nearest;
    }
}

/// <summary>
/// After the native overlay evaluated its own nearest-airbase pass (every 2 s),
/// overwrite its target with the chosen base; the native LateUpdate then
/// renders the glideslope and markers without further help.
/// </summary>
[HarmonyPatch(typeof(AirbaseOverlay), "UpdateNearestAirbase")]
internal static class ApproachAssistOverlayPatch
{
    private static readonly FieldInfo NearestAirbaseField =
        AccessTools.Field(typeof(AirbaseOverlay), "nearestAirbase");
    private static readonly FieldInfo RunwayUsageField =
        AccessTools.Field(typeof(AirbaseOverlay), "runwayUsage");

    private static AirbaseOverlay overlay;

    /// <summary>Push the chosen base into the overlay right away, between its slow passes.</summary>
    public static void ApplyNow()
    {
        Apply(overlay);
    }

    private static void Postfix(AirbaseOverlay __instance)
    {
        overlay = __instance;
        Apply(__instance);
    }

    private static void Apply(AirbaseOverlay instance)
    {
        if (instance == null || !ApproachAssist.IsReadyToForce()
            || NearestAirbaseField == null || RunwayUsageField == null)
            return;

        Airbase.Runway.RunwayUsage? usage = ApproachAssist.RequestLanding(ApproachAssist.ChosenAirbase);
        if (!usage.HasValue)
            return;
        NearestAirbaseField.SetValue(instance, ApproachAssist.ChosenAirbase);
        RunwayUsageField.SetValue(instance, usage.Value);
    }
}

/// <summary>
/// The native overlay only draws the glideslope with the gear down. Once a base
/// is deliberately selected, draw it straight away by calling the game's own
/// renderer after its LateUpdate switched the line off.
/// </summary>
[HarmonyPatch(typeof(AirbaseOverlay), "LateUpdate")]
internal static class ApproachAssistGlideslopePatch
{
    private static readonly FieldInfo RunwayUsageField =
        AccessTools.Field(typeof(AirbaseOverlay), "runwayUsage");
    private static readonly FieldInfo GlideslopeField =
        AccessTools.Field(typeof(AirbaseOverlay), "glideslope");
    private static readonly FieldInfo AimPointField =
        AccessTools.Field(typeof(AirbaseOverlay), "glideslopeAimPoint");
    private static readonly MethodInfo DrawGlideslope =
        AccessTools.Method(typeof(AirbaseOverlay), "DrawGlideslope");

    private static readonly object[] drawArguments = new object[2];

    private static void Postfix(AirbaseOverlay __instance)
    {
        if (DrawGlideslope == null || RunwayUsageField == null
            || GlideslopeField == null || AimPointField == null
            || !Plugin.GlideslopeWithGearUp.Value || !ApproachAssist.IsReadyToForce())
            return;

        Aircraft aircraft = ApproachAssist.Aircraft;
        if (aircraft == null || aircraft.gearDeployed)
            return; // Gear down: the native path already drew it.

        var usage = (Airbase.Runway.RunwayUsage?)RunwayUsageField.GetValue(__instance);
        if (!usage.HasValue || usage.Value.Runway == null)
            return;
        if (!(GlideslopeField.GetValue(__instance) is Image glideslope)
            || !(AimPointField.GetValue(__instance) is Image aimPoint))
            return;

        drawArguments[0] = aircraft;
        drawArguments[1] = usage.Value;
        bool drawn = (bool)DrawGlideslope.Invoke(__instance, drawArguments);
        glideslope.enabled = drawn;
        aimPoint.enabled = drawn;
    }
}

/// <summary>Track the local aircraft for the assist.</summary>
[HarmonyPatch(typeof(CombatHUD), "SetAircraft")]
internal static class ApproachAssistAircraftPatch
{
    private static void Postfix(Aircraft aircraft)
    {
        ApproachAssist.SetAircraft(aircraft);
    }
}

/// <summary>
/// Airbase map icons are click-dead while flying (the native handler only
/// serves spawn selection). In flight, a click picks the approach base instead.
/// </summary>
[HarmonyPatch(typeof(AirbaseMapIcon), "ClickIcon")]
internal static class ApproachAssistMapClickPatch
{
    private static void Postfix(AirbaseMapIcon __instance)
    {
        if (!Plugin.Enabled.Value || !ApproachAssist.HasLiveAircraft())
            return;
        if (__instance.airbase != null)
            ApproachAssist.RequestSelect(__instance.airbase);
    }
}

/// <summary>
/// The map's own "Select" bind (mouse or controller) only ever looks at unit
/// icons. In flight, let it fall through to an airbase icon under the cursor.
/// </summary>
[HarmonyPatch(typeof(DynamicMap), "SelectFromMap")]
internal static class ApproachAssistMapSelectPatch
{
    /// <summary>Native pick radius, squared screen pixels.</summary>
    private const float MaxSquareDistance = 10000f;

    private static bool Prefix(DynamicMap __instance)
    {
        if (!Plugin.Enabled.Value || !ApproachAssist.HasLiveAircraft())
            return true;

        Vector3 cursor = Input.mousePosition;
        AirbaseMapIcon icon = MapAccess.NearestAirbaseIcon(
            __instance, cursor, MaxSquareDistance, out float airbaseDistance);
        if (icon == null)
            return true;

        // A unit icon closer to the cursor still wins, so target selection is untouched.
        foreach (MapIcon mapIcon in __instance.mapIcons)
        {
            if (mapIcon == null || mapIcon.iconImage == null || !mapIcon.gameObject.activeInHierarchy)
                continue;
            if (FastMath.SquareDistance(cursor, mapIcon.iconImage.transform.position) < airbaseDistance)
                return true;
        }

        ApproachAssist.RequestSelect(icon.airbase);
        return false;
    }
}
