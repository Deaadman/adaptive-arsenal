using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="PlayerManager"/>, which tracks what the player is doing.
/// </summary>
internal static class PlayerManagerPatches
{
    /// <summary>
    /// Counts sprinting crouched as sprinting, so the game uses stamina, holds off recovering it, and adds fatigue and
    /// calories for it, as it does for any sprint: see <see cref="CrouchSprintAndSlide"/>.
    /// </summary>
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PlayerIsSprinting))]
    private static class CountCrouchedSprintingAsSprinting
    {
        private static void Postfix(ref bool __result)
        {
            if (CrouchSprintAndSlide.IsCrouchSprinting)
                __result = true;
        }
    }
}
