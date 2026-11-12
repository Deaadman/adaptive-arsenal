using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSController"/>, which moves the player each frame.
/// </summary>
internal static class vp_FPSControllerPatches
{
    /// <summary>
    /// Lets the player lie prone, sprint while crouched and slide, around each update of the player's controller: see
    /// <see cref="Prone"/> and <see cref="CrouchSprintAndSlide"/>. Puts back the acceleration they change afterwards.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSController), nameof(vp_FPSController.Update))]
    private static class ProneSprintCrouchedAndSlide
    {
        private static void Prefix(vp_FPSController __instance, out ControllerUpdate __state)
        {
            __state = ControllerUpdate.For(__instance);
            if (!__state.m_IsPlayers)
                return;

            // Prone first, since sprinting crouched and sliding don't happen while prone.
            Prone.BeforeControllerUpdate(__instance, ref __state);
            CrouchSprintAndSlide.BeforeControllerUpdate(__instance, ref __state);
        }

        private static void Postfix(vp_FPSController __instance, ControllerUpdate __state)
        {
            if (!__state.m_IsPlayers)
                return;

            __state.RestoreAcceleration(__instance);
            CrouchSprintAndSlide.AfterControllerUpdate(__instance, __state);
        }
    }
}
