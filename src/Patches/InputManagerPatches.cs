using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="InputManager"/>, which turns the player's input into actions.
/// </summary>
internal static class InputManagerPatches
{
    /// <summary>
    /// Marks while <see cref="InputManager.ProcessInput"/> runs, which handles the crouch key among others, so
    /// <see cref="Prone"/> can tell the crouch input's stand-ups apart from others.
    /// </summary>
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ProcessInput))]
    private static class MarkProcessInput
    {
        private static void Prefix() => Prone.s_CrouchInputDepth++;

        private static void Postfix() => Prone.s_CrouchInputDepth--;
    }

    /// <summary>
    /// Marks while <see cref="InputManager.ExecuteCrouchAction"/> runs, so <see cref="Prone"/> can tell the crouch
    /// input's stand-ups apart from others.
    /// </summary>
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExecuteCrouchAction))]
    private static class MarkCrouchAction
    {
        private static void Prefix() => Prone.s_CrouchInputDepth++;

        private static void Postfix() => Prone.s_CrouchInputDepth--;
    }
}
