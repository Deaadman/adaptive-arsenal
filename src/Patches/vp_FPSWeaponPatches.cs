using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSWeapon"/>, the first-person weapon in the player's hands, for rifles' duds with
/// <see cref="ManualBolt"/>.
/// </summary>
internal static class vp_FPSWeaponPatches
{
    /// <summary>Clears a rifle's dud once it's reloaded, since reloading opens the bolt.</summary>
    [HarmonyPatch(typeof(vp_FPSWeapon), nameof(vp_FPSWeapon.OnReloadComplete))]
    private static class ClearDudOnReload
    {
        private static void Postfix(vp_FPSWeapon __instance) => ManualBolt.Reloaded(__instance.m_GunItem);
    }
}
