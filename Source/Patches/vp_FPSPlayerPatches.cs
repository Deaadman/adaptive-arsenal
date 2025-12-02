namespace AdaptiveArsenal.Patches
{
    internal static class vp_FPSPlayerPatches
    {
        /// <summary>
        /// Ensures that movement input is processed while aiming with the revolver, if movement is not blocked.
        /// </summary>
        /// <remarks>This patch modifies the behavior of the <see cref="vp_FPSPlayer.Update"/> method to allow
        /// movement input when the player is in revolver aiming mode and movement is unblocked. It is applied using
        /// Harmony.</remarks>
        [HarmonyPatch(typeof(vp_FPSPlayer), nameof(vp_FPSPlayer.Update))]
        private static class UnblockRevolverAimingMovement
        {
            private static void Postfix(vp_FPSPlayer __instance)
            {
                if (GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.AimRevolver
                    && GameManager.IsMoveInputUnblocked()) __instance.InputWalk();
            }
        }
    }
}