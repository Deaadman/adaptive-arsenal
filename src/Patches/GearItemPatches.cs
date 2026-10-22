using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="GearItem"/>, any item in the world or the player's inventory.</summary>
internal static class GearItemPatches
{
    /// <summary>Shows a weapon's saved skin on its model when the weapon is loaded from a save.</summary>
    /// <remarks>
    /// Not in <see cref="GearItem.Awake"/>: the item gets a new <see cref="GearItem.m_InstanceID"/> there, and only gets
    /// its saved one, which its skin is saved under, here.
    /// </remarks>
    [HarmonyPatch(typeof(GearItem), nameof(GearItem.Deserialize))]
    private static class ApplySavedSkin
    {
        private static void Postfix(GearItem __instance) => WeaponSkins.ApplyToModel(__instance);
    }
}
