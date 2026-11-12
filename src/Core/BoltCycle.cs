namespace AdaptiveArsenal.Core;

/// <summary>Where a rifle's bolt is between shots, for <see cref="BoltCycle"/>.</summary>
internal enum BoltPhase
{
    /// <summary>A round is chambered, or the rifle is empty: the trigger fires as the game would.</summary>
    Ready,

    /// <summary>The rifle has just fired and is recoiling. The bolt is held once this is over.</summary>
    Settling,

    /// <summary>The spent case is still in the chamber and the bolt is closed until the player cycles it.</summary>
    Held,

    /// <summary>The round in the chamber misfired. The trigger only clicks until the player ejects it.</summary>
    Dud
}

/// <summary>What a trigger pull does, from <see cref="BoltCycle.PullTrigger"/>.</summary>
internal enum TriggerAction
{
    /// <summary>The rifle fires as the game would.</summary>
    Fire,

    /// <summary>The pull cycles the bolt instead of firing.</summary>
    Cycle,

    /// <summary>The firing pin falls on a dud, so the trigger clicks and nothing fires.</summary>
    Click,

    /// <summary>The trigger is still held from the last pull, so nothing happens.</summary>
    Ignore
}

/// <summary>What working the bolt by hand does, from <see cref="BoltCycle.CycleByHand"/>.</summary>
internal enum HandCycle
{
    /// <summary>Nothing: the rifle is empty.</summary>
    Nothing,

    /// <summary>Finishes cycling after a shot, ejecting the spent case.</summary>
    FinishCycle,

    /// <summary>Ejects the live round or dud in the chamber unfired, and chambers the next one.</summary>
    EjectRound
}

/// <summary>
/// The rules for cycling a bolt-action rifle by hand. After a shot, the bolt stays closed on the spent case until the
/// player pulls the trigger again, which cycles it, and the pull after that fires.
/// </summary>
/// <remarks>
/// The game's firing animation includes working the bolt, so the game-facing code holds the bolt by pausing that
/// animation once the recoil is over (<see cref="HoldDelaySeconds"/>) and resumes it when the bolt is cycled. Each
/// instance tracks the rifle in the player's hands. Times are in seconds and frames are Unity frame counts, passed in so
/// that this has no Unity types and the tests can run it.
/// </remarks>
internal sealed class BoltCycle
{
    /// <summary>How many rounds the chamber holds, on top of the magazine.</summary>
    internal const int ChamberRounds = 1;

    /// <summary>
    /// How long after a shot the bolt is held, in seconds: long enough for the recoil to play out, and before the
    /// firing animation starts opening the bolt.
    /// </summary>
    /// <remarks>This is a first guess. It needs tuning in game against the firing animation.</remarks>
    internal const float HoldDelaySeconds = 0.3f;

    /// <summary>
    /// How long the game's unjam animation gets to start after the bolt is worked to eject a round, in seconds, before
    /// the eject counts as stalled.
    /// </summary>
    internal const float EjectStartSeconds = 0.5f;

    /// <summary>Where the bolt is.</summary>
    internal BoltPhase Phase { get; private set; } = BoltPhase.Ready;

    /// <summary>
    /// Whether the bolt is ready to fire: closed on a live round, or the rifle is empty. Only then is the rifle cocked,
    /// so only then can the safety go on.
    /// </summary>
    internal bool IsReady => Phase == BoltPhase.Ready;

    /// <summary>The time at which the bolt is held, while <see cref="Phase"/> is <see cref="BoltPhase.Settling"/>.</summary>
    private float m_HoldTime;

    /// <summary>
    /// How many rounds a rifle holds with one in the chamber: its magazine, plus <see cref="ChamberRounds"/>.
    /// </summary>
    /// <param name="magazineSize">How many rounds the game lets the rifle hold.</param>
    internal static int CapacityWithChamber(int magazineSize) => magazineSize + ChamberRounds;

    /// <summary>Whether a weapon has a bolt to cycle: the game's own rifles, which are all bolt actions.</summary>
    /// <param name="gearItemName">The weapon's gear name, which may have a suffix like <c>(Clone)</c>.</param>
    internal static bool IsBoltAction(string gearItemName) => gearItemName.Contains("GEAR_Rifle");

    /// <summary>
    /// How long after the bolt is cycled the rifle can fire again, in seconds: the rest of the game's time between
    /// shots, after the part already spent before the bolt was held.
    /// </summary>
    /// <param name="firingRateSeconds">The game's time between shots for the rifle.</param>
    internal static float SecondsUntilNextShot(float firingRateSeconds) => MathF.Max(0f, firingRateSeconds - HoldDelaySeconds);

