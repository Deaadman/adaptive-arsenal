using AdaptiveArsenal.Components;
using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Api;

/// <summary>
/// Lets other mods fire projectiles that fly like Adaptive Arsenal's bullets, for guns of their own, like a shotgun
/// whose slugs and pellets should drop differently. See <c>docs/api.md</c>.
/// </summary>
/// <remarks>
/// Call these from the game's main thread, as Unity needs. A mod that works without Adaptive Arsenal should only reach
/// this class when Adaptive Arsenal is loaded, and fall back to its own shots when <see cref="Fire"/> returns
/// <see langword="false"/>.
/// </remarks>
public static class Projectiles
{
    /// <summary>
    /// The version of this API. It goes up by one with each change that could break a mod using it, so a mod can check
    /// it's the one it was built for.
    /// </summary>
    public static int ApiVersion { get; } = 1;

    /// <summary>
    /// Whether the player has real bullets on in the mod's settings. When they're off, <see cref="Fire"/> fires nothing,
    /// so the guns should fire the game's own shots.
    /// </summary>
    public static bool Enabled => Settings.Instance.m_ProjectilesEnabled;

    /// <summary>
    /// Fires a shot: all of its projectiles, each scattered within its spread. Returns <see langword="false"/>, firing
    /// nothing, when real bullets are off or the projectiles couldn't be made, so the caller can fire its own shot
    /// instead.
    /// </summary>
    /// <param name="shot">The shot to fire.</param>
    public static bool Fire(ProjectileShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        if (!Enabled)
            return false;

        var template = ProjectileTemplates.GetForAmmo(shot.AmmoGearName) ?? ProjectileTemplates.GetForAmmo(null);
        if (template == null)
            return false;

        var count = Math.Max(1, shot.Count);
        var spread = Math.Max(0f, shot.Spread);
        var onShotFinished = shot.OnShotFinished;
        var group = onShotFinished is null
            ? null
            : new ProjectileGroup<ProjectileHit>(count, Time.time, shot.ResultDelay, (fired, hits) =>
                ProjectileItem.CallOtherMod(() => onShotFinished(new(fired, hits)), nameof(ProjectileShot.OnShotFinished)));

        for (var i = 0; i < count; i++)
        {
            // Random within a circle, so the projectiles cover the whole spread evenly instead of bunching in the middle.
            var offset = UnityEngine.Random.insideUnitCircle * spread;
            var rotation = shot.Rotation * Quaternion.Euler(-offset.y, offset.x, 0f);
            ProjectileItem.SpawnAndFire(template, shot, rotation, group);
        }

        return true;
    }

    /// <summary>
    /// Finds where a gun's shots start and the direction it's aimed, the way the mod does for rifles and revolvers:
    /// along the line from the rear sight to the front sight, or along the bullet emission point for a gun without
    /// sights.
    /// </summary>
    /// <remarks>
    /// The first-person weapon is drawn by its own camera, so its sights aren't where they appear to be in the world.
    /// This works out where they really point. Hip-fire shots should use the main camera's forward direction instead.
    /// </remarks>
    /// <param name="shooter">The shooter of the gun in the player's hands.</param>
    /// <param name="position">Where its shots start, in world space.</param>
    /// <param name="rotation">The direction it's aimed, in world space.</param>
    /// <returns>Whether the gun has sights or a bullet emission point to work it out from.</returns>
    public static bool TryGetAim(vp_FPSShooter shooter, out Vector3 position, out Quaternion rotation) =>
        ProjectileUtilities.CalculateProjectileTransform(shooter, out position, out rotation);
}
