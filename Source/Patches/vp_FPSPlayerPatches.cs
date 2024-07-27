namespace AdaptiveArsenal.Patches;

internal static class vp_FPSPlayerPatches
{
    [HarmonyPatch(typeof(vp_FPSPlayer), nameof(vp_FPSPlayer.Update))]
    private static class UnblockRevolverAimingMovement
    {
        private static void Postfix(vp_FPSPlayer __instance)
        {
            if (GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.AimRevolver && GameManager.IsMoveInputUnblocked())
            {
                __instance.InputWalk();
            }
        }
    }
}