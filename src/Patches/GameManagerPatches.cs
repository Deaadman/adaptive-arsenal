using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="GameManager"/>, which loads saves and the main menu, to load and forget the mod's data with
/// the player's game.
/// </summary>
internal static class GameManagerPatches
{
    /// <summary>Loads each weapon's data when the player loads a save, before its scene and items load.</summary>
    /// <remarks>
    /// ModData opens the save's file in a prefix on the same methods, so it's ready by this postfix. The scene loads
    /// after these return, so the data is there when the weapons in it are set up.
    /// </remarks>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), [typeof(string), typeof(int)])]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), [typeof(SaveSlotInfo)])]
    private static class LoadWeaponData
    {
        private static void Postfix()
        {
            WeaponDataManager.Load();
            FrozenRifles.Reset();
        }
    }

    /// <summary>Forgets each weapon's data when the player leaves their game, so it doesn't carry into the next one.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoExitToMainMenu))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadMainMenu))]
    private static class ClearWeaponData
    {
        private static void Postfix()
        {
            WeaponDataManager.Clear();
            FrozenRifles.Reset();
        }
    }
}
