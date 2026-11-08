namespace AdaptiveArsenal.Core;

/// <summary>
/// The projectiles from one call to <c>Projectiles.Fire</c>, like the pellets of a shotgun shell. Collects what they
/// hit and reports it all at once: when the last of them is done, or when the time limit runs out, whichever is first.
/// </summary>
/// <remarks>
/// A projectile that misses can fly for seconds before it's removed, so without the time limit one pellet into the sky
/// would hold back the whole shot. Projectiles still flying when it runs out count as misses.
/// </remarks>
/// <typeparam name="THit">What a hit is reported as.</typeparam>
internal sealed class ProjectileGroup<THit>
{
    /// <summary>How many projectiles were fired.</summary>
    private readonly int m_Fired;

    /// <summary>When the result is reported even if projectiles are still flying, or infinity for no time limit.</summary>
    private readonly float m_Deadline;

    /// <summary>Called once, with how many were fired and what they hit.</summary>
    private readonly Action<int, IReadOnlyList<THit>> m_OnResult;

    /// <summary>What the projectiles have hit so far.</summary>
    private readonly List<THit> m_Hits = [];

    /// <summary>How many of the projectiles haven't hit or been removed yet.</summary>
    private int m_Remaining;

    /// <summary>Whether the result has been reported, after which nothing more is collected.</summary>
    private bool m_Reported;

    /// <summary>Starts a group of projectiles, fired now.</summary>
    /// <param name="count">How many projectiles are in it. Fewer than one counts as one.</param>
    /// <param name="now">The time they're fired, in seconds.</param>
    /// <param name="timeLimit">
    /// The most seconds to wait for them before reporting. 0 or less waits until every one is done.
    /// </param>
    /// <param name="onResult">Called once, with how many were fired and what they hit.</param>
    internal ProjectileGroup(int count, float now, float timeLimit, Action<int, IReadOnlyList<THit>> onResult)
    {
        m_Fired = Math.Max(1, count);
        m_Remaining = m_Fired;
        m_Deadline = timeLimit > 0f ? now + timeLimit : float.PositiveInfinity;
        m_OnResult = onResult;
    }

    /// <summary>Whether the result has been reported.</summary>
    internal bool IsReported => m_Reported;

    /// <summary>Records that one of the projectiles hit something.</summary>
    /// <param name="hit">What it hit.</param>
    /// <param name="now">The time it hit, in seconds. After the time limit, it counts as a miss.</param>
    internal void ProjectileHit(THit hit, float now)
    {
        Update(now);
        if (m_Reported)
            return;

        m_Hits.Add(hit);
        ProjectileDone();
    }

    /// <summary>Records that one of the projectiles was removed without hitting anything.</summary>
    /// <param name="now">The time it was removed, in seconds.</param>
    internal void ProjectileMissed(float now)
    {
        Update(now);
        if (!m_Reported)
            ProjectileDone();
    }

    /// <summary>Reports the result if the time limit has run out. Call it every frame while projectiles fly.</summary>
    /// <param name="now">The time now, in seconds.</param>
    internal void Update(float now)
    {
        if (!m_Reported && now >= m_Deadline)
            Report();
    }

    /// <summary>Counts one more projectile as done, and reports the result if it was the last.</summary>
    private void ProjectileDone()
    {
        if (--m_Remaining == 0)
            Report();
    }

    /// <summary>Reports the result, once.</summary>
    private void Report()
    {
        m_Reported = true;
        m_OnResult(m_Fired, m_Hits);
    }
}
