using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Lets the player sprint while crouched or prone, slide by crouching while sprinting, and dive. <see cref="PlayerMoves"/>
/// calls into this, telling it whether the player is prone, and starts dives; the patches on
/// <see cref="vp_FPSPlayer"/>, <see cref="PlayerManager"/> and <see cref="PlayerMovement"/> also call into it.
/// </summary>
/// <remarks>
/// <para>
/// Sprinting while crouched, or crawling fast while prone, counts as sprinting to the game:
/// <see cref="PlayerManager.PlayerIsSprinting"/> says so, so the game uses stamina, holds off recovering it, and adds
/// fatigue and calories as it does for any sprint. The speed comes from raising the controller's
/// <see cref="vp_FPSController.MotorAcceleration"/> for each of its updates, dividing out the game's own sprint
/// multiplier. The game stands the player up when they sprint, from <see cref="PlayerMovement.Update"/>, so that's
/// blocked while doing either. If the game turns out not to use stamina for them, this does, and logs that once.
/// </para>
/// <para>
/// A slide starts the update after the player presses crouch while sprinting, so the game has handled the press
/// first. It takes over the player's movement: their input does nothing, and each frame this moves the controller
/// along the slide itself, at the speed from <see cref="Movement"/>, and tells the game about the move as its own
/// movement does. It ends when the slide slows down, hits something, leaves the ground, or the player stands up or
/// something else takes over movement, leaving the player crouched.
/// </para>
/// <para>
/// A dive is a slide on the player's front, started at once from the mod's own prone key, once
/// <see cref="PlayerMoves"/> has laid the player down. It's shorter, and leaves them prone.
/// </para>
/// <para>
/// The decompiled game code only shows what calls what, so some of this is a guess at how the game behaves, logged at
/// debug level so it can be checked in game.
/// </para>
/// </remarks>
internal static class CrouchSprintAndSlide
{
    /// <summary>Whether <see cref="PlayerMovement.Update"/> is running, so the crouch it ends can be told apart from the player's own.</summary>
    internal static bool s_InPlayerMovementUpdate;

    /// <summary>Whether the player is sliding or diving.</summary>
    private static bool s_Sliding;

    /// <summary>Whether the player pressed crouch while sprinting, so a slide starts in the next update.</summary>
    private static bool s_SlideRequested;

    /// <summary>Whether the slide under way is a dive.</summary>
    private static bool s_Diving;

    /// <summary>The player's velocity across the ground when they asked to slide.</summary>
    private static Vector3 s_SlideEntryVelocity;

    /// <summary>Which way the slide is going, across the ground. A unit vector with no Y.</summary>
    private static Vector3 s_SlideDirection;

    /// <summary>How fast the slide is going.</summary>
    private static float s_SlideSpeed;

    /// <summary>How long the slide has lasted.</summary>
    private static float s_SlideSeconds;

    /// <summary>How long the slide has been off the ground.</summary>
    private static float s_SlideAirborneSeconds;

    /// <summary>Which low sprint the player is doing, as worked out at the start of the controller's last update.</summary>
    private static LowSprint s_LowSprint;

    /// <summary>Whether the player was sprinting standing up at the end of the last update, which a slide starts from.</summary>
    private static bool s_WasSprintingStanding;

    /// <summary>The player's velocity across the ground at the end of the last update.</summary>
    private static Vector3 s_LastGroundVelocity;

    /// <summary>The player's stamina at the end of the last update, to tell whether the game used any since.</summary>
    private static float s_StaminaAfterLastUpdate;

    /// <summary>Whether it's been logged that the game doesn't use stamina for sprinting crouched.</summary>
    private static bool s_LoggedGameUsesNoStamina;

    /// <summary>The frame <see cref="s_LowSprint"/> was last worked out in.</summary>
    private static int s_LowSprintFrame;

    /// <summary>A sprint the player does without standing up.</summary>
    private enum LowSprint
    {
        /// <summary>Not sprinting low.</summary>
        None,

        /// <summary>Sprinting while crouched.</summary>
        Crouched,

        /// <summary>Crawling fast while prone.</summary>
        Prone
    }

    /// <summary>
    /// Whether the player is sprinting crouched or crawling fast, which <see cref="PlayerManager.PlayerIsSprinting"/>
    /// reports as sprinting. Only while the player's controller is updating, so it doesn't stick after a load or in
    /// the menus.
    /// </summary>
    internal static bool IsCrouchSprinting => s_LowSprint != LowSprint.None && Time.frameCount - s_LowSprintFrame <= 1;

