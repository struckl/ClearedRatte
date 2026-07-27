using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClearedRatte;

/// <summary>
/// The approach picture: a highway-in-the-sky tunnel down the glidepath and an
/// ILS-style deviation cluster with a virtual PAPI and a data block.
///
/// Every line is a clone of one of the overlay's own graphics, so it inherits
/// the HUD material, canvas and 1080p-referenced scaling, and is positioned the
/// same way the game positions its runway borders: screen-space pixels, rotated
/// to the target and scaled along its own +Y.
/// </summary>
internal static class ApproachDisplay
{
    /// <summary>The 6 % grade the game's own glidepath math uses.</summary>
    private const float GlidepathGrade = 0.06f;

    /// <summary>Deflection at the edge of the scales, in degrees, as per ILS.</summary>
    private const float LocalizerFullScale = 2.5f;
    private const float GlidepathFullScale = 0.7f;

    /// <summary>Angles at which each PAPI light flips from red to white.</summary>
    private static readonly float[] PapiThresholds = { -0.5f, -0.17f, 0.17f, 0.5f };

    private static float[] gateDistances;
    private static int gateCount;

    private static Transform parent;
    private static Image lineTemplate;
    private static TextMeshProUGUI textTemplate;
    private static readonly List<Image> linePool = new List<Image>();
    private static readonly List<TextMeshProUGUI> textPool = new List<TextMeshProUGUI>();
    private static int linesUsed;
    private static int textsUsed;

    /// <summary>
    /// Gate distances from the threshold, geometric so that perspective spreads
    /// them roughly evenly on screen at any range.
    /// </summary>
    private static float[] GateDistances()
    {
        int count = Mathf.Clamp(Plugin.TunnelGates.Value, 3, 14);
        if (gateDistances != null && gateCount == count)
            return gateDistances;

        gateDistances = new float[count];
        float distance = 300f;
        for (int i = 0; i < count; i++)
        {
            gateDistances[i] = distance;
            distance *= 1.7f;
        }
        gateCount = count;
        return gateDistances;
    }

    public static void Draw(AirbaseOverlay overlay, Aircraft aircraft,
        Airbase.Runway.RunwayUsage usage, bool highway, bool instruments, bool nativeRunwayBox)
    {
        Camera camera = SceneSingleton<CameraStateManager>.i != null
            ? SceneSingleton<CameraStateManager>.i.mainCamera
            : null;
        if (camera == null || !Ensure(overlay))
        {
            Hide();
            return;
        }

        linesUsed = 0;
        textsUsed = 0;

        ApproachState state = ApproachState.Evaluate(aircraft, usage);
        Color color = DeviationColor(state.Deviation);

        if (highway)
        {
            if (!nativeRunwayBox)
                DrawRunwayBox(camera, usage.Runway, color);
            if (state.Distance > 30f)
                DrawHighway(camera, aircraft, usage, state, color);
        }
        if (instruments)
            DrawInstruments(state, color);

        Flush();
    }

    public static void Hide()
    {
        linesUsed = 0;
        textsUsed = 0;
        Flush();
    }

    /// <summary>
    /// True when the approach picture lives inside that object, so the HUD
    /// declutter knows not to switch off the layer we draw into.
    /// </summary>
    public static bool DrawsInside(GameObject candidate)
    {
        return parent != null && candidate != null && parent.IsChildOf(candidate.transform);
    }

