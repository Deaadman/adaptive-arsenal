using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Makes the player cycle a rifle's bolt by hand, following <see cref="BoltCycle"/>'s rules: after a shot, the next
/// trigger pull cycles the bolt, and the one after that fires.
/// </summary>
/// <remarks>
/// The game's firing animation includes working the bolt, so the bolt is held by pausing the player's animator once the
/// recoil is over, and cycled by resuming it. Anything that needs the animator back lets the bolt cycle on its own:
/// lowering the rifle, putting it away, or the player losing control, as in a struggle. Only the rifle in the player's
/// hands is tracked, and only while <see cref="Settings.m_ManualBolt"/> is on.
/// </remarks>
internal static class ManualBolt
{
    /// <summary>The bolt of <see cref="s_GunItem"/>.</summary>
    private static readonly BoltCycle s_Bolt = new();

    /// <summary>The rifle whose bolt is tracked, or null while there's nothing to track.</summary>
    private static GunItem? s_GunItem;

    /// <summary>The animator paused to hold the bolt, or null when it isn't paused.</summary>
    private static Animator? s_PausedAnimator;

    /// <summary>The speed to give the paused animator when it resumes: the game's own speed for it.</summary>
    private static float s_AnimatorSpeed = 1f;

    /// <summary>Whether the game started a firing animation during the current trigger pull.</summary>
    private static bool s_FireAnimationStarted;

    /// <summary>Whether the game said that firing animation will jam the rifle.</summary>
    private static bool s_WillJam;

    /// <summary>Whether a gun has a bolt the player cycles by hand.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool AppliesTo(GunItem gunItem) =>
        Settings.Instance.m_ManualBolt && gunItem.m_GunType == GunType.Rifle && BoltCycle.IsBoltAction(gunItem.name);

    /// <summary>
    /// Handles a trigger pull on a rifle: says whether the game should fire, or cycles the bolt instead.
    /// </summary>
    /// <param name="shooter">The shooter whose trigger was pulled.</param>
    /// <param name="roundsBefore">The rounds in the rifle before the pull, or -1 when this doesn't track it.</param>
    /// <returns>Whether the game should go on to fire.</returns>
    internal static bool PullTrigger(vp_FPSShooter shooter, out int roundsBefore)
    {
        roundsBefore = -1;

        var gunItem = shooter.m_Weapon != null ? shooter.m_Weapon.m_GunItem : null;
        if (gunItem == null || !AppliesTo(gunItem))
            return true;

        if (gunItem != s_GunItem)
        {
            Release();
            s_GunItem = gunItem;
        }

        switch (s_Bolt.PullTrigger(Time.frameCount))
        {
            case TriggerAction.Fire:
                s_FireAnimationStarted = false;
                s_WillJam = false;
                roundsBefore = gunItem.NumRoundsInClip();
                return true;

            case TriggerAction.Cycle:
                if (Resume())
                    shooter.m_NextAllowedFireTime = Time.time + BoltCycle.SecondsUntilNextShot(gunItem.m_FiringRateSeconds);
                Mod.Logger.Log("Cycled the rifle's bolt.", FlaggedLoggingLevel.Trace);
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Starts holding the bolt if the trigger pull fired a round: it used one and played the firing animation, so a
    /// misfire that uses a round without firing isn't held.
    /// </summary>
    /// <param name="roundsBefore">The rounds before the pull, from <see cref="PullTrigger"/>.</param>
    internal static void AfterTrigger(int roundsBefore)
    {
        if (roundsBefore < 0 || s_GunItem == null || !s_FireAnimationStarted)
            return;

        var roundsAfter = s_GunItem.NumRoundsInClip();
        if (roundsAfter < roundsBefore)
            s_Bolt.Fired(Time.time, roundsAfter, s_WillJam);
    }

    /// <summary>Remembers that the game started a firing animation, and whether it will jam the rifle.</summary>
    /// <param name="willJam">What the game passed to the animation.</param>
    internal static void FireAnimationStarted(bool willJam)
    {
        s_FireAnimationStarted = true;
        s_WillJam = willJam;
    }

    /// <summary>
    /// Holds the bolt once the recoil is over, and lets it cycle if the rifle is no longer aimed in the player's hands.
    /// Called every frame.
    /// </summary>
    internal static void Update()
    {
        if (s_GunItem == null)
            return;

        // A ready bolt needs nothing until the next pull, which tracks the rifle again.
        if (s_Bolt.Phase == BoltPhase.Ready)
        {
            s_GunItem = null;
            return;
        }

        var playerManager = GameManager.GetPlayerManagerComponent();
        var playerAnimation = GameManager.GetPlayerAnimationComponent();

        if (!Settings.Instance.m_ManualBolt
            || playerManager == null
            || playerAnimation == null
            || playerManager.m_ItemInHands == null
            || playerManager.m_ItemInHands.m_GunItem != s_GunItem
            || playerManager.GetControlMode() != PlayerControlMode.Normal
            || playerAnimation.IsReloading())
        {
            Release();
            return;
        }

        if (s_Bolt.TryHold(Time.time))
            Pause(playerAnimation);

        KeepPaused();
    }

    /// <summary>Lets the bolt cycle on its own, resuming the animation if it was held, and stops tracking the rifle.</summary>
    internal static void Release()
    {
        s_Bolt.Release();
        Resume();
        s_GunItem = null;
    }

    /// <summary>
    /// Pauses the animator again if the game has changed its speed while the bolt is held, and remembers the game's
    /// speed to resume at.
    /// </summary>
    /// <remarks>Called after the game sets the animator's speed, and every frame in case it sets it elsewhere.</remarks>
    internal static void KeepPaused()
    {
        // Unity objects need == null, not ?., to tell when they've been destroyed.
        if (s_PausedAnimator == null || s_PausedAnimator.speed == 0f)
            return;

        s_AnimatorSpeed = s_PausedAnimator.speed;
        s_PausedAnimator.speed = 0f;
    }

    /// <summary>Pauses the player's animator to hold the bolt.</summary>
    /// <param name="playerAnimation">The player's animation component.</param>
    private static void Pause(PlayerAnimation playerAnimation)
    {
        var animator = playerAnimation.m_Animator;
        if (animator == null)
        {
            s_Bolt.Release();
            return;
        }

        s_AnimatorSpeed = animator.speed;
        animator.speed = 0f;
        s_PausedAnimator = animator;
        Mod.Logger.Log("Holding the rifle's bolt.", FlaggedLoggingLevel.Trace);
    }

    /// <summary>Resumes the player's animator if it was paused, which plays the rest of the bolt's motion.</summary>
    /// <returns>Whether it was paused.</returns>
    private static bool Resume()
    {
        // Unity objects need == null, not ?., to tell when they've been destroyed.
        if (s_PausedAnimator == null)
        {
            s_PausedAnimator = null;
            return false;
        }

        s_PausedAnimator.speed = s_AnimatorSpeed;
        s_PausedAnimator = null;
        return true;
    }
}
