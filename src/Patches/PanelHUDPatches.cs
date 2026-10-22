namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="Panel_HUD"/>, the in-game HUD.
/// </summary>
internal static class PanelHUDPatches
{
    /// <summary>
    /// Hides the HUD's "limited mobility while aiming" indicator, since
    /// <see cref="vp_FPSPlayerPatches"/> lets the player walk while aiming a revolver.
    /// </summary>
    /// <remarks>It's hidden every frame, so it doesn't show for any weapon.</remarks>
    [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
    private static class HideAimingLimitedMobility
    {
        private static void Postfix(Panel_HUD __instance) => __instance.m_AimingLimitedMobility.gameObject.SetActive(false);
    }
}
