namespace AdaptiveArsenal.Patches;

internal static class PanelHUDPatches
{
    [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
    private static class UnblockRevolverAimingMovementHUD
    {
        private static void Postfix(Panel_HUD __instance) => __instance.m_AimingLimitedMobility.gameObject.SetActive(false);
    }
}