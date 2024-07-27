using AdaptiveArsenal.Components;
using AdaptiveArsenal.Utilities;
using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal.Patches;

internal static class vp_FPSShooterPatches
{
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Awake))]
    private static class SwapToProjectiles
    {
        private static void Prefix(vp_FPSShooter __instance)
        {
            string? gearItem = null;

            if (__instance.gameObject.name.Contains("Rifle")) gearItem = "GEAR_RifleAmmoSingle";
            else if (__instance.gameObject.name.Contains("Revolver")) gearItem = "GEAR_RevolverAmmoSingle";

            if (gearItem == null) return;
            
            var newProjectilePrefab = Addressables.LoadAsset<GameObject>(gearItem).WaitForCompletion();
            if (newProjectilePrefab == null) return;
            
            _ = newProjectilePrefab.GetComponent<ProjectileItem>() ?? newProjectilePrefab.AddComponent<ProjectileItem>();
            __instance.ProjectileCustomPrefab = true;
            __instance.ProjectilePrefab = newProjectilePrefab;
        }
    }
    
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
    private static class FireProjectile
    {
        private static void Prefix(vp_FPSShooter __instance)
        {
            if (Time.time < __instance.m_NextAllowedFireTime || __instance.m_Weapon.ReloadInProgress() 
                                                             || !GameManager.GetPlayerAnimationComponent().IsAllowedToFire(__instance.m_Weapon.m_GunItem.m_AllowHipFire) 
                                                             || GameManager.GetPlayerAnimationComponent().IsReloading() 
                                                             || __instance.m_Weapon.GetAmmoCount() < 1
                                                             || __instance.m_Weapon.m_GunItem.m_IsJammed) return;
            if (!__instance.ProjectilePrefab.GetComponent<ProjectileItem>()) return;
            
            ProjectileUtilities.SetBulletEmissionLocator(__instance);
            ProjectileUtilities.CalculateProjectileTransform(__instance, out var position, out var rotation);

            if (__instance.m_Weapon.m_GunItem.m_AllowHipFire && !GameManager.GetPlayerAnimationComponent().IsAllowedToFire(false))
            {
                ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, position, Quaternion.LookRotation(GameManager.GetVpFPSCamera().transform.forward));
            }
            else
            {
                ProjectileItem.SpawnAndFire(__instance.ProjectilePrefab, position, rotation);
            }
        }
    }
}