    /// <summary>Whether the player is sliding or diving, or about to start a slide in the next update.</summary>
    internal static bool IsSliding => s_Sliding || s_SlideRequested;

    /// <summary>
    /// Whether to stop the game standing the player up now: when <see cref="PlayerMovement.Update"/> does it, because
    /// they're sprinting, while they slide, dive, sprint crouched or crawl fast.
    /// </summary>
    /// <remarks>
    /// Anything else can still stand them up, like the player's own crouch key, a rope or a scripted action. That ends
    /// a slide, or sprinting crouched.
    /// </remarks>
    internal static bool ShouldKeepCrouched() => s_InPlayerMovementUpdate && (s_Sliding || IsCrouchSprinting);

    /// <summary>
    /// Before the player's controller moves them: starts or ends a slide, works out whether the player is sprinting
    /// crouched or crawling fast, and sets the controller's acceleration for either.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update, and what's changed, to put back afterwards.</param>
    /// <param name="prone">Whether the player is prone.</param>
    internal static void BeforeControllerUpdate(vp_FPSController controller, ref ControllerUpdate update, bool prone)
    {
        var movement = update.m_Movement;

        if (s_SlideRequested)
            StartRequestedSlide(controller, ref update, prone);
        else if (!s_Sliding && Settings.Instance.m_SlideEnabled && CanStartSlide(controller, update.m_MoveUnblocked) && InputManager.GetCrouchPressed(update.m_Player))
            RequestSlide();

        if (s_Sliding && (!update.m_Crouched || !update.m_MoveUnblocked))
            EndSlide(update.m_Crouched ? "movement was taken over" : "the player stood up");

        var lowSprint = WantsToCrouchSprint(update, prone) ? (prone ? LowSprint.Prone : LowSprint.Crouched) : LowSprint.None;
        s_LowSprintFrame = Time.frameCount;
        if (lowSprint != s_LowSprint)
        {
            if (s_LowSprint != LowSprint.None)
                Mod.Logger.Log($"Stopped {Describe(s_LowSprint)}.", FlaggedLoggingLevel.Debug);

            s_LowSprint = lowSprint;
            if (lowSprint != LowSprint.None)
                Mod.Logger.Log($"Started {Describe(lowSprint)}, with the game's sprint multiplier at {movement.GetSprintMovementMultiplier():F2}.", FlaggedLoggingLevel.Debug);
        }

        // Crouching for a slide can change the acceleration, so this comes after it.
        if (s_Sliding)
        {
            // The slide moves the player itself, after the controller's update, so their input does nothing.
            update.ChangeAcceleration(controller, 0f);
        }
        else if (s_LowSprint != LowSprint.None)
        {
            // GetSprintMovementMultiplier asks PlayerIsSprinting, which now says the player is sprinting.
            update.MultiplyAcceleration(controller, Movement.CrouchSprintAccelerationMultiplier(movement.GetSprintMovementMultiplier(), prone));
        }
    }

    /// <summary>
    /// After the player's controller has moved them: moves a slide on, makes sure sprinting low uses stamina, and
    /// remembers how the player was moving for the next update.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update.</param>
    internal static void AfterControllerUpdate(vp_FPSController controller, in ControllerUpdate update)
    {
        var characterController = controller.m_Controller;
        if (characterController == null)
            return;

        var movement = update.m_Movement;
        if (s_Sliding)
            MoveSlide(controller, characterController, movement);
        else if (s_LowSprint != LowSprint.None)
            KeepUsingStamina(movement);

        var velocity = characterController.velocity;
        s_LastGroundVelocity = new Vector3(velocity.x, 0f, velocity.z);
        s_WasSprintingStanding = !s_Sliding && s_LowSprint == LowSprint.None && !update.m_Crouched && update.m_PlayerManager.PlayerIsSprinting();
        s_StaminaAfterLastUpdate = movement.CurrentStamina;
    }

    /// <summary>Forgets any slide, dive or low sprint, for a new scene, where the player starts afresh.</summary>
    internal static void Reset()
    {
        s_Sliding = false;
        s_SlideRequested = false;
        s_Diving = false;
        s_LowSprint = LowSprint.None;
        s_WasSprintingStanding = false;
        s_LastGroundVelocity = Vector3.zero;
    }