    /// <summary>
    /// The tunnel of gates, plus the rails that make it read as a road.
    ///
    /// Gates fade with distance from the aircraft and the touchdown frame stays
    /// bright, so the corridor recedes into the aim point instead of every line
    /// competing for attention at the same weight.
    /// </summary>
    private static void DrawHighway(Camera camera, Aircraft aircraft,
        Airbase.Runway.RunwayUsage usage, in ApproachState state, Color color)
    {
        Airbase.Runway runway = usage.Runway;
        float halfWidth = Mathf.Clamp(runway.GetWidth() * 1.2f, 25f, 70f);
        Vector3 across = state.Right * halfWidth;
        Vector3 up = Vector3.up * (halfWidth * 0.45f);
        float span = Mathf.Max(state.Distance, 1f);
        float[] gates = GateDistances();

        Vector3 previousLeft = Vector3.zero;
        Vector3 previousRight = Vector3.zero;
        float previousAlpha = 0f;
        bool hasPrevious = false;

        // Index -1 is the touchdown frame, the rest recede back up the glidepath.
        for (int i = -1; i < gates.Length; i++)
        {
            bool aimFrame = i < 0;
            float along = aimFrame ? 0f : gates[i];
            if (along > state.Distance)
                break;

            float nearness = 1f - Mathf.Clamp01((state.Distance - along) / span);
            float alpha = aimFrame ? 1f : Mathf.Lerp(0.2f, 0.85f, nearness);
            Color gate = Fade(color, alpha);
            float width = aimFrame ? 1.8f : 1f;

            Vector3 center = runway.GetGlideslopeAimpoint(
                aircraft, along, usage.Reverse, state.TimeToReach(along));
            Vector3 bottomLeft = center - across - up;
            Vector3 bottomRight = center + across - up;
            Vector3 topLeft = center - across + up;
            Vector3 topRight = center + across + up;

            WorldLine(camera, bottomLeft, bottomRight, gate, width);
            WorldLine(camera, topLeft, topRight, gate, width);
            // Uprights carry the frame; keeping them lighter than the horizontals
            // stops the far gates reading as a stack of solid boxes.
            Color upright = aimFrame ? gate : Fade(color, alpha * 0.7f);
            WorldLine(camera, bottomRight, topRight, upright, width);
            WorldLine(camera, topLeft, bottomLeft, upright, width);

            if (hasPrevious)
            {
                Color rail = Fade(color, Mathf.Min(previousAlpha, alpha) * 0.45f);
                WorldLine(camera, previousLeft, bottomLeft, rail, 1f);
                WorldLine(camera, previousRight, bottomRight, rail, 1f);
            }
            previousLeft = bottomLeft;
            previousRight = bottomRight;
            previousAlpha = alpha;
            hasPrevious = true;
        }
    }

    /// <summary>The runway outline, same corners the native overlay uses on short final.</summary>
    private static void DrawRunwayBox(Camera camera, Airbase.Runway runway, Color color)
    {
        Transform start = runway.Start;
        Transform end = runway.End;
        if (start == null || end == null)
            return;

        float half = 0.5f * runway.GetWidth();
        Vector3 a = start.position - start.right * half;
        Vector3 b = start.position + start.right * half;
        Vector3 c = end.position + end.right * half;
        Vector3 d = end.position - end.right * half;
        WorldLine(camera, a, b, color, 1f);
        WorldLine(camera, b, c, color, 1f);
        WorldLine(camera, c, d, color, 1f);
        WorldLine(camera, d, a, color, 1f);
    }

    /// <summary>Deviation scales, virtual PAPI and data block, hung off the velocity vector.</summary>
    private static void DrawInstruments(in ApproachState state, Color color)
    {
        float unit = Screen.height / 1080f * Mathf.Clamp(Plugin.DisplayScale.Value, 0.4f, 2f);
        Vector3 anchor = GetAnchor();
        Color dim = Fade(color, 0.35f);
        float scale = 66f * unit;
        float offset = 82f * unit;

        // Localizer: horizontal scale below the anchor, needle deflects toward the course.
        float localizerY = anchor.y - offset;
        for (int i = -2; i <= 2; i++)
        {
            float x = anchor.x + i * scale * 0.5f;
            float tick = (i == 0 ? 7f : 3f) * unit;
            ScreenLine(new Vector3(x, localizerY - tick), new Vector3(x, localizerY + tick), dim, 1f);
        }
        float localizer = Mathf.Clamp(-state.LocalizerError / LocalizerFullScale, -1.12f, 1.12f);
        ScreenLine(
            new Vector3(anchor.x + localizer * scale, localizerY - 11f * unit),
            new Vector3(anchor.x + localizer * scale, localizerY + 11f * unit), color, 2f);

        // Glidepath: vertical scale right of the anchor, needle deflects toward the path.
        float glidepathX = anchor.x + offset;
        for (int i = -2; i <= 2; i++)
        {
            float y = anchor.y + i * scale * 0.5f;
            float tick = (i == 0 ? 7f : 3f) * unit;
            ScreenLine(new Vector3(glidepathX - tick, y), new Vector3(glidepathX + tick, y), dim, 1f);
        }
        float glidepath = Mathf.Clamp(-state.GlidepathError / GlidepathFullScale, -1.12f, 1.12f);
        ScreenLine(
            new Vector3(glidepathX - 11f * unit, anchor.y + glidepath * scale),
            new Vector3(glidepathX + 11f * unit, anchor.y + glidepath * scale), color, 2f);

        DrawPapi(state, anchor, unit);
        DrawDataBlock(state, anchor, unit, localizerY, color);
    }

