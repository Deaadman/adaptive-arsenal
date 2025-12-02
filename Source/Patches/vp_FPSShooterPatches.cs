using AdaptiveArsenal.Components;
using AdaptiveArsenal.Utilities;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace AdaptiveArsenal.Patches
{
    /// <summary>
    /// Provides Harmony patches for modifying the behavior of the <see cref="vp_FPSShooter"/> class, including adjustments
    /// to projectile spawning and firing logic.
    /// </summary>
    /// <remarks>This class contains static nested classes that implement specific patches for the <see
    /// cref="vp_FPSShooter"/> methods. These patches introduce custom logic for handling projectile prefabs and firing
    /// mechanics.</remarks>
    internal static class vp_FPSShooterPatches
    {
        /// <summary>
        /// A Harmony patch that modifies the behavior of the <see cref="vp_FPSShooter"/> class during its <see
        /// cref="vp_FPSShooter.Awake"/> method to swap the projectile prefab based on the associated weapon
        /// type.
        /// </summary>
        /// <remarks>This patch checks the name of the <see cref="GameObject"/> associated with the <see
        /// cref="vp_FPSShooter"/> instance to determine if it corresponds to a rifle or revolver. If a match is found, it
        /// loads the appropriate projectile prefab and assigns it to the <see cref="vp_FPSShooter.ProjectilePrefab"/>
        /// property.</remarks>
        [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Awake))]
        private static class SwapToProjectiles
        {
            private static void Prefix(vp_FPSShooter __instance)
            {
                string? gearItem = null;

                if (__instance.gameObject.name.Contains("Rifle"))
                    gearItem = "GEAR_RifleAmmoSingle";
                else if (__instance.gameObject.name.Contains("Revolver"))
                    gearItem = "GEAR_RevolverAmmoSingle";

                if (gearItem == null) return;

                GameObject newProjectilePrefab = Addressables.LoadAsset<GameObject>(gearItem).WaitForCompletion();

                if (newProjectilePrefab == null) return;

                _ = newProjectilePrefab.GetComponent<ProjectileItem>() 
                    ?? newProjectilePrefab.AddComponent<ProjectileItem>();
                __instance.ProjectileCustomPrefab = true;
                __instance.ProjectilePrefab = newProjectilePrefab;
            }
        }
    
        /// <summary>
        /// Modifies the behavior of the <see cref="vp_FPSShooter.Fire"/> method to introduce additional checks and
        /// calculations for firing projectiles, including accuracy adjustments and projectile spawning logic.
        /// </summary>
        /// <remarks>This patch ensures that projectiles are only fired under specific conditions, such as when
        /// the weapon is not jammed, the player is allowed to fire, and sufficient ammunition is available. It also
        /// calculates the projectile's accuracy based on the player's stance and firing mode (hip fire or aimed fire)</remarks>
        [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
        private static class FireProjectile
        {
            private static void Prefix(vp_FPSShooter __instance)
            {
                if (Time.time < __instance.m_NextAllowedFireTime ||
                    __instance.m_Weapon.ReloadInProgress() ||
                    !GameManager.GetPlayerAnimationComponent().IsAllowedToFire(__instance.m_Weapon.m_GunItem.m_AllowHipFire) ||
                    GameManager.GetPlayerAnimationComponent().IsReloading() ||
                    __instance.m_Weapon.GetAmmoCount() < 1 ||
                    __instance.m_Weapon.m_GunItem.m_IsJammed ||
                    __instance.ProjectilePrefab == null)
                {
                    return;
                }

                if (!__instance.ProjectilePrefab.GetComponent<ProjectileItem>())
                    return;

                ProjectileUtilities.SetBulletEmissionLocator(__instance);
                ProjectileUtilities.CalculateProjectileTransform(__instance, out var position, out var rotation);

                bool isHipFire = __instance.m_Weapon.m_GunItem.m_AllowHipFire && !GameManager.GetPlayerAnimationComponent().IsAllowedToFire(false);
            
                float accuracy = ProjectileItem.CalculateAccuracy(__instance.m_Weapon.m_GunItem, isHipFire, !GameManager.GetPlayerManagerComponent().PlayerIsCrouched(), GameManager.GetPlayerManagerComponent().PlayerIsWalking());

                float inaccuracyAngle = Mathf.Lerp(0f, 10f, 1f - accuracy / 100f);

                Vector3 randomRotation = new Vector3(Random.Range(-inaccuracyAngle, inaccuracyAngle), Random.Range(-inaccuracyAngle, inaccuracyAngle), 0);

                if (isHipFire)
                    ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, position, Quaternion.Euler(randomRotation) * Quaternion.LookRotation(GameManager.GetVpFPSCamera().transform.forward));
                else
                    ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, position, rotation * Quaternion.Euler(randomRotation));
            }
        }
    }
}