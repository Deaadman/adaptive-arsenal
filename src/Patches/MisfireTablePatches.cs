using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="MisfireTable"/>, which gives a gun's chance of misfiring, for rifles' misfires with
/// <see cref="ManualBolt"/>.
/// </summary>
internal static class MisfireTablePatches
{
    /// <summary>
    /// Stops the game rolling a misfire for a rifle shot whose misfire <see cref="ManualBolt"/> has already rolled, so
    /// each shot rolls once.
    /// </summary>
    /// <remarks>
    /// It blocks the original, since the original is the roll. Everything else, including <see cref="ManualBolt"/>'s
    /// own roll, rolls as the game would.
    /// </remarks>
    [HarmonyPatch(typeof(MisfireTable), nameof(MisfireTable.DoesMisfire))]
    private static class RollOncePerRifleShot
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ManualBolt.SuppressGameMisfire)
                return true;

            __result = false;
            return false;
        }
    }
}
