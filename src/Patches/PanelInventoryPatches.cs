using AdaptiveArsenal.Components;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="Panel_Inventory"/>, to keep the inventory from undoing or acting around the
/// <see cref="SkinsPanel"/> while it's open.
/// </summary>
internal static class PanelInventoryPatches
{
    /// <summary>Keeps the skin shown in the item details, rather than the selected item, while the panel is open.</summary>
    [HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.UpdateGearStatsBlock))]
    private static class KeepSkinDetails
    {
        private static bool Prefix(Panel_Inventory __instance)
        {
            var panel = SkinsPanel.Find(__instance);
            return panel == null || !panel.IsOpen;
        }
    }

    /// <summary>Hides again the parts of the inventory that refreshing it shows, while the panel is open.</summary>
    [HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.RefreshTable))]
    private static class KeepInventoryHidden
    {
        private static void Postfix(Panel_Inventory __instance)
        {
            var panel = SkinsPanel.Find(__instance);
            if (panel != null)
                panel.HideInventoryAgain();
        }
    }

    /// <summary>
    /// Ignores the inventory's keys, like the one for dropping an item, while the panel is open, apart from Escape,
    /// which cancels.
    /// </summary>
    [HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.UpdateButtonTriggers))]
    private static class CancelOnEscape
    {
        private static bool Prefix(Panel_Inventory __instance)
        {
            var panel = SkinsPanel.Find(__instance);
            if (panel == null || !panel.IsOpen)
                return true;

            if (InputManager.GetEscapePressed(__instance))
                panel.Cancel();

            return false;
        }
    }

    /// <summary>Makes the BACK button and the inventory key cancel while the panel is open, instead of closing.</summary>
    [HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.OnBack))]
    private static class CancelOnBack
    {
        private static bool Prefix(Panel_Inventory __instance)
        {
            var panel = SkinsPanel.Find(__instance);
            if (panel == null || !panel.IsOpen)
                return true;

            panel.Cancel();
            return false;
        }
    }

    /// <summary>Closes the panel when the inventory closes some other way, so it's closed next time.</summary>
    /// <remarks>Both overloads of <see cref="Panel_Inventory.Enable(bool)"/> are patched, whichever the game calls.</remarks>
    [HarmonyPatch]
    private static class CloseWithInventory
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(Panel_Inventory)).Where(method =>
                method.Name == nameof(Panel_Inventory.Enable)
                && method.GetParameters() is [{ ParameterType: var type }, ..] && type == typeof(bool));

        // __0 is whether the inventory is opening.
        private static void Postfix(Panel_Inventory __instance, bool __0)
        {
            if (__0)
                return;

            var panel = SkinsPanel.Find(__instance);
            if (panel != null && panel.IsOpen)
                panel.CloseQuietly();
        }
    }
}