    /// <summary>
    /// Whether the player can dive: diving is on, they're not sliding, and they were sprinting standing up, on the
    /// ground and fast enough.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update.</param>
    internal static bool CanDive(vp_FPSController controller, in ControllerUpdate update) =>
        Settings.Instance.m_DiveEnabled && !IsSliding && CanStartSlide(controller, update.m_MoveUnblocked);

    /// <summary>
    /// Starts a dive now, from the player's speed in the last update. <see cref="PlayerMoves"/> lays the player down
    /// first.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    internal static void StartDive(vp_FPSController controller)
    {
        s_SlideEntryVelocity = s_LastGroundVelocity;
        StartSlide(controller, dive: true);
    }

    /// <summary>
    /// Whether the player is trying to sprint while crouched, or crawl fast while prone, and can.
    /// </summary>
    /// <remarks>
    /// The same rules as the game's sprint, as far as the decompiled code shows them: the sprint key, moving mostly
    /// forward, stamina left, and nothing like an injury, heavy load or aiming stopping it.
    /// </remarks>
    /// <param name="update">The player's state for this update.</param>
    /// <param name="prone">Whether the player is prone.</param>
    private static bool WantsToCrouchSprint(in ControllerUpdate update, bool prone)
    {
        var enabled = prone ? Settings.Instance.m_FastCrawlEnabled : Settings.Instance.m_CrouchSprintEnabled;
        if (!enabled || s_Sliding || !update.m_Crouched || !update.m_MoveUnblocked || !InputManager.GetSprintDown(update.m_Player))
            return false;

        if (update.m_PlayerManager.PlayerCantSprintBecauseOfInjury() || update.m_PlayerManager.PlayerIsZooming())
            return false;

        var movement = update.m_Movement;
        if (movement.CurrentStamina <= 0f && !PlayerMovement.m_UnlimitedSprint)
            return false;

        var input = update.m_Player.GetMovementInput();
        return Movement.InputCanSprint(input.y, input.x, movement.m_SprintDisableWhenForwardInputBelow, movement.m_SprintDisableWhenStrafeInputAbove);
    }

    /// <summary>How a low sprint reads in the log.</summary>
    /// <param name="lowSprint">The low sprint.</param>
    private static string Describe(LowSprint lowSprint) => lowSprint == LowSprint.Prone ? "crawling fast" : "sprinting crouched";

    /// <summary>
    /// Whether a slide or dive can start: the player was sprinting standing up in the last update, can move, and is on
    /// the ground moving fast enough.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="moveUnblocked">Whether the player can move, with nothing like a menu in the way.</param>
    private static bool CanStartSlide(vp_FPSController controller, bool moveUnblocked)
    {
        var characterController = controller.m_Controller;
        return s_WasSprintingStanding
            && moveUnblocked
            && characterController != null
            && characterController.isGrounded
            && s_LastGroundVelocity.magnitude >= Movement.MinSlideStartSpeed;
    }

    /// <summary>
    /// Asks for a slide in the next update, from the player's speed now.
    /// </summary>
    /// <remarks>
    /// Waiting an update lets the game handle the crouch press first, so it isn't handled twice, and only the player's
    /// own crouch starts a slide, not one the game forces.
    /// </remarks>
    private static void RequestSlide()
    {
        s_SlideRequested = true;
        s_SlideEntryVelocity = s_LastGroundVelocity;
    }

    /// <summary>
    /// Starts the slide asked for in the last update, crouching the player first if the game didn't, unless they've
    /// gone prone since.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update, whose crouch this keeps up to date.</param>
    /// <param name="prone">Whether the player is prone.</param>
    private static void StartRequestedSlide(vp_FPSController controller, ref ControllerUpdate update, bool prone)
    {
        s_SlideRequested = false;
        if (prone)
            return;

        // The game may not crouch while sprinting.
        if (!update.m_Crouched)
        {
            update.m_Player.EnableCrouch(true);
            update.m_Crouched = update.m_PlayerManager.PlayerIsCrouched();
        }

        if (!update.m_Crouched)
        {
            Mod.Logger.Log("Couldn't crouch to start a slide.", FlaggedLoggingLevel.Debug);
            return;
        }

        StartSlide(controller, dive: false);
    }

