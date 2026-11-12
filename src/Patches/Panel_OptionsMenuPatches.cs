namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="Panel_OptionsMenu"/>, the game's options menu, which holds the Mod Settings page.
/// </summary>
internal static class Panel_OptionsMenuPatches
{
    /// <summary>
    /// Forgets the mod's switches changed but not confirmed when the options menu opens, so the settings under them
    /// show as the saved values say: see <see cref="Settings.ForgetUnconfirmedSwitches"/>.
    /// </summary>
    [HarmonyPatch(typeof(Panel_OptionsMenu), nameof(Panel_OptionsMenu.Enable))]
    private static class ForgetUnconfirmedSwitchesOnOpen
    {
        private static void Postfix(bool enable)
        {
            if (enable)
                Settings.Instance.ForgetUnconfirmedSwitches();
        }
    }

    /// <summary>
    /// Forgets the mod's switches changed but not confirmed when the options menu is cancelled: see
    /// <see cref="Settings.ForgetUnconfirmedSwitches"/>.
    /// </summary>
    [HarmonyPatch(typeof(Panel_OptionsMenu), nameof(Panel_OptionsMenu.OnCancel))]
    private static class ForgetUnconfirmedSwitchesOnCancel
    {
        private static void Postfix() => Settings.Instance.ForgetUnconfirmedSwitches();
    }
}
