namespace AdaptiveArsenal.Api;

/// <summary>What one projectile fired with <see cref="Projectiles.Fire"/> hit, in a <see cref="ProjectileShotResult"/>.</summary>
/// <param name="Collider">The collider it hit. For an animal, it's usually a body part with a <see cref="LocalizedDamage"/>.</param>
/// <param name="Point">Where it hit, in world space.</param>
/// <param name="Normal">The surface's normal where it hit.</param>
/// <param name="Direction">The direction it was flying in when it hit.</param>
/// <param name="Speed">How fast it was flying when it hit, in metres per second.</param>
/// <param name="Distance">How far it is from where it was fired, in metres.</param>
/// <param name="DamageAtDistance">The same as <see cref="Damage"/>, under its name from before 1.2.0.</param>
public readonly record struct ProjectileHit(
    Collider Collider,
    Vector3 Point,
    Vector3 Normal,
    Vector3 Direction,
    float Speed,
    float Distance,
    [property: Obsolete("Use Damage. Since 1.2.0, the damage falls with the speed the projectile has lost, not only the distance.")]
    float DamageAtDistance)
{
    /// <summary>
    /// <see cref="ProjectileShot.Damage"/> after the mod's falloff, before any body part scales it, for using the mod's
    /// falloff with your own damage. With a <see cref="ProjectileShot.BallisticCoefficient"/>, it falls with the share
    /// of its muzzle energy the projectile has left; with only <see cref="ProjectileShot.Drag"/>, it falls with
    /// <see cref="Distance"/>, as before 1.2.0. The player's damage from speed setting blends either towards the full
    /// damage.
    /// </summary>
#pragma warning disable CS0618 // Damage replaces DamageAtDistance, which stays for mods built against 1.1.0.
    public float Damage => DamageAtDistance;
#pragma warning restore CS0618
}
