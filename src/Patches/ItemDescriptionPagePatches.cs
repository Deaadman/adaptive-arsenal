using AdaptiveArsenal.Components;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="ItemDescriptionPage"/>, the details of the item selected in the inventory and other item
/// lists, to show a <see cref="SkinPicker"/> for weapons with skins.
/// </summary>
internal static class ItemDescriptionPagePatches
{
    /// <summary>Shows the skin picker for the item the page now shows, building it the first time.</summary>
    [HarmonyPatch(typeof(ItemDescriptionPage), nameof(ItemDescriptionPage.UpdateGearItemDescription))]
    private static class ShowSkinPicker
    {
        private static void Postfix(ItemDescriptionPage __instance, GearItem gi)
        {
            var picker = __instance.GetComponent<SkinPicker>();
            if (picker == null)
            {
                picker = __instance.gameObject.AddComponent<SkinPicker>();
                if (!picker.Build(__instance))
                    Mod.Logger.Log("The item description page has nothing to build the skin picker from.", FlaggedLoggingLevel.Warning);
            }

            picker.Show(gi);
        }
    }

    /// <summary>Hides the skin picker when the page shows a decoration rather than an item.</summary>
    [HarmonyPatch(typeof(ItemDescriptionPage), nameof(ItemDescriptionPage.UpdateDecorationItemDescription))]
    private static class HideSkinPickerForDecorations
    {
        private static void Postfix(ItemDescriptionPage __instance)
        {
            var picker = __instance.GetComponent<SkinPicker>();
            if (picker != null)
                picker.Hide();
        }
    }
}
