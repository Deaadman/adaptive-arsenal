using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="PlayerManager"/>, which handles what the player holds, for rifles' safety.</summary>
internal static class PlayerManagerPatches
{
    /// <summary>Sets a rifle's safety with <see cref="RifleSafety"/> each time the player equips it.</summary>
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.EquipItem))]
    private static class SetSafetyOnEquip
    {
        private static void Postfix(GearItem gi) => RifleSafety.Equipped(gi);
    }
}
