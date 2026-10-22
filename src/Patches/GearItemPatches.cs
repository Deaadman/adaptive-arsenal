using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="GearItem"/>, any item in the world or the player's inventory.</summary>
internal static class GearItemPatches
{
    /// <summary>
    /// Adds how frozen a rifle is after its name in the inventory, as the game does for frozen clothing, like
    /// "Hunting Rifle (Frozen 40%)".
    /// </summary>
    [HarmonyPatch(typeof(GearItem), nameof(GearItem.GetItemPostFixForInventoryInterfaces))]
    private static class ShowHowFrozen
    {
        private static void Postfix(GearItem __instance, ref string __result)
        {
            var percentFrozen = Mathf.FloorToInt(FrozenRifles.GetPercentFrozen(__instance));
            if (percentFrozen < 1)
                return;

            var frozen = $"(Frozen {percentFrozen}%)";
            __result = string.IsNullOrEmpty(__result) ? frozen : $"{__result} {frozen}";
        }
    }
}
