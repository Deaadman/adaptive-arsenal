namespace AdaptiveArsenal.Core;

/// <summary>
/// The projectiles from one call to <c>Projectiles.Fire</c>, like the pellets of a shotgun shell: tells the caller once,
/// when the last of them is done, rather than once for each.
/// </summary>
internal sealed class ProjectileGroup
{
    /// <summary>How many of the projectiles aren't done yet.</summary>
    private int m_Remaining;

    /// <summary>Whether any of the projectiles done so far hit something.</summary>
    private bool m_AnyHit;

    /// <summary>Called once, when the last projectile is done, with whether any of them hit something.</summary>
    private readonly Action<bool>? m_OnFinished;

    /// <summary>Starts a group of projectiles.</summary>
    /// <param name="count">How many projectiles are in it. Fewer than one counts as one.</param>
    /// <param name="onFinished">Called once, when the last of them is done, with whether any of them hit something.</param>
    internal ProjectileGroup(int count, Action<bool>? onFinished)
    {
        m_Remaining = Math.Max(1, count);
        m_OnFinished = onFinished;
    }

    /// <summary>Records that one of the projectiles is done, and calls back if it was the last.</summary>
    /// <remarks>Calls after the last one are ignored, so the callback only ever runs once.</remarks>
    /// <param name="hit">Whether it hit something.</param>
    internal void ProjectileFinished(bool hit)
    {
        if (m_Remaining == 0)
            return;

        m_AnyHit |= hit;
        if (--m_Remaining == 0)
            m_OnFinished?.Invoke(m_AnyHit);
    }
}
