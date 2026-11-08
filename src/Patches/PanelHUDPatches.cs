namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="Panel_HUD"/>, the in-game HUD.
/// </summary>
internal static class PanelHUDPatches
{
    /// <summary>
    /// Hides the HUD's "limited mobility while aiming" indicator while aiming a revolver, since
    /// <see cref="vp_FPSPlayerPatches"/> lets the player walk then.
    /// </summary>
    /// <remarks>
    /// Rifles still can't move while aiming, so the game shows it for them as usual, and for the revolver when
    /// <see cref="Settings.m_RevolverWalkWhileAiming"/> is off.
    /// </remarks>
    [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
    private static class HideAimingLimitedMobilityForRevolver
    {
        private static void Postfix(Panel_HUD __instance)
        {
            if (Settings.Instance.m_RevolverWalkWhileAiming
                && GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.AimRevolver)
                __instance.m_AimingLimitedMobility.gameObject.SetActive(false);
        }
    }
}
