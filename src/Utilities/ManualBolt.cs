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

    /// <summary>The rifle marked jammed so the game's unjam ejects its round, or null when there isn't one.</summary>
    private static GunItem? s_EjectingGun;

    /// <summary>When <see cref="s_EjectingGun"/>'s bolt was worked.</summary>
    private static float s_EjectStartTime;

    /// <summary>The rounds in <see cref="s_EjectingGun"/> when its bolt was worked, to tell when one has gone.</summary>
    private static int s_EjectRoundsBefore;

    /// <summary>
    /// The rifles with a dud in the chamber, so a dud stays when the player puts a rifle away and takes it out again.
    /// </summary>
    /// <remarks>
    /// It holds the rifles themselves, not their native addresses, so a destroyed rifle is pruned (it compares equal to
    /// null) rather than passing its dud on to a new rifle at the same address. It's cleared on every scene load,
    /// including loading a save, so duds aren't saved.
    /// </remarks>
    private static readonly List<GunItem> s_DudGuns = [];

    /// <summary>
    /// Whether the game's own misfire roll is turned off, while the game fires a rifle whose misfire this has already
    /// rolled.
    /// </summary>
    internal static bool SuppressGameMisfire { get; private set; }

    /// <summary>Whether a gun has a bolt the player cycles by hand.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool AppliesTo(GunItem gunItem) => Settings.Instance.m_ManualBolt && Rifles.IsBoltAction(gunItem);

    /// <summary>
    /// Handles a trigger pull on a rifle: says whether the game should fire, or cycles the bolt, misfires or clicks on a
    /// dud instead.
    /// </summary>
    /// <remarks>
    /// The misfire is rolled here, with the game's own roll for the next round (<see cref="GunItem.RollForMisfire"/>),
    /// and only for a pull the game will fire. A misfire leaves the round in the chamber as a dud, which the trigger
    /// only clicks on until the player ejects it with <see cref="CycleByHand"/>. When the round does fire, the game's
    /// own misfire roll is turned off for the shot (<see cref="SuppressGameMisfire"/>), so it isn't rolled twice.
    /// </remarks>
    /// <param name="shooter">The shooter whose trigger was pulled.</param>
    /// <param name="pull">The trigger, already updated for this frame.</param>
    /// <param name="roundsBefore">The rounds in the rifle before the pull, or -1 when this doesn't track it.</param>
    /// <returns>Whether the game should go on to fire.</returns>
    internal static bool PullTrigger(vp_FPSShooter shooter, TriggerPull pull, out int roundsBefore)
    {
        roundsBefore = -1;

        // A jammed rifle, including one being ejected from, is the game's to handle until it's cleared.
        var gunItem = shooter.m_Weapon != null ? shooter.m_Weapon.m_GunItem : null;
        if (gunItem == null || !AppliesTo(gunItem) || gunItem.m_IsJammed)
            return true;

        Track(gunItem);

        switch (s_Bolt.PullTrigger(pull))
        {
            case TriggerAction.Fire:
                if (ProjectileUtilities.WillFire(shooter) && RollForMisfire(gunItem))
                {
                    Misfire(gunItem, pull);
                    return false;
                }

                SuppressGameMisfire = true;
                s_FireAnimationStarted = false;
                s_WillJam = false;
                roundsBefore = gunItem.NumRoundsInClip();
                return true;

            case TriggerAction.Cycle:
                FinishHeldCycle(shooter, gunItem);
                Mod.Logger.Log("Cycled the rifle's bolt.", FlaggedLoggingLevel.Trace);
                return false;

            case TriggerAction.Click:
                if (ProjectileUtilities.CanPullTrigger(shooter))
                    gunItem.PlayDryFireAudio(gunItem.gameObject);
                return false;

            default:
                return false;
        }
    }

    /// <summary>Turns the game's own misfire roll back on once the game has handled a trigger pull.</summary>
    internal static void EndTrigger() => SuppressGameMisfire = false;

    /// <summary>
    /// Whether a rifle's bolt is ready to fire, so its safety can go on: not straight after a shot, before the bolt is
    /// cycled, and not with a dud in the chamber.
    /// </summary>
    /// <param name="gunItem">The rifle.</param>
    internal static bool IsBoltReady(GunItem gunItem)
    {
        if (!AppliesTo(gunItem))
            return true;

        return gunItem == s_GunItem ? s_Bolt.IsReady : !HasDud(gunItem);
    }

    /// <summary>
    /// Clears a rifle's dud once the game has reloaded it, since reloading opens the bolt, which pulls the dud out.
    /// </summary>
    /// <param name="gunItem">The rifle reloaded.</param>
    internal static void Reloaded(GunItem? gunItem)
    {
        if (gunItem == null || !HasDud(gunItem))
            return;

        s_DudGuns.Remove(gunItem);
        if (gunItem == s_GunItem)
            s_Bolt.Reset(hasDud: false);
        Mod.Logger.Log("Reloading cleared the dud from the rifle's chamber.", FlaggedLoggingLevel.Trace);
    }

    /// <summary>
    /// Forgets every rifle and dud, as the scene changes: on loading a save, the rifles are new objects, and duds
    /// aren't saved.
    /// </summary>
    internal static void SceneLoaded()
    {
        s_DudGuns.Clear();
        s_GunItem = null;
        s_EjectingGun = null;
        s_PausedAnimator = null;
        s_Bolt.Reset(hasDud: false);
    }

    /// <summary>
    /// Starts holding the bolt if the trigger pull fired a round: it used one and played the firing animation, so a
    /// misfire that uses a round without firing isn't held. A pull that fired is spent, so holding it never fires again.
    /// </summary>
    /// <param name="roundsBefore">The rounds before the pull, from <see cref="PullTrigger"/>.</param>
    /// <param name="pull">The trigger, already updated for this frame.</param>
    internal static void AfterTrigger(int roundsBefore, TriggerPull pull)
    {
        if (roundsBefore < 0 || s_GunItem == null || !s_FireAnimationStarted)
            return;

        var roundsAfter = s_GunItem.NumRoundsInClip();
        if (roundsAfter >= roundsBefore)
            return;

        pull.Spend();
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
    /// Works the rifle's bolt by hand, without the trigger. After a shot, it finishes cycling, the same as the next
    /// trigger pull would. With a round in the chamber, it ejects that round unfired, chambering the next one.
    /// </summary>
    /// <remarks>
    /// Ejecting a round uses the game's own unjam: the rifle is marked jammed, and the game's unjam animation works the
    /// bolt and clears the round, as it does for a jammed round. If the unjam doesn't start, or is cut short, the jam is
    /// taken off again (<see cref="UpdateEject"/>), and the save doesn't store it (<see cref="IsEjecting"/>). It works
    /// on any bolt-action rifle, even with <see cref="Settings.m_ManualBolt"/> off. Nothing happens with the safety on,
    /// while reloading, or while the rifle is already jammed, which the game's own reload clears.
    /// </remarks>
    /// <param name="gunItem">The rifle in the player's hands.</param>
    internal static void CycleByHand(GunItem gunItem)
    {
        if (!Rifles.IsBoltAction(gunItem) || gunItem.m_IsJammed)
            return;

        if (RifleSafety.IsOn(gunItem))
        {
            Rifles.ShowMessage("Safety on");
            return;
        }

        var playerAnimation = GameManager.GetPlayerAnimationComponent();
        var fpsCamera = GameManager.GetVpFPSCamera();
        if (playerAnimation == null || fpsCamera == null || playerAnimation.IsReloading())
            return;

        Track(gunItem);

        switch (s_Bolt.CycleByHand(gunItem.NumRoundsInClip()))
        {
            case HandCycle.FinishCycle:
                FinishHeldCycle(fpsCamera.m_CurrentShooter, gunItem);
                Mod.Logger.Log("Cycled the rifle's bolt by hand.", FlaggedLoggingLevel.Trace);
                break;

            case HandCycle.EjectRound:
                var weapon = fpsCamera.m_CurrentWeapon;
                if (weapon == null)
                    return;

                s_EjectRoundsBefore = gunItem.NumRoundsInClip();
                gunItem.m_IsJammed = true;
                playerAnimation.UpdateJamParameters(true);
                weapon.UnJam();
                s_EjectingGun = gunItem;
                s_EjectStartTime = Time.time;
                Mod.Logger.Log("Worked the rifle's bolt to eject the chambered round.", FlaggedLoggingLevel.Trace);
                break;
        }
    }

    /// <summary>Whether a gun is marked jammed only because its bolt is being worked to eject a round.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool IsEjecting(GunItem gunItem) => s_EjectingGun != null && gunItem == s_EjectingGun;

    /// <summary>
    /// Holds the bolt once the recoil is over, and lets it cycle if the rifle is no longer aimed in the player's hands.
    /// Called every frame.
    /// </summary>
    internal static void Update()
    {
        UpdateEject();

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

    /// <summary>
    /// Leaves the chambered round in a rifle as a dud: the trigger clicks, and it stays until the player ejects it.
    /// </summary>
    /// <param name="gunItem">The rifle.</param>
    /// <param name="pull">The trigger pull that misfired, which is spent.</param>
    private static void Misfire(GunItem gunItem, TriggerPull pull)
    {
        s_Bolt.Misfired();
        if (!HasDud(gunItem))
            s_DudGuns.Add(gunItem);
        pull.Spend();
        gunItem.PlayDryFireAudio(gunItem.gameObject);
        Rifles.ShowMessage("Misfire");
        Mod.Logger.Log("The rifle misfired, leaving a dud in the chamber.", FlaggedLoggingLevel.Trace);
    }

    /// <summary>
    /// Follows a rifle being ejected from: done once a round has gone from it, which clears any dud, and the jam taken
    /// off again if the unjam animation stalls before then, so a deliberate eject never leaves the rifle jammed and a
    /// dud that wasn't ejected stays.
    /// </summary>
    /// <remarks>
    /// It goes by the round count, not the jam flag, since the game may take the jam off when the unjam starts rather
    /// than when the round comes out.
    /// </remarks>
    private static void UpdateEject()
    {
        // Unity objects need == null, not ?., to tell when they've been destroyed.
        if (s_EjectingGun == null)
        {
            s_EjectingGun = null;
            return;
        }

        if (s_EjectingGun.NumRoundsInClip() < s_EjectRoundsBefore)
        {
            s_DudGuns.Remove(s_EjectingGun);
            s_EjectingGun = null;
            return;
        }

        var playerAnimation = GameManager.GetPlayerAnimationComponent();
        var unjamPlaying = playerAnimation != null && playerAnimation.IsReloading();
        if (!BoltCycle.EjectHasStalled(Time.time - s_EjectStartTime, unjamPlaying))
            return;

        if (s_EjectingGun.m_IsJammed)
        {
            s_EjectingGun.m_IsJammed = false;
            if (playerAnimation != null && s_EjectingGun == Rifles.InHands)
                playerAnimation.UpdateJamParameters(false);
        }

        // A dud that wasn't ejected is still in the chamber.
        if (s_EjectingGun == s_GunItem && HasDud(s_EjectingGun))
            s_Bolt.Reset(hasDud: true);

        s_EjectingGun = null;
        Mod.Logger.Log("The rifle's unjam didn't run to the end, so nothing was ejected.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>
    /// Rolls whether the next round misfires, with the game's own roll, keeping the game's misfire flag as it was so
    /// the game doesn't also react to the mod's misfire.
    /// </summary>
    /// <param name="gunItem">The rifle.</param>
    private static bool RollForMisfire(GunItem gunItem)
    {
        var hadMisfired = gunItem.m_HasMisfired;
        var misfires = gunItem.RollForMisfire();
        gunItem.m_HasMisfired = hadMisfired;

        Mod.Logger.Log($"Rolled for a misfire on round condition {gunItem.GetAmmoCondition(0)}: {(misfires ? "misfire" : "fires")}.", FlaggedLoggingLevel.Debug);
        return misfires;
    }

    /// <summary>Whether a rifle has a dud in the chamber, pruning destroyed rifles first.</summary>
    /// <param name="gunItem">The rifle.</param>
    private static bool HasDud(GunItem gunItem)
    {
        // Unity objects need == null, not ?., to tell when they've been destroyed.
        s_DudGuns.RemoveAll(dudGun => dudGun == null);
        return s_DudGuns.Exists(dudGun => dudGun == gunItem);
    }

    /// <summary>
    /// Starts tracking a rifle's bolt, letting the last tracked rifle's bolt cycle if it's a different one, and picks up
    /// the new rifle's dud if it has one.
    /// </summary>
    /// <param name="gunItem">The rifle.</param>
    private static void Track(GunItem gunItem)
    {
        if (gunItem == s_GunItem)
            return;

        Release();
        s_GunItem = gunItem;
        s_Bolt.Reset(HasDud(gunItem));
    }

    /// <summary>
    /// Finishes cycling a held bolt: resumes its animation, and gives the rifle the rest of its time between shots.
    /// </summary>
    /// <param name="shooter">The rifle's shooter, if there is one.</param>
    /// <param name="gunItem">The rifle.</param>
    private static void FinishHeldCycle(vp_FPSShooter? shooter, GunItem gunItem)
    {
        if (Resume() && shooter != null)
            shooter.m_NextAllowedFireTime = Time.time + BoltCycle.SecondsUntilNextShot(gunItem.m_FiringRateSeconds);
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
