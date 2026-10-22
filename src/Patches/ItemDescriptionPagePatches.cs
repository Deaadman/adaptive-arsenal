using AdaptiveArsenal.Components;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="ItemDescriptionPage"/>, the details of the item selected in the inventory and other item
/// lists, to show the <see cref="SkinsPanel"/>'s buttons for weapons with skins.
/// </summary>
internal static class ItemDescriptionPagePatches
{
    /// <summary>Tells the skins panel which item the page now shows, building it the first time.</summary>
    [HarmonyPatch(typeof(ItemDescriptionPage), nameof(ItemDescriptionPage.UpdateGearItemDescription))]
    private static class ShowItemInSkinsPanel
    {
        private static void Postfix(ItemDescriptionPage __instance, GearItem gi)
        {
            var panel = SkinsPanel.GetOrAdd(__instance);
            if (panel != null)
                panel.ShowItem(gi);
        }
    }

    /// <summary>Hides SKINS when the page shows a decoration rather than an item.</summary>
    [HarmonyPatch(typeof(ItemDescriptionPage), nameof(ItemDescriptionPage.UpdateDecorationItemDescription))]
    private static class HideSkinsForDecorations
    {
        private static void Postfix(ItemDescriptionPage __instance)
        {
            var panel = SkinsPanel.Find(__instance);
            if (panel != null)
                panel.ShowItem(null);
        }
    }

    /// <summary>Shows SKINS, or CANCEL and SAVE, after the game sets the page's buttons, which it does often.</summary>
    [HarmonyPatch(typeof(ItemDescriptionPage), nameof(ItemDescriptionPage.UpdateButtons))]
    private static class ShowSkinsButtons
    {
        private static void Postfix(ItemDescriptionPage __instance)
        {
            var panel = SkinsPanel.Find(__instance);
            if (panel != null)
                panel.UpdateButtons();
        }
    }
}