    /// <summary>
    /// Decides what a trigger pull does, and cycles the bolt if that's what it does.
    /// </summary>
    /// <remarks>
    /// Only a new pull cycles the bolt, so the player can keep the trigger held after a shot. A spent pull never fires,
    /// and cycling the bolt spends the pull, so it never fires the next round however long it's held. On a dud, each new
    /// pull only clicks.
    /// </remarks>
    /// <param name="pull">The trigger, already updated with <see cref="TriggerPull.Pull"/> for this frame.</param>
    internal TriggerAction PullTrigger(TriggerPull pull)
    {
        if (Phase == BoltPhase.Ready)
            return pull.IsSpent ? TriggerAction.Ignore : TriggerAction.Fire;

        if (!pull.IsNew)
            return TriggerAction.Ignore;

        pull.Spend();
        if (Phase == BoltPhase.Dud)
            return TriggerAction.Click;

        Phase = BoltPhase.Ready;
        return TriggerAction.Cycle;
    }

    /// <summary>
    /// Whether ejecting a round has stalled: the game's unjam animation isn't playing, and has had
    /// <see cref="EjectStartSeconds"/> to start. It either never started or was cut short, so the rifle mustn't stay
    /// marked jammed.
    /// </summary>
    /// <param name="secondsSinceEject">How long ago the bolt was worked.</param>
    /// <param name="unjamPlaying">Whether the unjam animation is playing.</param>
    internal static bool EjectHasStalled(float secondsSinceEject, bool unjamPlaying) =>
        !unjamPlaying && secondsSinceEject >= EjectStartSeconds;

    /// <summary>
    /// Decides what working the bolt by hand, rather than with the trigger, does, and readies the bolt.
    /// </summary>
    /// <remarks>
    /// After a shot, it finishes cycling, the same as the next trigger pull would. With a round or a dud in the
    /// chamber, it ejects that round unfired, as when clearing a dud or unloading a real rifle.
    /// </remarks>
    /// <param name="roundsInRifle">The rounds in the rifle, the chambered one included.</param>
    internal HandCycle CycleByHand(int roundsInRifle)
    {
        if (Phase == BoltPhase.Dud)
        {
            Phase = BoltPhase.Ready;
            return HandCycle.EjectRound;
        }

        if (Phase != BoltPhase.Ready)
        {
            Phase = BoltPhase.Ready;
            return HandCycle.FinishCycle;
        }

        return roundsInRifle > 0 ? HandCycle.EjectRound : HandCycle.Nothing;
    }

    /// <summary>
    /// Starts holding the bolt after a shot, unless the rifle is now empty or jammed, when the game's animation plays
    /// on as it would.
    /// </summary>
    /// <param name="time">The time of the shot.</param>
    /// <param name="roundsLeft">The rounds left in the rifle after the shot.</param>
    /// <param name="willJam">Whether the game is about to jam the rifle.</param>
    internal void Fired(float time, int roundsLeft, bool willJam)
    {
        if (roundsLeft < 1 || willJam)
            return;

        Phase = BoltPhase.Settling;
        m_HoldTime = time + HoldDelaySeconds;
    }

    /// <summary>
    /// Whether it's time to hold the bolt, which moves it from <see cref="BoltPhase.Settling"/> to
    /// <see cref="BoltPhase.Held"/>. It's true once, on the first call at or after the hold time.
    /// </summary>
    /// <param name="time">The current time.</param>
    internal bool TryHold(float time)
    {
        if (Phase != BoltPhase.Settling || time < m_HoldTime)
            return false;

        Phase = BoltPhase.Held;
        return true;
    }

    /// <summary>
    /// Lets the bolt cycle without a trigger pull, as when the player lowers the rifle or puts it away. A dud stays in
    /// the chamber, since only working the bolt by hand ejects it.
    /// </summary>
    /// <returns>Whether the bolt was held, so its animation needs resuming.</returns>
    internal bool Release()
    {
        var wasHeld = Phase == BoltPhase.Held;
        if (Phase != BoltPhase.Dud)
            Phase = BoltPhase.Ready;
        return wasHeld;
    }

    /// <summary>Records that the chambered round misfired, so the trigger only clicks until it's ejected.</summary>
    internal void Misfired() => Phase = BoltPhase.Dud;

    /// <summary>Starts following a different rifle, whose chamber holds a dud or a ready round.</summary>
    /// <param name="hasDud">Whether that rifle's chambered round misfired.</param>
    internal void Reset(bool hasDud) => Phase = hasDud ? BoltPhase.Dud : BoltPhase.Ready;
}