    /// <summary>Starts a slide or dive from <see cref="s_SlideEntryVelocity"/>.</summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="dive">Whether it's a dive.</param>
    private static void StartSlide(vp_FPSController controller, bool dive)
    {
        var entrySpeed = s_SlideEntryVelocity.magnitude;
        s_Sliding = true;
        s_Diving = dive;
        s_SlideDirection = s_SlideEntryVelocity / entrySpeed;
        s_SlideSpeed = Movement.SlideStartSpeed(entrySpeed, dive);
        s_SlideSeconds = 0f;
        s_SlideAirborneSeconds = 0f;

        // The slide replaces the sprint's movement, so it doesn't carry on as well.
        var throttle = controller.m_MotorThrottle;
        controller.m_MotorThrottle = new Vector3(0f, throttle.y, 0f);

        Mod.Logger.Log($"Started a {(dive ? "dive" : "slide")} from {entrySpeed:F1} m/s at {s_SlideSpeed:F1} m/s.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>
    /// Moves a slide on by one frame, following the ground, and ends it once it's slowed down, lasted too long, or been
    /// off the ground too long.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="characterController">The player's character controller.</param>
    /// <param name="movement">The player's movement rules, told about the move.</param>
    private static void MoveSlide(vp_FPSController controller, CharacterController characterController, PlayerMovement movement)
    {
        var deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        s_SlideSeconds += deltaTime;
        s_SlideAirborneSeconds = characterController.isGrounded ? 0f : s_SlideAirborneSeconds + deltaTime;
        if (Movement.SlideLeftGround(s_SlideAirborneSeconds))
        {
            EndSlide("the player left the ground");
            return;
        }

        var rise = GroundRise(characterController);
        s_SlideSpeed = Movement.NextSlideSpeed(s_SlideSpeed, rise, deltaTime, s_Diving);
        if (Movement.SlideHasEnded(s_SlideSpeed, s_SlideSeconds))
        {
            EndSlide($"it slowed to {s_SlideSpeed:F1} m/s");
            return;
        }

        var distance = s_SlideSpeed * deltaTime;
        var start = characterController.transform.position;
        var collisions = characterController.Move(s_SlideDirection * distance + Vector3.up * (rise * distance));
        var moved = characterController.transform.position - start;

        var movedAcross = new Vector3(moved.x, 0f, moved.z).magnitude;
        s_SlideSpeed = Movement.SlideSpeedAfterMove(s_SlideSpeed, movedAcross, deltaTime, (collisions & CollisionFlags.Sides) != 0);

        // Tell the game about the move, as the controller does for its own, for footsteps, distance travelled,
        // calories and sprains, and for anything that reads how fast the player is moving.
        movement.MovedDistance(moved, false);
        controller.m_LastMoveVelocity = moved / deltaTime;
    }

    /// <summary>
    /// How steep the ground under the player is along the slide, as from <see cref="Movement.Rise"/>, or 0 if there's
    /// no ground close below.
    /// </summary>
    /// <param name="characterController">The player's character controller.</param>
    private static float GroundRise(CharacterController characterController)
    {
        var bounds = characterController.bounds;
        if (!Physics.Raycast(bounds.center, Vector3.down, out var hit, bounds.extents.y + Movement.GroundProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return 0f;

        var normal = hit.normal;
        return Movement.Rise(normal.x, normal.y, normal.z, s_SlideDirection.x, s_SlideDirection.z);
    }

    /// <summary>
    /// Makes sure sprinting low uses stamina and holds off its recovery, as sprinting does, if the game isn't already
    /// doing it.
    /// </summary>
    /// <param name="movement">The player's movement rules.</param>
    private static void KeepUsingStamina(PlayerMovement movement)
    {
        // The game waits this long after a sprint before stamina recovers.
        movement.m_SecondsNotSprinting = 0f;

        if (PlayerMovement.m_UnlimitedSprint || movement.CurrentStamina < s_StaminaAfterLastUpdate)
            return;

        if (!s_LoggedGameUsesNoStamina)
        {
            Mod.Logger.Log("The game isn't using stamina for sprinting low, so the mod is.", FlaggedLoggingLevel.Debug);
            s_LoggedGameUsesNoStamina = true;
        }

        movement.AddSprintStamina(-Movement.StaminaUsed(movement.m_SprintStaminaUsagePerSecond, Time.deltaTime));
    }

    /// <summary>Ends the slide, leaving the player crouched, or prone after a dive.</summary>
    /// <param name="reason">Why, for the log.</param>
    private static void EndSlide(string reason)
    {
        s_Sliding = false;
        Mod.Logger.Log($"{(s_Diving ? "Dive" : "Slide")} ended after {s_SlideSeconds:F1} s because {reason}.", FlaggedLoggingLevel.Debug);
    }
}