    /// <summary>Four lights, white once you are above that light's angle. On path reads white white red red.</summary>
    private static void DrawPapi(in ApproachState state, Vector3 anchor, float unit)
    {
        Color white = Color.white;
        Color red = Assets != null ? Assets.HUDHostile : new Color(1f, 0.3f, 0.25f);
        float y = anchor.y + 86f * unit;
        float spacing = 17f * unit;
        float half = 4.5f * unit;

        for (int i = 0; i < PapiThresholds.Length; i++)
        {
            float x = anchor.x + (i - 1.5f) * spacing;
            Color light = state.GlidepathError > PapiThresholds[i] ? white : red;
            ScreenLine(new Vector3(x - half, y), new Vector3(x + half, y), light, 3f);
        }
    }

    /// <summary>
    /// Range, time to go and speed against reference. Deviation is deliberately
    /// absent: the needles and the PAPI already say it, and saying it twice is
    /// what makes a HUD look like a debug overlay.
    /// </summary>
    private static void DrawDataBlock(in ApproachState state, Vector3 anchor, float unit,
        float localizerY, Color color)
    {
        TextMeshProUGUI text = NextText();
        if (text == null)
            return;

        string timeToGo = state.TimeToGo > 0f
            ? $"{Mathf.FloorToInt(state.TimeToGo / 60f)}:{Mathf.FloorToInt(state.TimeToGo % 60f):00}"
            : "--:--";
        float excess = state.Speed - state.ReferenceSpeed;
        string reference = $"REF {(excess >= 0f ? "+" : "-")}{UnitConverter.SpeedReading(Mathf.Abs(excess))}";

        text.text = $"{UnitConverter.DistanceReading(state.Slant)}   {timeToGo}\n"
            + $"{UnitConverter.SpeedReading(state.Speed)}   {reference}";
        text.color = color;
        text.alignment = TextAlignmentOptions.Top;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontSize = Mathf.Max(9, Mathf.RoundToInt(
            PlayerSettings.overlayTextSize * 0.5f * Mathf.Clamp(Plugin.DisplayScale.Value, 0.4f, 2f)));
        text.transform.position = new Vector3(
            Mathf.Round(anchor.x), Mathf.Round(localizerY - 22f * unit), 0f);
        text.enabled = true;
    }

    /// <summary>Velocity vector when the flight HUD is up, screen centre otherwise, kept on screen.</summary>
    private static Vector3 GetAnchor()
    {
        Vector3 anchor = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        FlightHud hud = SceneSingleton<FlightHud>.i;
        if (hud != null && hud.velocityVector != null && hud.velocityVector.gameObject.activeInHierarchy)
        {
            Vector3 velocityVector = hud.velocityVector.transform.position;
            anchor = new Vector3(velocityVector.x, velocityVector.y, 0f);
        }
        anchor.x = Mathf.Clamp(anchor.x, Screen.width * 0.18f, Screen.width * 0.82f);
        anchor.y = Mathf.Clamp(anchor.y, Screen.height * 0.22f, Screen.height * 0.78f);
        return anchor;
    }

    private static Color DeviationColor(float deviation)
    {
        if (Assets == null)
            return deviation < 0.5f ? Color.green : (deviation < 1f ? Color.yellow : Color.red);
        if (deviation < 0.5f)
            return Assets.HUDFriendly;
        return deviation < 1f ? Assets.HUDNeutralSelected : Assets.HUDHostile;
    }

    private static GameAssets Assets => GameAssets.i;

