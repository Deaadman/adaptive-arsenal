using AdaptiveArsenal.Components;
using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

using UnityEngine.AddressableAssets;

using Random = UnityEngine.Random;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSShooter"/>, which fires the weapon in the player's hands, to fire
/// <see cref="ProjectileItem"/> bullets from rifles and revolvers.
/// </summary>
internal static class vp_FPSShooterPatches
{
    /// <summary>
    /// Gives each rifle and revolver shooter a bullet prefab: the single round of its ammo, with a
    /// <see cref="ProjectileItem"/> added.
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
            string? ammoGearName = null;

            if (__instance.gameObject.name.Contains("Rifle"))
                ammoGearName = "GEAR_RifleAmmoSingle";
            else if (__instance.gameObject.name.Contains("Revolver"))
                ammoGearName = "GEAR_RevolverAmmoSingle";

            if (ammoGearName == null)
                return;

            var projectilePrefab = Addressables.LoadAssetAsync<GameObject>(ammoGearName).WaitForCompletion();
            if (projectilePrefab == null)
                return;

            _ = projectilePrefab.GetComponent<ProjectileItem>() ?? projectilePrefab.AddComponent<ProjectileItem>();
            __instance.ProjectileCustomPrefab = true;
            __instance.ProjectilePrefab = projectilePrefab;
        }
    }

    /// <summary>
    /// Fires a <see cref="ProjectileItem"/> bullet for each rifle or revolver shot, turned away from where it was aimed
    /// by a random amount that grows as the shot's accuracy falls.
    /// </summary>
    /// <remarks>
    /// It only fires when the game is about to: not between shots, while reloading, when the gun is empty or jammed,
    /// or when the player isn't allowed to fire. Hip-fire shots go along the camera's view rather than the sights. The
    /// game's own <see cref="vp_FPSShooter.Fire"/> still runs afterwards.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
    private static class FireProjectile
    {
        private static void Prefix(vp_FPSShooter __instance)
        {
            var weapon = __instance.m_Weapon;
            var playerAnimation = GameManager.GetPlayerAnimationComponent();

            if (Time.time < __instance.m_NextAllowedFireTime
                || weapon.ReloadInProgress()
                || !playerAnimation.IsAllowedToFire(weapon.m_GunItem.m_AllowHipFire)
                || playerAnimation.IsReloading()
                || weapon.GetAmmoCount() < 1
                || weapon.m_GunItem.m_IsJammed
                || __instance.ProjectilePrefab == null)
            {
                return;
            }

            if (!__instance.ProjectilePrefab.GetComponent<ProjectileItem>())
                return;

            ProjectileUtilities.SetBulletEmissionLocator(__instance);
            ProjectileUtilities.CalculateProjectileTransform(__instance, out var position, out var aimRotation);

            // Hip fire is when the gun allows it but the player isn't aiming.
            var isHipFire = weapon.m_GunItem.m_AllowHipFire && !playerAnimation.IsAllowedToFire(false);

            var playerManager = GameManager.GetPlayerManagerComponent();
            var accuracy = ProjectileItem.CalculateAccuracy(weapon.m_GunItem, isHipFire, !playerManager.PlayerIsCrouched(), playerManager.PlayerIsWalking());

            var maxSpread = Ballistics.MaxSpreadDegrees(accuracy);
            var spread = Quaternion.Euler(new Vector3(Random.Range(-maxSpread, maxSpread), Random.Range(-maxSpread, maxSpread), 0));

            if (isHipFire)
                ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, position, spread * Quaternion.LookRotation(GameManager.GetVpFPSCamera().transform.forward));
            else
                ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, position, aimRotation * spread);
        }
    }
}
