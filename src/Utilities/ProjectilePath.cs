using UnityEngine.Rendering;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// One bullet's path, drawn in the world while <see cref="Settings.m_DebugDrawPaths"/> is on: its flight, a line for
/// each ricochet, the way it was fired, the wind, and markers where it glanced off and hit. Each part stays for
/// <see cref="Settings.m_DebugPathDuration"/> after the bullet stops.
/// </summary>
/// <remarks>
/// <para>
/// The lines are their own objects, not the bullet's children, so they outlast it. They're drawn with an unlit shader
/// that takes the line's colour, falling back to the bullet trail's material.
/// </para>
/// <para>
/// Lines and markers grow with their distance from where the bullet was fired, so from there they look about the same
/// size near and far, and a path hundreds of metres out can still be seen.
/// </para>
/// </remarks>
internal sealed class ProjectilePath
{
    /// <summary>The narrowest the bullet's path is drawn, in metres, close to where it was fired.</summary>
    private const float PathWidth = 0.02f;

    /// <summary>The narrowest the line of sight, wind arrow, normals and markers are drawn, in metres.</summary>
    private const float GuideWidth = 0.01f;

    /// <summary>
    /// How much wider, in metres, lines are drawn for each metre from where the bullet was fired: about 0.1°, a pixel
    /// or two on screen from there.
    /// </summary>
    private const float WidthPerMetre = 0.002f;

    /// <summary>How far each arm of a marker reaches from its centre, in metres, close to where the bullet was fired.</summary>
    private const float MarkerSize = 0.15f;

    /// <summary>How much bigger, in metres, markers are drawn for each metre from where the bullet was fired.</summary>
    private const float MarkerSizePerMetre = 0.01f;

    /// <summary>How long the line for a ricochet's surface normal is, as a multiple of the marker's size there.</summary>
    private const float NormalLength = 3f;

    /// <summary>How long the wind arrow is for each metre per second of wind, in metres.</summary>
    private const float WindArrowScale = 0.25f;

    /// <summary>How many points each part of the path makes room for up front: 10 seconds of physics steps.</summary>
    private const int InitialCapacity = 512;

    /// <summary>The colour of each part of the path: before the first ricochet, after it, and after the second.</summary>
    private static readonly Color[] SegmentColours = [Color.yellow, new(1f, 0.5f, 0f), Color.red];

    /// <summary>The colour of the line of sight: the way the shot was fired, before the sights raised it.</summary>
    private static readonly Color AimColour = Color.cyan;

    /// <summary>The colour of the wind arrow.</summary>
    private static readonly Color WindColour = new(0.2f, 0.4f, 1f);

    /// <summary>The colour of a ricochet's marker.</summary>
    private static readonly Color RicochetColour = Color.magenta;

    /// <summary>The colour of the surface's normal where the bullet glanced off.</summary>
    private static readonly Color NormalColour = Color.green;

    /// <summary>The colour of the marker where the bullet hit.</summary>
    private static readonly Color HitColour = Color.red;

    /// <summary>The colour of the marker where a bullet that hit nothing was removed.</summary>
    private static readonly Color MissColour = Color.grey;

    /// <summary>The material every drawn line uses, found the first time it's needed.</summary>
    private static Material? s_Material;

    /// <summary>Whether <see cref="s_Material"/> has been looked for, so a failed search isn't repeated for every line.</summary>
    private static bool s_MaterialSearched;

    /// <summary>Where the bullet was fired from.</summary>
    private readonly Vector3 m_Start;

    /// <summary>The way the shot was fired, with its spread, before the sights raised it, of length 1.</summary>
    private readonly Vector3 m_AimDirection;

    /// <summary>Every line drawn so far, to remove after the bullet stops.</summary>
    private readonly List<GameObject> m_Objects = [];

    /// <summary>The points of the part of the path being drawn.</summary>
    private readonly List<Vector3> m_Points = new(InitialCapacity);

    /// <summary>The line of the part of the path being drawn, or <see langword="null"/> once the bullet has stopped.</summary>
    private LineRenderer? m_Line;

    /// <summary>How many times the bullet has glanced off, which sets the next part's colour.</summary>
    private int m_Segment;

