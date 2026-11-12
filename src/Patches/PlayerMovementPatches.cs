using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="PlayerMovement"/>, which handles sprinting, stamina and crouching rules.
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
}
