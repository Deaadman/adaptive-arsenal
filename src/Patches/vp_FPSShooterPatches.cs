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
    /// Each rifle and revolver shooter's bullets, by its Il2Cpp pointer: the game's own and the mod's, which
    /// <see cref="FireProjectile"/> swaps in for each shot.
    /// </summary>
    private static readonly Dictionary<IntPtr, ShooterBullets> s_ShooterBullets = [];

    /// <summary>
    /// Finds each rifle and revolver shooter's bullet template from <see cref="ProjectileTemplates"/>, and remembers it
    /// with the game's own bullet for <see cref="FireProjectile"/>.
    /// </summary>
    /// <remarks>
    /// The shooter's object name says which weapon it's for. Other weapons, like the flare gun and bow, are left as
    /// they are. The shooter keeps the game's bullet: other mods copy it from the game's rifle shooter for guns of
    /// their own, like Shotgun's double barrel, and those must get the game's bullet, not the mod's.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Awake))]
    private static class RememberProjectilePrefab
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

            s_ShooterBullets[__instance.Pointer] = new(__instance.ProjectilePrefab, __instance.ProjectileCustomPrefab, template);
        }
    }

    /// <summary>
    /// Fires a <see cref="ProjectileItem"/> bullet for each rifle or revolver shot, turned away from where it was aimed
    /// by a random amount that grows as the shot's accuracy falls.
    /// </summary>
    /// <remarks>
    /// It only fires when the game is about to: not between shots, while reloading, when the gun is empty or jammed,
    /// or when the player isn't allowed to fire. Hip-fire shots go along the camera's view rather than the sights. The
    /// game's own <see cref="vp_FPSShooter.Fire"/> still runs afterwards, with the mod's bullet swapped into the
    /// shooter so it fires none of its own, and the game's bullet is put back once it's done. With
    /// <see cref="Settings.m_ProjectilesEnabled"/> off, the shooter keeps the game's bullet and this fires nothing.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
    private static class FireProjectile
    {
        // __state is the shooter's bullets to put back, or null when the mod's weren't swapped in. __runOriginal is false
        // when HandleRifleTrigger has blocked the pull with the safety or turned it into cycling the bolt, so nothing
        // fires.
        private static void Prefix(vp_FPSShooter __instance, bool __runOriginal, out ShooterBullets? __state)
        {
            __state = null;
            if (!__runOriginal || !Settings.Instance.m_ProjectilesEnabled || !s_ShooterBullets.TryGetValue(__instance.Pointer, out var bullets))
                return;

            bullets.Use(__instance, true);
            __state = bullets;

            var weapon = __instance.m_Weapon;
            var playerAnimation = GameManager.GetPlayerAnimationComponent();

            if (Time.time < __instance.m_NextAllowedFireTime
                || weapon.ReloadInProgress()
                || !playerAnimation.IsAllowedToFire(weapon.m_GunItem.m_AllowHipFire, weapon.m_GunItem.m_GunType)
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
            var isHipFire = weapon.m_GunItem.m_AllowHipFire && !playerAnimation.IsAllowedToFire(false, weapon.m_GunItem.m_GunType);

            var playerManager = GameManager.GetPlayerManagerComponent();
            var accuracy = ProjectileItem.CalculateAccuracy(weapon.m_GunItem, isHipFire, !playerManager.PlayerIsCrouched(), playerManager.PlayerIsWalking());

            var maxSpread = Ballistics.MaxSpreadDegrees(accuracy);
            var (pitch, yaw) = Ballistics.SpreadOffset(maxSpread, Random.value, Random.value);
            var spread = Quaternion.Euler(pitch, yaw, 0f);

            // The spread turns the shot around its own axes, so it's the same whichever way the player faces.
            var direction = isHipFire ? Quaternion.LookRotation(GameManager.GetVpFPSCamera().transform.forward) : aimRotation;
            ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, weapon.m_GunItem, position, direction * spread);
        }

        private static void Finalizer(vp_FPSShooter __instance, ShooterBullets? __state) =>
            __state?.Use(__instance, false);
    }

    /// <summary>
    /// Applies a rifle's safety with <see cref="RifleSafety"/>, then makes the player cycle its bolt by hand with
    /// <see cref="ManualBolt"/>: with the safety on, the trigger does nothing, and after a shot, the next trigger pull
    /// cycles the bolt instead of firing, and the pull after that fires.
    /// </summary>
    /// <remarks>
    /// It runs before <see cref="FireProjectile"/>, so a pull the safety blocks or that cycles the bolt fires no bullet
    /// either. Revolvers, other weapons, and rifles while <see cref="Settings.m_RifleSafety"/> and
    /// <see cref="Settings.m_ManualBolt"/> are off fire as they would.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
    private static class HandleRifleTrigger
    {
        /// <summary>The trigger, followed from frame to frame for the safety and the bolt to share.</summary>
        private static readonly TriggerPull s_Trigger = new();

        // __state is the rounds in the rifle before the pull, or -1 when ManualBolt isn't tracking it.
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(vp_FPSShooter __instance, out int __state)
        {
            __state = -1;
            s_Trigger.Pull(Time.frameCount);
            return !RifleSafety.BlocksTrigger(__instance, s_Trigger) && ManualBolt.PullTrigger(__instance, s_Trigger, out __state);
        }

        private static void Postfix(int __state) => ManualBolt.AfterTrigger(__state, s_Trigger);
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