    /// <summary>Where the first part of the path ended, to draw the line of sight as far.</summary>
    private Vector3? m_FirstSegmentEnd;

    /// <summary>Starts drawing a bullet's path where it was fired, with an arrow for the wind.</summary>
    /// <param name="start">Where the bullet was fired from.</param>
    /// <param name="aimDirection">The way the shot was fired, with its spread, before the sights raised it.</param>
    /// <param name="wind">The wind's velocity, in m/s, or zero for none.</param>
    internal ProjectilePath(Vector3 start, Vector3 aimDirection, Vector3 wind)
    {
        m_Start = start;
        m_AimDirection = aimDirection.normalized;

        if (wind.sqrMagnitude > 0f)
            DrawArrow(start, start + wind * WindArrowScale, WindColour);

        StartSegment(start);
    }

    /// <summary>Adds the bullet's position after a physics step, widening the line's far end to match.</summary>
    /// <param name="point">Where the bullet is now.</param>
    internal void Add(Vector3 point)
    {
        if (m_Line == null)
            return;

        m_Points.Add(point);
        m_Line.positionCount = m_Points.Count;
        m_Line.SetPosition(m_Points.Count - 1, point);
        m_Line.endWidth = WidthAt(point, PathWidth);
    }

    /// <summary>
    /// Ends the part of the path before a ricochet at the point the bullet glanced off, marks it, draws the surface's
    /// normal, and starts the next part there in the next colour.
    /// </summary>
    /// <param name="point">Where the bullet glanced off.</param>
    /// <param name="normal">The surface's normal there.</param>
    internal void Ricochet(Vector3 point, Vector3 normal)
    {
        if (m_Line == null)
            return;

        EndSegment(point);
        DrawMarker(point, RicochetColour);
        var normalEnd = point + normal.normalized * (MarkerSizeAt(point) * NormalLength);
        DrawLine([point, normalEnd], NormalColour, WidthAt(point, GuideWidth), WidthAt(normalEnd, GuideWidth));

        m_Segment++;
        StartSegment(point);
    }

    /// <summary>
    /// Ends the path where the bullet stopped, marks it, draws the line of sight, and removes everything after
    /// <see cref="Settings.m_DebugPathDuration"/>.
    /// </summary>
    /// <param name="point">Where the bullet stopped.</param>
    /// <param name="hit">Whether it hit something, rather than being removed after flying too long.</param>
    internal void End(Vector3 point, bool hit)
    {
        if (m_Line == null)
            return;

        EndSegment(point);
        DrawMarker(point, hit ? HitColour : MissColour);

        // The line of sight reaches as far as the bullet got before it first glanced off, so the drop below it shows.
        var aimEnd = m_Start + m_AimDirection * Vector3.Distance(m_Start, m_FirstSegmentEnd ?? point);
        DrawLine([m_Start, aimEnd], AimColour, GuideWidth, WidthAt(aimEnd, GuideWidth));

        m_Line = null;
        RemoveLater();
    }

    /// <summary>
    /// Removes what's drawn after <see cref="Settings.m_DebugPathDuration"/>, without drawing anything more, for a
    /// bullet destroyed before it stopped, as when its scene unloads.
    /// </summary>
    internal void Abandon()
    {
        if (m_Line == null)
            return;

        m_Line = null;
        RemoveLater();
    }

    /// <summary>How wide to draw a line at a point, growing with its distance from where the bullet was fired.</summary>
    /// <param name="point">The point.</param>
    /// <param name="minimum">The narrowest it's drawn, close to where the bullet was fired.</param>
    private float WidthAt(Vector3 point, float minimum) =>
        Math.Max(minimum, Vector3.Distance(m_Start, point) * WidthPerMetre);

    /// <summary>How far a marker's arms reach at a point, growing with its distance from where the bullet was fired.</summary>
    /// <param name="point">The marker's centre.</param>
    private float MarkerSizeAt(Vector3 point) =>
        Math.Max(MarkerSize, Vector3.Distance(m_Start, point) * MarkerSizePerMetre);

    /// <summary>Starts a part of the path at a point, in the colour for <see cref="m_Segment"/>.</summary>
    /// <param name="start">Where the part starts.</param>
    private void StartSegment(Vector3 start)
    {
        m_Points.Clear();
        m_Points.Add(start);
        var width = WidthAt(start, PathWidth);
        m_Line = DrawLine(m_Points, SegmentColours[Math.Min(m_Segment, SegmentColours.Length - 1)], width, width);
    }

