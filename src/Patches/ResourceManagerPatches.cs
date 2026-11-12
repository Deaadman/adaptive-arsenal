using AdaptiveArsenal.Utilities;

using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on Addressables' <see cref="ResourceManager"/>, so the game can load the objects in
/// <see cref="RuntimeAddressables"/> by key like its own.
/// </summary>
internal static class ResourceManagerPatches
{
    /// <summary>
    /// Hands back the object for one of the mod's keys, instead of loading it from a bundle, which it isn't in.
    /// </summary>
    [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.ProvideResource),
        [typeof(IResourceLocation), typeof(Il2CppSystem.Type), typeof(bool)])]
    private static class ProvideRuntimeAsset
    {
        private static bool Prefix(ResourceManager __instance, IResourceLocation location, Il2CppSystem.Type desiredType,
            ref AsyncOperationHandle __result)
        {
            if (location == null || !RuntimeAddressables.TryProvide(__instance, location.PrimaryKey, desiredType, out var handle))
                return true;

            __result = handle;
            return false;
        }
    }
}
