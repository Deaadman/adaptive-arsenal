namespace AdaptiveArsenal.Api;

/// <summary>
/// What a whole shot fired with <see cref="Projectiles.Fire"/> hit, for its <see cref="ProjectileShot.OnShotFinished"/>:
/// enough to work out how many of its pellets hit each animal, and do the damage, bleeding and critical hits once.
/// </summary>
/// <param name="Fired">How many projectiles the shot fired, <see cref="ProjectileShot.Count"/>.</param>
/// <param name="Hits">
/// Everything they hit within <see cref="ProjectileShot.ResultDelay"/>, one for each projectile that did, in the order
/// they hit. Projectiles that missed or were still flying aren't in it.
/// </param>
public readonly record struct ProjectileShotResult(int Fired, IReadOnlyList<ProjectileHit> Hits);
