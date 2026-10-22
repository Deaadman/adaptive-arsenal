using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on the game's <see cref="Utils"/>.</summary>
internal static class UtilsPatches
{
    /// <summary>
    /// Shows a skinned weapon's skin icon instead of its own, wherever the game shows the icon of a particular item,
    /// like the inventory, the radial menu and the item's description.
    /// </summary>
    [HarmonyPatch(typeof(Utils), nameof(Utils.GetInventoryIconTexture), [typeof(GearItem)])]
    [HarmonyPatch(typeof(Utils), nameof(Utils.GetInventoryIconTexture), [typeof(GearItem), typeof(float)])]
    private static class ShowSkinIcon
    {
        private static void Postfix(GearItem gi, ref Texture2D __result)
        {
            if (gi != null && WeaponSkins.TryGetIcon(gi, out var icon))
                __result = icon;
        }
    }
}
