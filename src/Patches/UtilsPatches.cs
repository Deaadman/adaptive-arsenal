using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on the game's <see cref="Utils"/>.</summary>
internal static class UtilsPatches
{
    /// <summary>
    /// Shows a skinned weapon's skin icon instead of its own, wherever the game shows the icon of a particular item,
    /// like the inventory, the radial menu and the item's description.
    /// </summary>
    /// <remarks>
    /// Harmony merges several <see cref="HarmonyPatch"/> attributes on one class into a single target, so the
    /// overloads are listed in <see cref="TargetMethods"/> instead.
    /// </remarks>
    [HarmonyPatch]
    private static class ShowSkinIcon
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(Utils)).Where(method =>
                method.Name == nameof(Utils.GetInventoryIconTexture)
                && method.GetParameters() is [{ ParameterType: var type }, ..] && type == typeof(GearItem));

        // __0 is the GearItem, whatever each overload names it.
        private static void Postfix(GearItem __0, ref Texture2D __result)
        {
            if (__0 != null && WeaponSkins.TryGetIcon(__0, out var icon))
                __result = icon;
        }
    }
}
