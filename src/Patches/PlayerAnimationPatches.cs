using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="PlayerAnimation"/>, which plays the player's first-person animations, for cycling a rifle's
/// bolt by hand with <see cref="ManualBolt"/>.
/// </summary>
internal static class PlayerAnimationPatches
{
    /// <summary>
    /// Tells <see cref="ManualBolt"/> that a shot's animation started, and whether it will jam the gun, since only a
    /// shot that fires is held, and a jammed rifle's bolt isn't.
    /// </summary>
    [HarmonyPatch(typeof(PlayerAnimation), nameof(PlayerAnimation.Trigger_Generic_Fire))]
    private static class RememberFireAnimation
    {
        private static void Postfix(bool willJam) => ManualBolt.FireAnimationStarted(willJam);
    }

    /// <summary>
    /// Keeps the animator paused while a rifle's bolt is held, since the game sets the animator's speed itself when it
    /// updates it and when it changes state.
    /// </summary>
    [HarmonyPatch(typeof(PlayerAnimation), nameof(PlayerAnimation.UpdateAnimatorSpeed))]
    private static class KeepBoltHeldOnSpeedUpdate
    {
        private static void Postfix() => ManualBolt.KeepPaused();
    }

    /// <inheritdoc cref="KeepBoltHeldOnSpeedUpdate"/>
    [HarmonyPatch(typeof(PlayerAnimation), nameof(PlayerAnimation.MaybeSetState))]
    private static class KeepBoltHeldOnStateChange
    {
        private static void Postfix() => ManualBolt.KeepPaused();
    }
}
