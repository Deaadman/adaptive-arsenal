using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSPlayer"/>, which handles the player's first-person movement input.
/// </summary>
internal static class vp_FPSPlayerPatches
{
    /// <summary>
    /// Lets the player walk while aiming a revolver, which the game otherwise doesn't allow.
    /// </summary>
    /// <remarks>
    /// Only when <see cref="Settings.m_RevolverWalkWhileAiming"/> is on, and movement isn't blocked for another reason,
    /// like a menu being open.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSPlayer), nameof(vp_FPSPlayer.Update))]
    private static class AllowWalkingWhileAimingRevolver
    {
        private static void Postfix(vp_FPSPlayer __instance)
        {
            if (Settings.Instance.m_RevolverWalkWhileAiming
                && GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.AimRevolver
                && GameManager.IsMoveInputUnblocked())
            {
                __instance.InputWalk();
            }
        }
    }

    /// <summary>
    /// Stops the game standing the player up because they're sprinting, while they slide or sprint crouched: see
    /// <see cref="CrouchSprintAndSlide.ShouldKeepCrouched"/>. Anything else can still stand them up.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSPlayer), nameof(vp_FPSPlayer.EnableCrouch))]
    private static class KeepCrouched
    {
        private static bool Prefix(bool enabled, ref bool __result)
        {
            if (enabled || !CrouchSprintAndSlide.ShouldKeepCrouched())
                return true;

            __result = false;
            return false;
        }
    }
}
