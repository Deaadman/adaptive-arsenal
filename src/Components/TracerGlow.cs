using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Components;

/// <summary>
/// The red glow of a tracer bullet in flight: a small red light that moves with it, and a short bright streak over the
/// distance it covered since the last frame, which is how a burning tracer looks to the eye. It's separate from the
/// bullet's trail, which is unchanged.
/// </summary>
/// <remarks>
/// A bullet covers several metres each frame, so a glow drawn only where it is would jump from point to point. The
/// streak joins them up.
/// </remarks>
internal sealed class TracerGlow
{
    /// <summary>The glow's colour.</summary>
    private static readonly Color s_Colour = new(1f, 0.15f, 0.05f);

    /// <summary>How far, in metres, the light reaches.</summary>
    private const float LightRange = 4f;

    /// <summary>How bright the light is.</summary>
    private const float LightIntensity = 3f;

    /// <summary>The streak's width at the bullet, in metres.</summary>
    private const float StreakHeadWidth = 0.08f;

    /// <summary>The streak's width at its tail, in metres.</summary>
    private const float StreakTailWidth = 0.02f;

    /// <summary>The longest the streak gets, in metres, so a slow frame doesn't draw a long line.</summary>
    private const float MaxStreakLength = 10f;

    /// <summary>The object holding the light and streak, a child of the bullet.</summary>
    private readonly GameObject m_Object;

    /// <summary>The streak, or <see langword="null"/> if the material couldn't be found.</summary>
    private readonly LineRenderer? m_Streak;

    /// <summary>Where the bullet was last frame, where the streak's tail is.</summary>
    private Vector3 m_LastPosition;

    /// <summary>Adds a glow to a bullet that has just been fired.</summary>
    /// <param name="bullet">The bullet.</param>
    internal TracerGlow(Transform bullet)
    {
        m_Object = new GameObject("TracerGlow");
        m_Object.transform.SetParent(bullet, false);
        m_LastPosition = bullet.position;

        var light = m_Object.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = s_Colour;
        light.range = LightRange;
        light.intensity = LightIntensity;
        light.shadows = LightShadows.None;

        var material = PrefabMaterials.ArrowTrail;
        if (material == null)
        {
            Mod.Logger.Log("Couldn't find the arrow's trail material, so tracers only light up.", FlaggedLoggingLevel.Warning);
            return;
        }

        m_Streak = m_Object.AddComponent<LineRenderer>();
        m_Streak.material = material;
        m_Streak.useWorldSpace = true;
        m_Streak.startWidth = StreakHeadWidth;
        m_Streak.endWidth = StreakTailWidth;
        m_Streak.startColor = s_Colour;
        m_Streak.endColor = new Color(s_Colour.r, s_Colour.g, s_Colour.b, 0f);
        m_Streak.positionCount = 2;
        m_Streak.SetPosition(0, m_LastPosition);
        m_Streak.SetPosition(1, m_LastPosition);
    }

    /// <summary>Draws the streak from where the bullet was last frame to where it is now.</summary>
    /// <param name="position">Where the bullet is now.</param>
    internal void Follow(Vector3 position)
    {
        if (m_Streak is not null)
        {
            var tail = Vector3.Distance(position, m_LastPosition) > MaxStreakLength
                ? position + (m_LastPosition - position).normalized * MaxStreakLength
                : m_LastPosition;

            m_Streak.SetPosition(0, position);
            m_Streak.SetPosition(1, tail);
        }

        m_LastPosition = position;
    }

    /// <summary>Puts the glow out, once the bullet has hit something.</summary>
    internal void Extinguish() => m_Object.SetActive(false);
}
