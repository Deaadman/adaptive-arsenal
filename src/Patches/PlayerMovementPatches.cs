using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="PlayerMovement"/>, which handles sprinting, stamina, crouching and stealth rules.
/// </summary>
internal static class PlayerMovementPatches
{
    /// <summary>
    /// Marks while <see cref="PlayerMovement.Update"/> runs, so <see cref="CrouchSprintAndSlide"/> can stop it standing
    /// the player up when they sprint while crouched.
    /// </summary>
    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.Update))]
    private static class MarkUpdate
    {
        private static void Prefix() => CrouchSprintAndSlide.s_InPlayerMovementUpdate = true;

        private static void Postfix() => CrouchSprintAndSlide.s_InPlayerMovementUpdate = false;
    }

    /// <summary>
    /// Shortens the range animals can see the player from while prone, by <see cref="Movement.ProneDetectionMultiplier"/>.
    /// </summary>
    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.GetDetectionRangeScale))]
    private static class HarderToSeeProne
    {
        private static void Postfix(ref float __result)
        {
            if (Prone.IsProne)
                __result *= Movement.ProneDetectionMultiplier;
        }
    }

    /// <summary>
    /// Shortens the range animals can hear the player's footsteps from while prone, by
    /// <see cref="Movement.ProneFootstepMultiplier"/>.
    /// </summary>
    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.GetFootStepsRangeScale))]
    private static class QuieterProne
    {
        private static void Postfix(ref float __result)
        {
            if (Prone.IsProne)
                __result *= Movement.ProneFootstepMultiplier;
        }
    }
}
