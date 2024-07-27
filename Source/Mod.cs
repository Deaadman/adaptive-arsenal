using AdaptiveArsenal.Components;
using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal;

internal sealed class Mod : MelonMod
{
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Awake))]
    private static class SwapRaycastsToProjectiles
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
}