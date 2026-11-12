using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSController"/>, which moves the player each frame.
/// </summary>
internal static class vp_FPSControllerPatches
{
    /// <summary>
    /// Lets the player sprint while crouched and slide, around each update of the player's controller: see
    /// <see cref="CrouchSprintAndSlide"/>.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSController), nameof(vp_FPSController.Update))]
    private static class SprintCrouchedAndSlide
    {
        private static void Prefix(vp_FPSController __instance, out CrouchSprintAndSlide.ControllerUpdate __state)
        {
            var player = GameManager.GetVpFPSPlayer();
            __state = player != null && player.Controller == __instance
                ? CrouchSprintAndSlide.BeforeControllerUpdate(__instance, player)
                : default;
        }

        private static void Postfix(vp_FPSController __instance, CrouchSprintAndSlide.ControllerUpdate __state)
        {
            if (__state.m_IsPlayers)
                CrouchSprintAndSlide.AfterControllerUpdate(__instance, __state);
        }
    }
}
