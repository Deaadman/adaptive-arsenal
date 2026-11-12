namespace AdaptiveArsenal.Utilities;

/// <summary>
/// The moves the mod adds, together: <see cref="Prone"/> and <see cref="CrouchSprintAndSlide"/> each look after
/// their own, and this decides how they fit together, so neither needs to know about the other.
/// </summary>
/// <remarks>
/// <para>
/// Each update of the player's controller, prone goes first, since it decides whether the player is prone, which
/// sliding and sprinting low depend on. The prone key lies the player down, dives instead if they're sprinting, or
/// gets them back up to a crouch, as does pressing crouch, unless they're mid-dive, which has to end first.
/// </para>
/// <para>
/// Prone with fast crawling on, holding sprint never stands the player up, even when they can't crawl fast, like
/// when they're out of stamina.
/// </para>
/// </remarks>
internal static class PlayerMoves
{
    /// <summary>
    /// Before the player's controller moves them: works out prone, then slides, dives and sprinting low, and sets the
    /// controller up for them.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update, and what's changed, to put back afterwards.</param>
    internal static void BeforeControllerUpdate(vp_FPSController controller, ref ControllerUpdate update)
    {
        Prone.Refresh(controller, update);

        // Turning prone off gets the player up too, once there's room.
        var getUpRequested = Prone.TakeGetUpRequest();
        var getUp = getUpRequested || (Prone.IsProne && !Settings.Instance.m_ProneEnabled);
        var logIfNoRoom = getUpRequested;

        if (Settings.Instance.m_ProneEnabled && update.m_MoveUnblocked && InputManager.GetKeyDown(update.m_Player, Settings.Instance.m_ProneKey))
        {
            if (Prone.IsProne)
            {
                getUp = true;
                logIfNoRoom = true;
            }
            else if (CrouchSprintAndSlide.CanDive(controller, update))
            {
                if (Prone.TryLieDown(controller, ref update))
                    CrouchSprintAndSlide.StartDive(controller);
            }
            else if (!CrouchSprintAndSlide.IsSliding)
            {
                Prone.TryLieDown(controller, ref update);
            }
        }

        // A dive has to end before the player can get up from it.
        if (getUp && Prone.IsProne && !CrouchSprintAndSlide.IsSliding)
            Prone.TryGetUp(controller, logIfNoRoom);

        var prone = Prone.IsProne;
        if (prone)
            Prone.KeepLowAndSlow(controller, ref update);

        CrouchSprintAndSlide.BeforeControllerUpdate(controller, ref update, prone);
    }

    /// <summary>After the player's controller has moved them: moves a slide or dive on, and uses stamina for sprinting low.</summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update.</param>
    internal static void AfterControllerUpdate(vp_FPSController controller, in ControllerUpdate update) =>
        CrouchSprintAndSlide.AfterControllerUpdate(controller, update);

    /// <summary>
    /// Whether to stop the game standing the player up now: from the crouch input while prone, from sprinting while
    /// sliding, diving or sprinting low, or from sprinting at all while prone with fast crawling on.
    /// </summary>
    internal static bool ShouldKeepCrouched() =>
        Prone.ShouldKeepCrouched()
        || CrouchSprintAndSlide.ShouldKeepCrouched()
        || (CrouchSprintAndSlide.s_InPlayerMovementUpdate && Prone.IsProne && Settings.Instance.m_FastCrawlEnabled);

    /// <summary>Forgets every move, for a new scene, where the player starts afresh.</summary>
    internal static void Reset()
    {
        Prone.Reset();
        CrouchSprintAndSlide.Reset();
    }
}
