using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="SaveGameSystem"/>, which saves and loads the player's game, to save the mod's data with it.
/// </summary>
internal static class SaveGameSystemPatches
{
    /// <summary>Saves each weapon's data with the game, whenever the game saves what isn't tied to a scene.</summary>
    /// <remarks>
    /// ModData writes its file after the game writes the save to disk, so the data handed to it here goes into the same
    /// save.
    /// </remarks>
    [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveGlobalData))]
    private static class SaveWeaponData
    {
        private static void Postfix() => WeaponDataManager.Save();
    }
}