    /// <summary>Everything the approach needs to know, in one pass.</summary>
    private readonly struct ApproachState
    {
        public readonly Vector3 Right;
        /// <summary>Horizontal distance still to run to the threshold.</summary>
        public readonly float Distance;
        public readonly float Slant;
        public readonly float Closure;
        public readonly float TimeToGo;
        public readonly float RequiredAngle;
        public readonly float GlidepathAngle;
        /// <summary>Degrees off the glidepath, positive high.</summary>
        public readonly float GlidepathError;
        /// <summary>Degrees off the centreline, positive right of course.</summary>
        public readonly float LocalizerError;
        public readonly float Speed;
        public readonly float ReferenceSpeed;

        private ApproachState(Vector3 right, float distance, float slant, float closure, float timeToGo,
            float requiredAngle, float glidepathAngle, float localizerError, float speed, float referenceSpeed)
        {
            Right = right;
            Distance = distance;
            Slant = slant;
            Closure = closure;
            TimeToGo = timeToGo;
            RequiredAngle = requiredAngle;
            GlidepathAngle = glidepathAngle;
            GlidepathError = glidepathAngle - requiredAngle;
            LocalizerError = localizerError;
            Speed = speed;
            ReferenceSpeed = referenceSpeed;
        }

        /// <summary>Worst of the two deviations, 1 = full-scale.</summary>
        public float Deviation => Mathf.Max(
            Mathf.Abs(GlidepathError) / GlidepathFullScale,
            Mathf.Abs(LocalizerError) / LocalizerFullScale);

        /// <summary>Seconds until the aircraft reaches a point that far up the path, for carrier drift.</summary>
        public float TimeToReach(float along)
        {
            if (Closure < 5f)
                return 0f;
            return Mathf.Max(0f, (Distance - along) / Closure);
        }

        public static ApproachState Evaluate(Aircraft aircraft, Airbase.Runway.RunwayUsage usage)
        {
            Vector3 position = aircraft.transform.position;
            Vector3 deckVelocity = usage.Runway.GetVelocity();
            Vector3 relativeVelocity = aircraft.rb.velocity - deckVelocity;
            Vector3 threshold = usage.GetEnd().position
                + Vector3.up * (aircraft.definition.spawnOffset.y + 0.5f);

            float slant = Vector3.Distance(position, threshold);
            float closure = ClosureRate(relativeVelocity, position, threshold, slant);

            // Aim where the deck will be when you get there, the same lead the
            // gates use — otherwise the needles disagree with the tunnel by the
            // length of the carrier's run, which is most of a mile from far out.
            if (closure > 5f && deckVelocity.sqrMagnitude > 0.01f)
            {
                threshold += deckVelocity * (slant / closure);
                slant = Vector3.Distance(position, threshold);
                closure = ClosureRate(relativeVelocity, position, threshold, slant);
            }

            Vector3 direction = usage.GetDirection();
            Vector3 forward = new Vector3(direction.x, 0f, direction.z).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 offset = position - threshold;
            float distance = -Vector3.Dot(offset, forward);
            float lateral = Vector3.Dot(offset, right);
            float timeToGo = closure > 5f ? slant / closure : -1f;

            float glidepathAngle = distance > 1f
                ? Mathf.Atan2(offset.y, distance) * Mathf.Rad2Deg
                : 0f;
            float localizerError = distance > 1f
                ? Mathf.Atan2(lateral, distance) * Mathf.Rad2Deg
                : 0f;

            return new ApproachState(right, distance, slant, closure, timeToGo,
                Mathf.Atan(GlidepathGrade) * Mathf.Rad2Deg, glidepathAngle, localizerError,
                aircraft.rb.velocity.magnitude, ApproachAssist.ReferenceSpeed(aircraft));
        }

        private static float ClosureRate(Vector3 relativeVelocity, Vector3 position, Vector3 threshold, float slant)
        {
            return slant > 1f ? Vector3.Dot(relativeVelocity, (threshold - position) / slant) : 0f;
        }
    }

    /// <summary>Draw a world-space segment, clipped against the camera's near side.</summary>
    private static void WorldLine(Camera camera, Vector3 a, Vector3 b, Color color, float width)
    {
        const float near = 2f;
        Vector3 position = camera.transform.position;
        Vector3 forward = camera.transform.forward;
        float aheadA = Vector3.Dot(a - position, forward);
        float aheadB = Vector3.Dot(b - position, forward);
        if (aheadA < near && aheadB < near)
            return;
        if (aheadA < near)
            a = Vector3.Lerp(a, b, (near - aheadA) / (aheadB - aheadA));
        else if (aheadB < near)
            b = Vector3.Lerp(b, a, (near - aheadB) / (aheadA - aheadB));

        ScreenLine(camera.WorldToScreenPoint(a), camera.WorldToScreenPoint(b), color, width, snap: false);
    }

