namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSPlayer"/>, which handles the player's first-person movement input.
/// </summary>
internal static class vp_FPSPlayerPatches
{
    /// <summary>
    /// Lets the player walk while aiming a revolver, which the game otherwise doesn't allow.
    /// </summary>
    /// <remarks>Only when movement isn't blocked for another reason, like a menu being open.</remarks>
    [HarmonyPatch(typeof(vp_FPSPlayer), nameof(vp_FPSPlayer.Update))]
    private static class AllowWalkingWhileAimingRevolver
    {
        private static void Postfix(vp_FPSPlayer __instance)
        {
            if (GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.AimRevolver
                && GameManager.IsMoveInputUnblocked())
            {
                __instance.InputWalk();
            }
        }
    }
}