    /// <summary>Ends the part of the path being drawn at a point.</summary>
    /// <param name="end">Where the part ends.</param>
    private void EndSegment(Vector3 end)
    {
        Add(end);
        m_FirstSegmentEnd ??= end;
    }

    /// <summary>Draws a cross of three lines, one along each axis, centred on a point.</summary>
    /// <param name="point">The marker's centre.</param>
    /// <param name="colour">Its colour.</param>
    private void DrawMarker(Vector3 point, Color colour)
    {
        var size = MarkerSizeAt(point);
        var width = WidthAt(point, GuideWidth);
        DrawLine([point - Vector3.right * size, point + Vector3.right * size], colour, width, width);
        DrawLine([point - Vector3.up * size, point + Vector3.up * size], colour, width, width);
        DrawLine([point - Vector3.forward * size, point + Vector3.forward * size], colour, width, width);
    }

    /// <summary>Draws a line with a small head at its tip.</summary>
    /// <param name="from">The arrow's tail.</param>
    /// <param name="to">The arrow's tip.</param>
    /// <param name="colour">Its colour.</param>
    private void DrawArrow(Vector3 from, Vector3 to, Color colour)
    {
        DrawLine([from, to], colour, GuideWidth, GuideWidth);

        var back = (from - to).normalized * MarkerSize;
        var side = Vector3.Cross(back, Vector3.up);
        if (side.sqrMagnitude <= 0f)
            side = Vector3.right;

        side = side.normalized * (MarkerSize * 0.5f);
        DrawLine([to + back + side, to, to + back - side], colour, GuideWidth, GuideWidth);
    }

    /// <summary>Draws a line through some points, as its own object.</summary>
    /// <param name="points">The points, in world space.</param>
    /// <param name="colour">The line's colour.</param>
    /// <param name="startWidth">How wide it is at its first point, in metres.</param>
    /// <param name="endWidth">How wide it is at its last point, in metres.</param>
    /// <returns>The line, to add points to.</returns>
    private LineRenderer DrawLine(List<Vector3> points, Color colour, float startWidth, float endWidth)
    {
        GameObject lineObject = new("AdaptiveArsenal_DebugPath");
        m_Objects.Add(lineObject);

        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.startColor = colour;
        line.endColor = colour;

        var material = GetMaterial();
        if (material != null)
            line.material = material;

        line.positionCount = points.Count;
        for (var i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);

        return line;
    }

    /// <summary>Removes everything drawn after <see cref="Settings.m_DebugPathDuration"/>.</summary>
    private void RemoveLater()
    {
        var duration = Math.Max(0f, Settings.Instance.m_DebugPathDuration);
        foreach (var drawn in m_Objects)
        {
            // Unity objects need == null, not ?., to tell when they've been destroyed.
            if (drawn != null)
                UnityEngine.Object.Destroy(drawn, duration);
        }

        m_Objects.Clear();
    }

    /// <summary>
    /// The material the lines are drawn with: an unlit shader that shows each line's own colour, or the arrow's trail
    /// material if the game hasn't one. It's looked for once, and again only if a material it found is destroyed.
    /// </summary>
    /// <returns>The material, or <see langword="null"/> if there's none, which draws the lines in Unity's error pink.</returns>
    private static Material? GetMaterial()
    {
        // Unity objects need == null, not ??=, to tell when they've been destroyed.
        if (s_Material != null || s_MaterialSearched && s_Material is null)
            return s_Material;

        s_MaterialSearched = true;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");

        if (shader != null)
        {
            s_Material = new Material(shader);
            return s_Material;
        }

        s_Material = PrefabMaterials.GetLineRendererMaterial("GEAR_Arrow", "LineRenderer");
        Mod.Logger.Log(
            s_Material != null
                ? "Couldn't find an unlit shader for drawing bullet paths, so they use the trail's material and may not show their colours."
                : "Couldn't find an unlit shader or the trail's material for drawing bullet paths, so they're drawn without a material.",
            FlaggedLoggingLevel.Warning);
        return s_Material;
    }
}
