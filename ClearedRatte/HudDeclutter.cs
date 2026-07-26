using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ClearedRatte;

/// <summary>
/// Clears combat clutter off the HUD while an approach base is selected.
///
/// The game ships an unfinished version of this — CombatHUD.landingMode and
/// HUDUnitMarker.SetLandingMode() exist but nothing ever switches them on, so
/// the only decluttering you actually get is unit markers hiding when the gear
/// comes down, which is why it looks like it happens at landing clearance.
/// This hides the busy layers outright and puts back exactly what it took away.
/// </summary>
internal static class HudDeclutter
{
    /// <summary>One of these is up at any time; it is the reticle in the middle of the HUD.</summary>
    private static readonly string[] WeaponUiFields =
    {
        "MissileUI", "BoresightUI", "BombingUI", "TurretUI",
        "CargoUI", "LaserGuidedUI", "SlingUI", "NoWeaponUI",
    };

    private static bool active;
    private static int appliedFlags;
    private static readonly List<GameObject> hidden = new List<GameObject>();
    private static GameObject[] weaponUis;
    private static GameObject weaponUi;

    public static void Set(bool wanted)
    {
        if (!wanted)
        {
            Restore();
            return;
        }

        // Picking up config changes live means a restore-and-reapply cycle.
        if (active && appliedFlags != CurrentFlags())
            Restore();
        if (!active)
            Begin();
        if (active)
            Enforce();
    }

    private static void Begin()
    {
        CombatHUD hud = SceneSingleton<CombatHUD>.i;
        if (hud == null)
            return;

        hidden.Clear();
        weaponUi = null;
        weaponUis = null;

        if (Plugin.HideUnitMarkers.Value && hud.iconLayer != null)
            Hide(hud.iconLayer.gameObject);
        if (Plugin.HideTargetMarkers.Value)
        {
            Hide(hud.targetDesignator != null ? hud.targetDesignator.gameObject : null);
            Hide(Resolve(hud, "targetArrow"));
            Hide(Resolve(hud, "targetText"));
        }
        if (Plugin.HideWeaponStatus.Value)
            Hide(Resolve(hud, "topRightPanel"));
        if (Plugin.HideThreatList.Value)
            Hide(Resolve(hud, "threatList"));
        if (Plugin.HideWeaponUI.Value)
        {
            weaponUis = new GameObject[WeaponUiFields.Length];
            for (int i = 0; i < WeaponUiFields.Length; i++)
                weaponUis[i] = Resolve(hud, WeaponUiFields[i]);
        }

        appliedFlags = CurrentFlags();
        active = true;
    }

    /// <summary>
    /// Re-assert every frame: the weapon state machine puts its reticle back
    /// whenever the player changes station, and we want to keep winning.
    /// </summary>
    private static void Enforce()
    {
        for (int i = hidden.Count - 1; i >= 0; i--)
        {
            GameObject go = hidden[i];
            if (go == null)
            {
                hidden.RemoveAt(i);
                continue;
            }
            // Never switch off a layer the approach picture itself draws into.
            if (ApproachDisplay.DrawsInside(go))
            {
                go.SetActive(true);
                hidden.RemoveAt(i);
                continue;
            }
            if (go.activeSelf)
                go.SetActive(false);
        }

        if (weaponUis == null)
            return;
        foreach (GameObject ui in weaponUis)
        {
            if (ui == null || !ui.activeSelf)
                continue;
            weaponUi = ui; // Remember the newest one so the right reticle comes back.
            ui.SetActive(false);
        }
    }

    public static void Restore()
    {
        if (!active)
            return;

        foreach (GameObject go in hidden)
        {
            if (go != null)
                go.SetActive(true);
        }
        if (weaponUi != null)
            weaponUi.SetActive(true);

        hidden.Clear();
        weaponUis = null;
        weaponUi = null;
        active = false;
    }

    /// <summary>Only hide what is actually up, so restoring cannot switch something on.</summary>
    private static void Hide(GameObject go)
    {
        if (go != null && go.activeSelf)
            hidden.Add(go);
    }

    /// <summary>Field lookup that copes with the HUD storing objects, images and components alike.</summary>
    private static GameObject Resolve(CombatHUD hud, string field)
    {
        object value = AccessTools.Field(typeof(CombatHUD), field)?.GetValue(hud);
        if (value is GameObject go)
            return go;
        if (value is Component component)
            return component.gameObject;
        return null;
    }

    private static int CurrentFlags()
    {
        return (Plugin.HideUnitMarkers.Value ? 1 : 0)
            | (Plugin.HideTargetMarkers.Value ? 2 : 0)
            | (Plugin.HideWeaponUI.Value ? 4 : 0)
            | (Plugin.HideWeaponStatus.Value ? 8 : 0)
            | (Plugin.HideThreatList.Value ? 16 : 0);
    }
}
