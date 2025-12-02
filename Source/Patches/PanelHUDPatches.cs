namespace AdaptiveArsenal.Patches
{
    internal static class PanelHUDPatches
    {
        /// <summary>
        /// A Harmony patch that modifies the behavior of the <see cref="Panel_HUD.Update"/> method to disable the limited
        /// mobility indicator when aiming with the revolver.
        /// </summary>
        /// <remarks>This patch ensures that the <c>m_AimingLimitedMobility</c> GameObject is always inactive,
        /// allowing unrestricted movement while aiming.</remarks>
        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
        private static class UnblockRevolverAimingMovementHUD
        {
            private static void Postfix(Panel_HUD __instance) => __instance.m_AimingLimitedMobility.gameObject.SetActive(false);
        }
    }
}