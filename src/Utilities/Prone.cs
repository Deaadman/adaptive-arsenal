using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Lets the player lie prone, a stance below crouching, with its own key. The patches on
/// <see cref="vp_FPSController"/>, <see cref="vp_FPSPlayer"/>, <see cref="InputManager"/>,
/// <see cref="PlayerMovement"/> and <see cref="vp_FPSShooter"/> call into this.
/// </summary>
/// <remarks>
/// <para>
/// Prone is the game's crouch, made lower: the player stays crouched, and while prone the controller's crouched
/// capsule (<see cref="vp_FPSController.m_CompactHeight"/>) and the camera's
/// <see cref="vp_FPSCamera.PositionOffset"/> are lowered, by the fractions in <see cref="Movement"/>. Anything that
/// works crouched works prone. Prone, animals see and hear the player from less far, and the player moves slower and
/// shoots more accurately.
/// </para>
/// <para>
/// The prone key lies the player down, crouching them first if they're standing, and gets them back up to a crouch
/// if there's room above. So does pressing crouch. Otherwise the crouch input can't stand them up while prone, so
/// letting go of a held crouch key leaves them prone. Anything else that stands them up, like sprinting, a rope or a
/// scripted action, ends it.
/// </para>
/// <para>
/// The game can set the crouched capsule and the camera's offset itself, for a change of stance or aiming, so each
/// update this checks they're still the prone values, and if not, takes the new ones as crouched and lowers them
/// again. Leaving prone puts back only what's still the mod's, and only grows the capsule back where there's room.
/// </para>
/// </remarks>
internal static class Prone
{
    /// <summary>
    /// How deep the crouch input is, from <see cref="vp_FPSPlayer.InputCrouch"/>, <see cref="InputManager.ProcessInput"/>
    /// or <see cref="InputManager.ExecuteCrouchAction"/>, so its stand-ups can be told apart from others.
    /// </summary>
    internal static int s_CrouchInputDepth;

    /// <summary>Whether the player is prone.</summary>
    private static bool s_Prone;

    /// <summary>Whether the player pressed crouch while prone, so they get up in the next update.</summary>
    private static bool s_GetUpRequested;

    /// <summary>The controller whose capsule was lowered, so it isn't touched once the player's controller changes.</summary>
    private static vp_FPSController? s_Controller;

    /// <summary>The camera whose offset was lowered.</summary>
    private static vp_FPSCamera? s_Camera;

    /// <summary>The mod's change to the controller's crouched capsule: its height and middle.</summary>
    private static ValueOverride<(float Height, Vector3 Center)> s_Capsule;

    /// <summary>The mod's change to the camera's offset.</summary>
    private static ValueOverride<Vector3> s_CameraOffset;

    /// <summary>Whether the player is lying prone.</summary>
    internal static bool IsProne => s_Prone;

    /// <summary>
    /// Whether to stop the game standing the player up now: while prone, when the crouch input does it. A crouch press
    /// gets the player up to a crouch instead, in the next update.
    /// </summary>
    internal static bool ShouldKeepCrouched()
    {
        if (!s_Prone || s_CrouchInputDepth <= 0)
            return false;

        var player = GameManager.GetVpFPSPlayer();
        if (player != null && InputManager.GetCrouchPressed(player))
            s_GetUpRequested = true;

        return true;
    }

    /// <summary>
    /// Before the player's controller moves them: lies the player down or gets them up when they ask, ends prone if
    /// something stood them up, and keeps them low and slow while prone.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update, and what's changed, to put back afterwards.</param>
    internal static void BeforeControllerUpdate(vp_FPSController controller, ref ControllerUpdate update)
    {
        // The controller doesn't update during the crouch input, so this only clears a count an exception left behind.
        s_CrouchInputDepth = 0;

        if (s_Prone && s_Controller != controller)
            Leave("the player's controller changed", resize: false);
        else if (s_Prone && !update.m_Crouched)
            Leave("the player stood up", resize: false);

        // Turning prone off gets the player up too, once there's room.
        var getUp = s_GetUpRequested || (s_Prone && !Settings.Instance.m_ProneEnabled);
        var logIfNoRoom = s_GetUpRequested;
        s_GetUpRequested = false;

        if (Settings.Instance.m_ProneEnabled && update.m_MoveUnblocked && InputManager.GetKeyDown(update.m_Player, Settings.Instance.m_ProneKey))
        {
            if (s_Prone)
            {
                getUp = true;
                logIfNoRoom = true;
            }
            else
            {
                TryLieDown(controller, ref update);
            }
        }

        if (s_Prone && getUp)
            TryGetUp(controller, logIfNoRoom);

        if (!s_Prone)
            return;

        KeepLow(controller);
        update.MultiplyAcceleration(controller, Movement.ProneSpeedMultiplier);
    }

    /// <summary>
    /// Forgets prone for a new scene, where the player starts afresh: puts the crouched capsule and camera offset back,
    /// without resizing the capsule now, since the player may not be in place yet.
    /// </summary>
    internal static void Reset()
    {
        s_GetUpRequested = false;
        if (s_Prone)
            Leave("a new scene loaded", resize: false);
    }

