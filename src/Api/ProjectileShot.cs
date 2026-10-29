namespace AdaptiveArsenal.Api;

/// <summary>
/// One projectile for <see cref="Projectiles.Fire"/> to fire: where from, how fast, how it looks and what happens when
/// it hits. A gun that fires several pellets fires one of these for each.
/// </summary>
/// <remarks>
/// The projectile flies as a physics object under gravity, like the mod's rifle and revolver bullets. With
/// <see cref="OnHit"/> left empty, it damages animals itself, the way those bullets do; set it to do your own damage
/// instead. The object is read when it's fired, so changing it afterwards changes nothing.
/// </remarks>
public sealed class ProjectileShot
{
    /// <summary>Where to fire from, in world space. <see cref="Projectiles.TryGetAim"/> finds the gun's muzzle.</summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// The direction to fire in, in world space, with any spread already applied. Its forward axis is the direction.
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.identity;

    /// <summary>
    /// How fast the projectile leaves the muzzle, in metres per second, before the player's bullet speed setting
    /// scales it. The mod's rifle and revolver bullets fly at 150 to 550.
    /// </summary>
    public float MuzzleVelocity { get; set; }

    /// <summary>
    /// How quickly the air slows the projectile, as the Unity rigidbody's drag: higher slows it sooner, so it drops
    /// more at range. The mod's own bullets use 0.1.
    /// </summary>
    public float Drag { get; set; } = 0.1f;

    /// <summary>
    /// The damage at the muzzle, for the mod to deal when <see cref="OnHit"/> is empty. It falls with distance as the
    /// mod's own bullets' damage does. 0 or less uses the mod's default.
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// The weapon animals take the hit from when the mod deals the damage: how body parts scale it, how fast they
    /// bleed and the chance of a critical hit.
    /// </summary>
    public WeaponSource WeaponSource { get; set; } = WeaponSource.Unspecified;

    /// <summary>
    /// The gear name of the ammo whose single round the projectile looks like in flight, like
    /// <c>GEAR_RifleAmmoSingle</c>, or <see langword="null"/> for an invisible one. If the ammo can't be loaded, the
    /// projectile is invisible.
    /// </summary>
    public string? AmmoGearName { get; set; }

    /// <summary>
    /// Whether the projectile plays the game's bullet impact particles, decal and sound where it hits. Turn it off to
    /// play your own, like one sound for a whole spread of pellets.
    /// </summary>
    public bool PlayImpactEffects { get; set; } = true;

    /// <summary>
    /// Called once, when the projectile hits something, instead of the mod damaging the animal it hit. Exceptions it
    /// throws are logged and don't stop the projectile.
    /// </summary>
    public Action<ProjectileHit>? OnHit { get; set; }

    /// <summary>
    /// Called once, when the projectile is done: <see langword="true"/> right after it hits something, or
    /// <see langword="false"/> when it's removed without hitting anything, like a shot into the sky or one still in the
    /// air when the scene changes. Use it to tell when every pellet of a shot has landed. Exceptions it throws are
    /// logged.
    /// </summary>
    public Action<bool>? OnFinished { get; set; }
}
