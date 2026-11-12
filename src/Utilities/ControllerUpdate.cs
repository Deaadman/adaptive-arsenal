using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// One update of the player's controller: the player's state, looked up once for <see cref="PlayerMoves"/> and the
/// moves it runs to share, and what they changed, to put back afterwards. See
/// <see cref="Patches.vp_FPSControllerPatches"/>.
/// </summary>
internal struct ControllerUpdate
{
    /// <summary>Whether the update is of the player's controller, with everything below set.</summary>
    internal bool m_IsPlayers;

    /// <summary>The player.</summary>
    internal vp_FPSPlayer m_Player;

    /// <summary>The player manager.</summary>
    internal PlayerManager m_PlayerManager;

    /// <summary>The player's movement rules.</summary>
    internal PlayerMovement m_Movement;

    /// <summary>Whether the player is crouched, kept up to date when the mod crouches them.</summary>
    internal bool m_Crouched;

    /// <summary>Whether the player can move, with nothing like a menu in the way.</summary>
    internal bool m_MoveUnblocked;

    /// <summary>The mod's change to the controller's acceleration for this update.</summary>
    private ValueOverride<float> m_Acceleration;

    /// <summary>
    /// Looks up the player's state for an update of <paramref name="controller"/>, if it's the player's.
    /// </summary>
    /// <param name="controller">The controller being updated.</param>
    /// <returns>The update, with <see cref="m_IsPlayers"/> false if it isn't the player's or the player isn't ready.</returns>
    internal static ControllerUpdate For(vp_FPSController controller)
    {
        var player = GameManager.GetVpFPSPlayer();
        var playerManager = GameManager.GetPlayerManagerComponent();
        var movement = GameManager.GetPlayerMovementComponent();
        if (player == null || playerManager == null || movement == null || player.Controller != controller)
            return default;

        return new ControllerUpdate
        {
            m_IsPlayers = true,
            m_Player = player,
            m_PlayerManager = playerManager,
            m_Movement = movement,
            m_Crouched = playerManager.PlayerIsCrouched(),
            m_MoveUnblocked = GameManager.IsMoveInputUnblocked()
        };
    }

    /// <summary>Changes the controller's acceleration for this update.</summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="acceleration">The acceleration to use.</param>
    internal void ChangeAcceleration(vp_FPSController controller, float acceleration) =>
        controller.MotorAcceleration = m_Acceleration.Set(controller.MotorAcceleration, acceleration);

    /// <summary>Multiplies the controller's acceleration for this update.</summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="multiplier">What to multiply it by.</param>
    internal void MultiplyAcceleration(vp_FPSController controller, float multiplier) =>
        ChangeAcceleration(controller, controller.MotorAcceleration * multiplier);

    /// <summary>
    /// Puts the controller's acceleration back, unless something else set it during the update, like a change of
    /// stance, whose value is left as it is.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    internal void RestoreAcceleration(vp_FPSController controller)
    {
        if (m_Acceleration.Restore(controller.MotorAcceleration, out var original))
            controller.MotorAcceleration = original;
    }
}
