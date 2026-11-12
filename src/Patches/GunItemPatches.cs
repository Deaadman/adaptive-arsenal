using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="GunItem"/>, a gun's rounds and handling, for cycling a rifle's bolt by hand with
/// <see cref="ManualBolt"/>.
/// </summary>
internal static class GunItemPatches
{
    /// <summary>
    /// Gives each bolt-action rifle room for a round in the chamber on top of its magazine, so a full rifle holds one
    /// more round than the game gives it.
    /// </summary>
    /// <remarks>
    /// Only while <see cref="Settings.m_ManualBolt"/> is on when the rifle is created, so changing the setting applies
    /// to rifles from the next time the save loads. The capacity comes from the first rifle of each kind, before it's
    /// changed, so a rifle copied from one that already has the extra round doesn't get another.
    /// </remarks>
    [HarmonyPatch(typeof(GunItem), nameof(GunItem.Awake))]
    private static class AddChamberRound
    {
        /// <summary>Each kind of rifle's magazine size, by its gear name without <c>(Clone)</c>.</summary>
        private static readonly Dictionary<string, int> s_MagazineSizes = [];

        private static void Postfix(GunItem __instance)
        {
            if (!ManualBolt.AppliesTo(__instance))
                return;

            var gearName = __instance.name.Replace("(Clone)", "").Trim();
            if (!s_MagazineSizes.TryGetValue(gearName, out var magazineSize))
            {
                magazineSize = __instance.m_ClipSize;
                s_MagazineSizes[gearName] = magazineSize;
            }

            __instance.m_ClipSize = BoltCycle.CapacityWithChamber(magazineSize);
        }
    }

    /// <summary>Lets a held bolt cycle when the player stops aiming, so lowering the rifle plays its animation.</summary>
    [HarmonyPatch(typeof(GunItem), nameof(GunItem.ZoomEnd))]
    private static class CycleBoltWhenLowered
    {
        private static void Postfix() => ManualBolt.Release();
    }
}
