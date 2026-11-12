using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSController"/>, which moves the player each frame.
/// </summary>
internal static class vp_FPSControllerPatches
{
    /// <summary>
    /// Lets the player lie prone, sprint low, slide and dive, around each update of the player's controller: see
    /// <see cref="PlayerMoves"/>. Puts back the acceleration they change afterwards.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSController), nameof(vp_FPSController.Update))]
    private static class PlayerMovesAroundUpdate
    {
        private static void Prefix(vp_FPSController __instance, out ControllerUpdate __state)
        {
            __state = ControllerUpdate.For(__instance);
            if (__state.m_IsPlayers)
                PlayerMoves.BeforeControllerUpdate(__instance, ref __state);
        }

        private static void Postfix(vp_FPSController __instance, ControllerUpdate __state)
        {
            if (!__state.m_IsPlayers)
                return;

            __state.RestoreAcceleration(__instance);
            PlayerMoves.AfterControllerUpdate(__instance, __state);
        }
    }
}