    /// <summary>
    /// Position, rotate and stretch one pooled line between two screen points —
    /// the same trick AirbaseOverlay.DrawRunwayBorders uses.
    ///
    /// Instrument lines are snapped to whole device pixels: a thin line landing
    /// on a fractional pixel gets smeared across two of them by the sampler,
    /// which is most of what makes hand-drawn HUD graphics look soft. World
    /// geometry is left unsnapped — quantising something that moves every frame
    /// trades a little softness for shimmer.
    /// </summary>
    private static void ScreenLine(Vector3 from, Vector3 to, Color color, float width, bool snap = true)
    {
        Image line = NextLine();
        if (line == null)
            return;

        float x = snap ? Mathf.Round(from.x) : from.x;
        float y = snap ? Mathf.Round(from.y) : from.y;
        Vector3 delta = snap
            ? new Vector3(Mathf.Round(to.x) - x, Mathf.Round(to.y) - y, 0f)
            : new Vector3(to.x - x, to.y - y, 0f);

        line.transform.position = new Vector3(x, y, 0f);
        line.transform.eulerAngles =
            new Vector3(0f, 0f, -Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg);
        line.transform.localScale =
            new Vector3(width, 1f + delta.magnitude * (1080f / Screen.height), 1f);
        line.color = color;
        line.enabled = true;
    }

    private static Color Fade(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, color.a * alpha);
    }

    /// <summary>Grab the graphics we clone from, once per scene.</summary>
    private static bool Ensure(AirbaseOverlay overlay)
    {
        if (lineTemplate != null && parent != null)
            return true;

        // A destroyed template means the scene went away and took the pool with it.
        linePool.Clear();
        textPool.Clear();

        var borders = AccessTools.Field(typeof(AirbaseOverlay), "runwayBorders")
            ?.GetValue(overlay) as Image[];
        Image template = borders != null && borders.Length > 0 ? borders[0] : null;
        if (template == null)
            template = AccessTools.Field(typeof(AirbaseOverlay), "glideslope")?.GetValue(overlay) as Image;
        if (template == null)
        {
            Plugin.Logger.LogWarning("No overlay line graphic to clone; approach display disabled.");
            return false;
        }

        lineTemplate = template;
        parent = template.transform.parent;
        textTemplate = AccessTools.Field(typeof(AirbaseOverlay), "airbaseLabel")?.GetValue(overlay) as TextMeshProUGUI;
        return true;
    }

    private static Image NextLine()
    {
        if (linesUsed < linePool.Count)
        {
            Image pooled = linePool[linesUsed++];
            return pooled != null ? pooled : null;
        }

        Image line = Object.Instantiate(lineTemplate, parent);
        line.name = "ClearedRatteLine";
        line.raycastTarget = false;
        line.transform.localScale = Vector3.one;
        line.gameObject.SetActive(true); // The template itself may be parked inactive.
        linePool.Add(line);
        linesUsed++;
        return line;
    }

    private static TextMeshProUGUI NextText()
    {
        if (textTemplate == null)
            return null;
        if (textsUsed < textPool.Count)
        {
            TextMeshProUGUI pooled = textPool[textsUsed++];
            return pooled != null ? pooled : null;
        }

        TextMeshProUGUI text = Object.Instantiate(textTemplate, parent);
        text.name = "ClearedRatteText";
        text.raycastTarget = false;
        text.transform.localScale = Vector3.one;
        text.gameObject.SetActive(true);
        textPool.Add(text);
        textsUsed++;
        return text;
    }

    private static void Flush()
    {
        for (int i = linesUsed; i < linePool.Count; i++)
        {
            if (linePool[i] != null)
                linePool[i].enabled = false;
        }
        for (int i = textsUsed; i < textPool.Count; i++)
        {
            if (textPool[i] != null)
                textPool[i].enabled = false;
        }
    }
}
