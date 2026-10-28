using AdaptiveArsenal.Components;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSCamera"/>, which shows the first-person model of the item in the player's hands.
/// </summary>
internal static class vp_FPSCameraPatches
{
    /// <summary>
    /// Puts <see cref="LeftHandedView"/> on the weapon camera, which mirrors the first-person view when the player is
    /// left-handed. It's also tried on each equip, in case the weapon camera wasn't there when the camera started.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSCamera), nameof(vp_FPSCamera.Start))]
    [HarmonyPatch(typeof(vp_FPSCamera), nameof(vp_FPSCamera.SetWeapon))]
    private static class AttachLeftHandedView
    {
        private static void Postfix(vp_FPSCamera __instance) => LeftHandedView.AttachTo(__instance.GetWeaponCamera());
    }
}
