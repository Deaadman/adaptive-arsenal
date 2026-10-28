using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="GearItem"/>, any item in the world or the player's inventory.</summary>
internal static class GearItemPatches
{
    /// <summary>
    /// Gives each gun an instance ID no other gun has had in the player's game, so a new gun isn't handed the
    /// <see cref="Core.WeaponData"/> of one saved with the same ID in an earlier session.
    /// </summary>
    /// <remarks>
    /// The game sets <see cref="GearItem.m_InstanceID"/> from Unity's instance ID here, which nothing has read yet. A gun
    /// loaded from a save gets its saved ID back in <see cref="GearItem.Deserialize"/>, after this.
    /// </remarks>
    [HarmonyPatch(typeof(GearItem), nameof(GearItem.Awake))]
    private static class GiveGunsUniqueIds
    {
        private static void Postfix(GearItem __instance)
        {
            if (__instance.m_GunItem != null)
                __instance.m_InstanceID = WeaponDataManager.NewInstanceId();
        }
    }

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
