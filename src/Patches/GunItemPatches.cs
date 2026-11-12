using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="GunItem"/>, to let rifles load <see cref="TracerAmmo"/> cartridges.</summary>
internal static class GunItemPatches
{
    /// <summary>
    /// Adds the tracer cartridge to each rifle's ammo types before the rifle loads them, so the game's own ammo
    /// switching can choose it.
    /// </summary>
    [HarmonyPatch(typeof(GunItem), nameof(GunItem.Awake))]
    private static class AddTracerAmmo
    {
        private static void Prefix(GunItem __instance) => TracerAmmo.AddToRifle(__instance);
    }
}
