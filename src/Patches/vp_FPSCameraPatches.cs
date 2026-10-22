using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSCamera"/>, which shows the first-person model of the item in the player's hands.
/// </summary>
internal static class vp_FPSCameraPatches
{
    /// <summary>
    /// Shows the skin of the weapon being equipped on its first-person model, or its own look. Every weapon of a kind
    /// shares the first-person model, so this runs on every equip.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSCamera), nameof(vp_FPSCamera.SetWeapon))]
    private static class ApplySkinOnEquip
    {
        private static void Postfix(vp_FPSCamera __instance, GearItem gearItem)
        {
            if (gearItem != null && __instance.CurrentWeapon != null)
                WeaponSkins.ApplyToFirstPerson(__instance.CurrentWeapon, gearItem);
        }
    }
}