    /// <summary>
    /// Lies the player down, crouching them first if they're standing, unless they're sliding or off the ground.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="update">The player's state for this update, whose crouch this keeps up to date.</param>
    private static void TryLieDown(vp_FPSController controller, ref ControllerUpdate update)
    {
        var characterController = controller.m_Controller;
        if (CrouchSprintAndSlide.IsSliding || characterController == null || !characterController.isGrounded)
            return;

        if (!update.m_Crouched)
        {
            update.m_Player.EnableCrouch(true);
            update.m_Crouched = update.m_PlayerManager.PlayerIsCrouched();
            if (!update.m_Crouched)
            {
                Mod.Logger.Log("Couldn't crouch to lie prone.", FlaggedLoggingLevel.Debug);
                return;
            }
        }

        s_Prone = true;
        s_Controller = controller;
        s_Camera = GameManager.GetVpFPSCamera();
        KeepLow(controller);

        Mod.Logger.Log(
            $"Lying prone: capsule {s_Capsule.Original.Height:F2} m to {controller.m_CompactHeight:F2} m (radius {characterController.radius:F2} m), "
            + $"camera {s_CameraOffset.Original.y:F2} to {(s_Camera != null ? s_Camera.PositionOffset.y : 0f):F2}.",
            FlaggedLoggingLevel.Debug);
    }

    /// <summary>Gets the player back up to a crouch, if there's room above them for it.</summary>
    /// <param name="controller">The player's controller.</param>
    /// <param name="logIfNoRoom">Whether to log when there isn't room, which the player asked for.</param>
    private static void TryGetUp(vp_FPSController controller, bool logIfNoRoom)
    {
        if (HasRoomToCrouch(controller))
            Leave("the player got up", resize: true);
        else if (logIfNoRoom)
            Mod.Logger.Log("No room to get up from prone.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>
    /// Whether there's room above the prone player to crouch: nothing between the top of the prone capsule and the top
    /// of the crouched one.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    private static bool HasRoomToCrouch(vp_FPSController controller)
    {
        var characterController = controller.m_Controller;
        if (characterController == null)
            return true;

        var bounds = characterController.bounds;
        var radius = characterController.radius * Movement.HeadroomRadiusFraction;
        var origin = new Vector3(bounds.center.x, bounds.max.y - radius, bounds.center.z);
        var distance = Movement.HeadroomToCrouch(s_Capsule.Original.Height, controller.m_CompactHeight);
        return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Keeps the crouched capsule and the camera at their prone values, taking any value the game set since as the new
    /// crouched one.
    /// </summary>
    /// <param name="controller">The player's controller.</param>
    private static void KeepLow(vp_FPSController controller)
    {
        var capsule = (controller.m_CompactHeight, controller.m_CompactCenter);
        if (!s_Capsule.IsCurrent(capsule))
        {
            var radius = controller.m_Controller != null ? controller.m_Controller.radius : 0f;
            var proneHeight = Movement.ProneHeight(capsule.m_CompactHeight, radius);
            var center = capsule.m_CompactCenter;
            var proneCenter = new Vector3(center.x, Movement.ProneCenterY(center.y, capsule.m_CompactHeight, proneHeight), center.z);

            (controller.m_CompactHeight, controller.m_CompactCenter) = s_Capsule.Set(capsule, (proneHeight, proneCenter));
            ResizeIfCrouched(controller);
        }

        if (s_Camera != null && !s_CameraOffset.IsCurrent(s_Camera.PositionOffset))
        {
            // The camera's springs ease it down.
            var offset = s_Camera.PositionOffset;
            s_Camera.PositionOffset = s_CameraOffset.Set(offset, new Vector3(offset.x, Movement.ProneEyeHeight(offset.y), offset.z));
        }
    }

    /// <summary>
    /// Sizes the controller's capsule to its crouched values now, in case it only does so when the player crouches.
    /// </summary>
    /// <param name="controller">The player's controller, if it's crouched.</param>
    private static void ResizeIfCrouched(vp_FPSController controller)
    {
        if (controller.m_Compact)
            controller.SetCompactImmmediate(true);
    }

    /// <summary>
    /// Ends prone, putting back the crouched capsule and camera offset where they're still the prone values.
    /// </summary>
    /// <param name="reason">Why, for the log.</param>
    /// <param name="resize">
    /// Whether to grow the capsule back now, which only the caller knows there's room for. Otherwise it takes its
    /// crouched size the next time the game sizes it.
    /// </param>
    private static void Leave(string reason, bool resize)
    {
        s_Prone = false;

        if (s_Controller != null && s_Capsule.Restore((s_Controller.m_CompactHeight, s_Controller.m_CompactCenter), out var capsule))
        {
            (s_Controller.m_CompactHeight, s_Controller.m_CompactCenter) = capsule;
            if (resize)
                ResizeIfCrouched(s_Controller);
        }

        if (s_Camera != null && s_CameraOffset.Restore(s_Camera.PositionOffset, out var offset))
            s_Camera.PositionOffset = offset;

        s_Capsule = default;
        s_CameraOffset = default;
        s_Controller = null;
        s_Camera = null;
        Mod.Logger.Log($"Stopped lying prone because {reason}.", FlaggedLoggingLevel.Debug);
    }
}
