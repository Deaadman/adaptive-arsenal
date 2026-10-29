namespace AdaptiveArsenal.Api;

/// <summary>What one projectile fired with <see cref="Projectiles.Fire"/> hit, in a <see cref="ProjectileShotResult"/>.</summary>
/// <param name="Collider">The collider it hit. For an animal, it's usually a body part with a <see cref="LocalizedDamage"/>.</param>
/// <param name="Point">Where it hit, in world space.</param>
/// <param name="Normal">The surface's normal where it hit.</param>
/// <param name="Direction">The direction it was flying in when it hit.</param>
/// <param name="Speed">How fast it was flying when it hit, in metres per second.</param>
/// <param name="Distance">How far it is from where it was fired, in metres.</param>
/// <param name="DamageAtDistance">
/// <see cref="ProjectileShot.Damage"/> after the mod's falloff for <paramref name="Distance"/>, before any body part
/// scales it, for using the mod's falloff with your own damage.
/// </param>
public readonly record struct ProjectileHit(
    Collider Collider,
    Vector3 Point,
    Vector3 Normal,
    Vector3 Direction,
    float Speed,
    float Distance,
    float DamageAtDistance);
