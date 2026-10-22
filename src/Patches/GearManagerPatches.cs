using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>Patches on <see cref="GearManager"/>, which loads the items lying around a scene from the save.</summary>
internal static class GearManagerPatches
{
    /// <summary>Shows the saved skins of the weapons lying in a scene, once every item in it has loaded.</summary>
    /// <remarks>
    /// <see cref="GearItemPatches"/> also shows a weapon's skin as each item loads, but weapons on the ground loaded
    /// with a scene came back without theirs, so they're shown again here, after the scene's items are all set up.
    /// </remarks>
    [HarmonyPatch(typeof(GearManager), nameof(GearManager.Deserialize), [typeof(GearSaveList), typeof(Il2CppSystem.Collections.Generic.List<GearItem>)])]
    private static class ApplySavedSkins
    {
        private static void Postfix(Il2CppSystem.Collections.Generic.List<GearItem> loadedItems)
        {
            if (loadedItems == null)
                return;

            var skinned = 0;
            foreach (var gearItem in loadedItems)
            {
                if (gearItem == null)
                    continue;

                WeaponSkins.ApplyToModel(gearItem);
                if (WeaponSkins.GetSkinId(gearItem) != null)
                    skinned++;
            }

            if (skinned > 0)
                Mod.Logger.Log($"Showed the skins of {skinned} weapons loaded in the scene.", FlaggedLoggingLevel.Debug);
        }
    }
}
