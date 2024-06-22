using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal;

internal static class InitializeComponents
{
    [HarmonyPatch(typeof(GearItem), nameof(GearItem.Awake))]
    private static class ApplyGunExtensionComponent
    {
        private static void Postfix(GearItem __instance)
        {
            if (__instance.GetComponent<GunItem>() != null)
            {
                _ = __instance.gameObject.GetComponent<GunExtension>() ?? __instance.gameObject.AddComponent<GunExtension>();
            }
        }
    }

    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Awake))]
    private static class SwapRaycastsToProjectiles
    {
        private static void Prefix(vp_FPSShooter __instance)
        {
            string? gearItem = null;

            if (__instance.gameObject.name.Contains("Rifle"))
                gearItem = "GEAR_RifleAmmoSingle";
            else if (__instance.gameObject.name.Contains("Revolver"))
                gearItem = "GEAR_RevolverAmmoSingle";

            if (gearItem == null) return;
            var newProjectilePrefab = Addressables.LoadAsset<GameObject>(gearItem).WaitForCompletion();
            if (newProjectilePrefab == null) return;
            _ = newProjectilePrefab.GetComponent<AmmoProjectile>() ?? newProjectilePrefab.AddComponent<AmmoProjectile>();
            __instance.ProjectileCustomPrefab = true;
            __instance.ProjectilePrefab = newProjectilePrefab;
        }
    }
}