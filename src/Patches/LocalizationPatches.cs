using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="Localization"/>, to give the mod's own items their names and descriptions.</summary>
internal static class LocalizationPatches
{
    /// <summary>Gives the text for the <see cref="TracerAmmo"/> cartridge's keys, which the game's tables don't have.</summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.Get), [typeof(string)])]
    private static class TracerAmmoText
    {
        private static void Postfix(string key, ref string __result)
        {
            if (TracerAmmo.TryGetText(key, out var text))
                __result = text;
        }
    }
}
