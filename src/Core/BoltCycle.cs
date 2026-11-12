namespace AdaptiveArsenal.Core;

/// <summary>Where a rifle's bolt is between shots, for <see cref="BoltCycle"/>.</summary>
internal enum BoltPhase
{
    /// <summary>A round is chambered, or the rifle is empty: the trigger fires as the game would.</summary>
    Ready,

    /// <summary>The rifle has just fired and is recoiling. The bolt is held once this is over.</summary>
    Settling,

    /// <summary>The spent case is still in the chamber and the bolt is closed until the player cycles it.</summary>
    Held
}

/// <summary>What a trigger pull does, from <see cref="BoltCycle.PullTrigger"/>.</summary>
internal enum TriggerAction
{
    /// <summary>The rifle fires as the game would.</summary>
    Fire,

    /// <summary>The pull cycles the bolt instead of firing.</summary>
    Cycle,

    /// <summary>The trigger is still held from the last pull, so nothing happens.</summary>
    Ignore
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

    /// <summary>Where the bolt is.</summary>
    internal BoltPhase Phase { get; private set; } = BoltPhase.Ready;

    /// <summary>The time at which the bolt is held, while <see cref="Phase"/> is <see cref="BoltPhase.Settling"/>.</summary>
    private float m_HoldTime;

    /// <summary>
    /// The last frame the trigger was pulled on, to tell a new pull from one that's still held, or null before the
    /// first pull.
    /// </summary>
    private int? m_LastTriggerFrame;

    /// <summary>Whether the pull that cycled the bolt is still held, so it mustn't fire the next round.</summary>
    private bool m_CyclePullHeld;

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
    /// The game may ask to fire on every frame the trigger is held, so a pull only counts as new when the trigger
    /// wasn't pulled on the frame before. A held trigger never cycles the bolt, so the player can keep it pulled after a
    /// shot, and the pull that cycles the bolt never fires the next round, however long it's held.
    /// </remarks>
    /// <param name="frame">The current frame.</param>
    internal TriggerAction PullTrigger(int frame)
    {
        var isNewPull = m_LastTriggerFrame is not { } lastFrame || frame - lastFrame > 1;
        m_LastTriggerFrame = frame;

        if (isNewPull)
            m_CyclePullHeld = false;

        if (Phase == BoltPhase.Ready)
            return m_CyclePullHeld ? TriggerAction.Ignore : TriggerAction.Fire;

        if (!isNewPull)
            return TriggerAction.Ignore;

        Phase = BoltPhase.Ready;
        m_CyclePullHeld = true;
        return TriggerAction.Cycle;
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
    /// Lets the bolt cycle without a trigger pull, as when the player lowers the rifle or puts it away.
    /// </summary>
    /// <returns>Whether the bolt was held, so its animation needs resuming.</returns>
    internal bool Release()
    {
        var wasHeld = Phase == BoltPhase.Held;
        Phase = BoltPhase.Ready;
        return wasHeld;
    }
}
