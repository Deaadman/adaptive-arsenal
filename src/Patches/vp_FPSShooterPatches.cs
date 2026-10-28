using AdaptiveArsenal.Components;
using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

using Random = UnityEngine.Random;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSShooter"/>, which fires the weapon in the player's hands, to fire
/// <see cref="ProjectileItem"/> bullets from rifles and revolvers.
/// </summary>
internal static class vp_FPSShooterPatches
{
    /// <summary>
    /// Each rifle and revolver shooter's bullets, by its Il2Cpp pointer: the game's own and the mod's, to switch
    /// between when <see cref="Settings.m_ProjectilesEnabled"/> changes.
    /// </summary>
    private static readonly Dictionary<IntPtr, ShooterBullets> s_ShooterBullets = [];

    /// <summary>
    /// Gives each rifle and revolver shooter its bullet template from <see cref="ProjectileTemplates"/>, remembering the
    /// game's own bullet so <see cref="FireProjectile"/> can switch back to it.
    /// </summary>
    /// <remarks>
    /// The shooter's object name says which weapon it's for. Other weapons, like the flare gun and bow, are left as
    /// they are.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Awake))]
    private static class UseProjectilePrefab
    {
        private static void Prefix(vp_FPSShooter __instance)
        {
            GunType gunType;

            if (__instance.gameObject.name.Contains("Rifle"))
                gunType = GunType.Rifle;
            else if (__instance.gameObject.name.Contains("Revolver"))
                gunType = GunType.Revolver;
            else
                return;

            // The game's own bullet, before it's replaced, says how hard a shot pushes loose gear.
            var gameBullet = __instance.ProjectilePrefab != null ? __instance.ProjectilePrefab.GetComponent<vp_Bullet>() : null;
            if (gameBullet != null)
                ProjectileTemplates.RememberGearImpactForce(gunType, gameBullet);

            var template = ProjectileTemplates.Get(gunType);
            if (template == null)
            {
                Mod.Logger.Log($"{__instance.gameObject.name} has no bullet, so it fires the game's own shots.", FlaggedLoggingLevel.Warning);
                return;
            }

            ShooterBullets bullets = new(__instance.ProjectilePrefab, __instance.ProjectileCustomPrefab, template);
            s_ShooterBullets[__instance.Pointer] = bullets;
            bullets.Use(__instance, Settings.Instance.m_ProjectilesEnabled);
        }
    }

    /// <summary>
    /// Fires a <see cref="ProjectileItem"/> bullet for each rifle or revolver shot, turned away from where it was aimed
    /// by a random amount that grows as the shot's accuracy falls.
    /// </summary>
    /// <remarks>
    /// It only fires when the game is about to: not between shots, while reloading, when the gun is empty or jammed,
    /// or when the player isn't allowed to fire. Hip-fire shots go along the camera's view rather than the sights. The
    /// game's own <see cref="vp_FPSShooter.Fire"/> still runs afterwards. With
    /// <see cref="Settings.m_ProjectilesEnabled"/> off, it puts the game's own bullet back and fires nothing itself.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
    private static class FireProjectile
    {
        private static void Prefix(vp_FPSShooter __instance)
        {
            if (!s_ShooterBullets.TryGetValue(__instance.Pointer, out var bullets))
                return;

            var projectilesEnabled = Settings.Instance.m_ProjectilesEnabled;
            bullets.Use(__instance, projectilesEnabled);
            if (!projectilesEnabled)
                return;

            var weapon = __instance.m_Weapon;
            var playerAnimation = GameManager.GetPlayerAnimationComponent();

            if (Time.time < __instance.m_NextAllowedFireTime
                || weapon.ReloadInProgress()
                || !GameCompatibility.IsAllowedToFire(playerAnimation, weapon.m_GunItem.m_AllowHipFire, weapon.m_GunItem.m_GunType)
                || playerAnimation.IsReloading()
                || weapon.GetAmmoCount() < 1
                || weapon.m_GunItem.m_IsJammed
                || __instance.ProjectilePrefab == null)
            {
                return;
            }

            if (!__instance.ProjectilePrefab.GetComponent<ProjectileItem>())
                return;

            if (!ProjectileUtilities.CalculateProjectileTransform(__instance, out var position, out var aimRotation))
            {
                Mod.Logger.Log($"{__instance.gameObject.name} has no sights or bullet emission point, so it fires no bullet.", FlaggedLoggingLevel.Warning);
                return;
            }

            // Hip fire is when the gun allows it but the player isn't aiming.
            var isHipFire = weapon.m_GunItem.m_AllowHipFire && !GameCompatibility.IsAllowedToFire(playerAnimation, false, weapon.m_GunItem.m_GunType);

            var playerManager = GameManager.GetPlayerManagerComponent();
            var accuracy = ProjectileItem.CalculateAccuracy(weapon.m_GunItem, isHipFire, !playerManager.PlayerIsCrouched(), playerManager.PlayerIsWalking());

            var maxSpread = Ballistics.MaxSpreadDegrees(accuracy);
            var (pitch, yaw) = Ballistics.SpreadOffset(maxSpread, Random.value, Random.value);
            var spread = Quaternion.Euler(pitch, yaw, 0f);

            // The spread turns the shot around its own axes, so it's the same whichever way the player faces.
            var direction = isHipFire ? Quaternion.LookRotation(GameManager.GetVpFPSCamera().transform.forward) : aimRotation;
            ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, weapon.m_GunItem, position, direction * spread);
        }
    }

    /// <summary>A shooter's bullets: the game's own, and the mod's template.</summary>
    /// <param name="GamePrefab">The shooter's own <see cref="vp_FPSShooter.ProjectilePrefab"/>.</param>
    /// <param name="GameCustomPrefab">The shooter's own <see cref="vp_FPSShooter.ProjectileCustomPrefab"/>.</param>
    /// <param name="Template">The mod's bullet template for the shooter's gun.</param>
    private readonly record struct ShooterBullets(GameObject? GamePrefab, bool GameCustomPrefab, GameObject Template)
    {
        /// <summary>Gives the shooter the mod's bullet or the game's own.</summary>
        /// <param name="shooter">The shooter.</param>
        /// <param name="projectiles">Whether to use the mod's bullet.</param>
        internal void Use(vp_FPSShooter shooter, bool projectiles)
        {
            shooter.ProjectilePrefab = projectiles ? Template : GamePrefab;
            shooter.ProjectileCustomPrefab = projectiles || GameCustomPrefab;
        }
    }
}
