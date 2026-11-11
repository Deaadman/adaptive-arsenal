using Vec3 = System.Numerics.Vector3;

namespace AdaptiveArsenal.Core;

/// <summary>
/// When a bullet glances off a hard surface instead of stopping, and which way it goes after.
/// </summary>
/// <remarks>
/// A real bullet that meets rock, metal or ice at a shallow angle skips off it, slower and a little off the mirror
/// angle, and can still hit something. Steeper hits, and softer surfaces like snow, wood or flesh, stop it. The numbers
/// here are starting points to tune in game.
/// </remarks>
internal static class Ricochet
{
    /// <summary>The steepest angle, in degrees between the bullet's path and the surface, at which it glances off.</summary>
    internal const float MaxAngleDegrees = 10f;

    /// <summary>The share of its speed a bullet keeps when it glances off.</summary>
    internal const float SpeedRetained = 0.6f;

    /// <summary>The furthest, in degrees, a glancing bullet can stray from the mirror angle.</summary>
    internal const float ScatterDegrees = 3f;

    /// <summary>How many times a bullet can glance off before the next hard surface stops it.</summary>
    internal const int MaxRicochets = 2;

    /// <summary>
    /// The shallowest angle, in degrees, a glancing bullet leaves the surface at, so scatter can never send it back into
    /// the surface it glanced off.
    /// </summary>
    private const float MinExitAngleDegrees = 0.5f;

    /// <summary>The angle, in degrees, between a bullet's path and the surface it hits: 0 grazing, 90 head on.</summary>
    /// <param name="velocity">The bullet's velocity as it hits.</param>
    /// <param name="normal">The surface's normal where it hits.</param>
    /// <returns>The angle, or 0 if the bullet isn't moving or the normal is zero.</returns>
    internal static float AngleToSurface(Vec3 velocity, Vec3 normal)
    {
        var speed = velocity.Length();
        var normalLength = normal.Length();
        if (speed <= 0f || normalLength <= 0f)
            return 0f;

        var sine = MathF.Abs(Vec3.Dot(velocity / speed, normal / normalLength));
        return MathF.Asin(MathF.Min(1f, sine)) * 180f / MathF.PI;
    }

    /// <summary>Whether a bullet glances off a surface rather than stopping: <see cref="IsShallow"/> and <see cref="IsHard"/>.</summary>
    /// <param name="velocity">The bullet's velocity as it hits.</param>
    /// <param name="normal">The surface's normal where it hits.</param>
    /// <param name="surface">What the surface is.</param>
    /// <param name="ricochetsSoFar">How many times the bullet has already glanced off.</param>
    internal static bool Glances(Vec3 velocity, Vec3 normal, SurfaceKind surface, int ricochetsSoFar) =>
        IsShallow(velocity, normal, ricochetsSoFar) && IsHard(surface);

    /// <summary>
    /// The half of <see cref="Glances"/> that needs no surface: whether the bullet, with ricochets to spare, is moving
    /// into the surface at under <see cref="MaxAngleDegrees"/>. The game checks it first, since it's cheap and most
    /// hits fail it, before looking up what the surface is.
    /// </summary>
    /// <param name="velocity">The bullet's velocity as it hits.</param>
    /// <param name="normal">The surface's normal where it hits.</param>
    /// <param name="ricochetsSoFar">How many times the bullet has already glanced off, up to <see cref="MaxRicochets"/>.</param>
    internal static bool IsShallow(Vec3 velocity, Vec3 normal, int ricochetsSoFar) =>
        ricochetsSoFar < MaxRicochets
        && Vec3.Dot(velocity, normal) < 0f
        && AngleToSurface(velocity, normal) < MaxAngleDegrees;

    /// <summary>
    /// Whether a surface is hard enough for a bullet to glance off: rock, metal and ice. Snow, wood, fabric, flesh and
    /// glass stop it, and so do untagged surfaces, since what they are isn't known.
    /// </summary>
    /// <param name="surface">What the surface is.</param>
    internal static bool IsHard(SurfaceKind surface) => surface is SurfaceKind.Stone or SurfaceKind.Metal or SurfaceKind.Ice;

    /// <summary>
    /// The velocity of a bullet after it glances off: mirrored off the surface, turned up to
    /// <see cref="ScatterDegrees"/> in a random direction, and slowed to <see cref="SpeedRetained"/> of its speed.
    /// </summary>
    /// <param name="velocity">The bullet's velocity as it hits.</param>
    /// <param name="normal">The surface's normal where it hits.</param>
    /// <param name="random1">A random number from 0 to 1, which picks the direction of the scatter.</param>
    /// <param name="random2">A random number from 0 to 1, which picks how far it scatters.</param>
    /// <returns>The new velocity, always leaving the surface at <see cref="MinExitAngleDegrees"/> or more.</returns>
    internal static Vec3 Deflect(Vec3 velocity, Vec3 normal, float random1, float random2)
    {
        var speed = velocity.Length();
        if (speed <= 0f || normal.LengthSquared() <= 0f)
            return Vec3.Zero;

        var surfaceNormal = Vec3.Normalize(normal);
        var mirrored = Vec3.Normalize(velocity - 2f * Vec3.Dot(velocity, surfaceNormal) * surfaceNormal);

        // Two directions at right angles to the path, to scatter it around.
        var reference = MathF.Abs(mirrored.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX;
        var across = Vec3.Normalize(Vec3.Cross(mirrored, reference));
        var up = Vec3.Cross(mirrored, across);

        var around = 2f * MathF.PI * Math.Clamp(random1, 0f, 1f);
        var scatter = ScatterDegrees * MathF.Sqrt(Math.Clamp(random2, 0f, 1f)) * MathF.PI / 180f;
        var direction = mirrored * MathF.Cos(scatter)
                        + (across * MathF.Cos(around) + up * MathF.Sin(around)) * MathF.Sin(scatter);

        // Lift it clear of the surface if the scatter turned it too far down.
        var minExit = MathF.Sin(MinExitAngleDegrees * MathF.PI / 180f);
        var exit = Vec3.Dot(direction, surfaceNormal);
        if (exit < minExit)
            direction += surfaceNormal * (minExit - exit);

        return Vec3.Normalize(direction) * (speed * SpeedRetained);
    }
}

/// <summary>
/// What a bullet hit, as the game sorts surfaces for its impact effects (<c>BulletImpactEffectType</c>), which can't be
/// used here. Animals are <see cref="Flesh"/>, whatever their tag.
/// </summary>
internal enum SurfaceKind
{
    /// <summary>Snow.</summary>
    Snow,

    /// <summary>Wood.</summary>
    Wood,

    /// <summary>Cloth and other fabric.</summary>
    Fabric,

    /// <summary>Ice.</summary>
    Ice,

    /// <summary>Rock and stone.</summary>
    Stone,

    /// <summary>Metal.</summary>
    Metal,

    /// <summary>An animal, or anything else of flesh.</summary>
    Flesh,

    /// <summary>Glass.</summary>
    Glass,

    /// <summary>A surface with no material tag, whose kind isn't known.</summary>
    Untagged
}
