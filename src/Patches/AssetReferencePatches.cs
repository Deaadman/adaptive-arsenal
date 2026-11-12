using AdaptiveArsenal.Utilities;

using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on Addressables' <see cref="AssetReference"/>, so the game accepts references to the objects in
/// <see cref="RuntimeAddressables"/>.
/// </summary>
internal static class AssetReferencePatches
{
    /// <summary>
    /// Counts the mod's keys as valid. The game checks a reference's key before loading it, and expects one from its
    /// own catalog, which the mod's aren't.
    /// </summary>
    [HarmonyPatch(typeof(AssetReference), nameof(AssetReference.RuntimeKeyIsValid))]
    private static class AcceptRuntimeKeys
    {
        private static bool Prefix(AssetReference __instance, ref bool __result)
        {
            if (!RuntimeAddressables.Contains(__instance.RuntimeKey?.ToString()))
                return true;

            __result = true;
            return false;
        }
    }
}
