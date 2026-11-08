namespace AdaptiveArsenal.Api;

/// <summary>
/// A shot for <see cref="Projectiles.Fire"/> to fire: where from, how many projectiles, how fast, how they look and
/// what happens when they hit. A gun that fires pellets fires all of them in one shot, with <see cref="Count"/> and
/// <see cref="Spread"/>.
/// </summary>
/// <remarks>
/// Each projectile flies as a physics object under gravity, like the mod's rifle and revolver bullets. With
/// <see cref="OnShotFinished"/> left empty, each damages the animal it hits itself, the way those bullets do; set it
/// to do your own damage instead. The object is read when it's fired, so changing it afterwards changes nothing.
/// </remarks>
public sealed class ProjectileShot
{
    /// <summary>Where to fire from, in world space. <see cref="Projectiles.TryGetAim"/> finds the gun's muzzle.</summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// The direction to fire in, in world space: where the gun is aimed. Its forward axis is the direction.
    /// <see cref="Spread"/> scatters each projectile around it.
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.identity;

    /// <summary>How many projectiles to fire, like a shell's pellets. Fewer than one fires one.</summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// The furthest each projectile can stray from <see cref="Rotation"/>, in degrees. Each one gets its own random
    /// direction within that, spread evenly across the circle. 0 fires them all straight along it.
    /// </summary>
    public float Spread { get; set; }

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
    /// The damage at the muzzle, for the mod to deal when <see cref="OnShotFinished"/> is empty, and for
    /// <see cref="ProjectileHit.DamageAtDistance"/>. It falls with distance as the mod's own bullets' damage does. 0 or
    /// less uses the mod's default.
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
    /// Called once for the whole shot, with how many projectiles it fired and everything they hit, instead of the mod
    /// damaging the animals they hit. It comes when every projectile has hit something or been removed, or after
    /// <see cref="ResultDelay"/>, whichever is first. Exceptions it throws are logged.
    /// </summary>
    public Action<ProjectileShotResult>? OnShotFinished { get; set; }

    /// <summary>
    /// The most seconds to wait for the projectiles before calling <see cref="OnShotFinished"/>. Any still flying then
    /// count as misses, so one that flies off into the sky doesn't hold back the rest. 0 or less waits for every one,
    /// which for a miss can be seconds.
    /// </summary>
    public float ResultDelay { get; set; } = 0.5f;
}
