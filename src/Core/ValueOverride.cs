namespace AdaptiveArsenal.Core;

/// <summary>
/// The mod's change to a value the game owns, so it can be put back without undoing a change the game made since.
/// </summary>
/// <remarks>
/// <see cref="Set"/> remembers the value it replaced, unless that value is the one it set last time, so setting it
/// again keeps the first original. If the game has set something else since, that becomes the new original.
/// <see cref="Restore"/> only gives the original back while the value is still the mod's.
/// </remarks>
/// <typeparam name="T">The value's type. Compared with <see cref="EqualityComparer{T}.Default"/>, so exactly.</typeparam>
internal struct ValueOverride<T>
{
    /// <summary>Whether the mod has set the value and not put it back.</summary>
    private bool m_Active;

    /// <summary>The value before the mod set it.</summary>
    private T m_Original;

    /// <summary>The value the mod last set.</summary>
    private T m_Set;

    /// <summary>Whether the mod has set the value and not put it back.</summary>
    internal readonly bool IsActive => m_Active;

    /// <summary>The value before the mod set it, or the default if it hasn't.</summary>
    internal readonly T Original => m_Original;

    /// <summary>Whether <paramref name="current"/> is still the value the mod set.</summary>
    /// <param name="current">The value now.</param>
    internal readonly bool IsCurrent(T current) => m_Active && EqualityComparer<T>.Default.Equals(current, m_Set);

    /// <summary>
    /// Records the mod setting the value to <paramref name="value"/>, and returns it for the caller to write.
    /// </summary>
    /// <param name="current">The value now, which becomes the original unless it's the mod's own.</param>
    /// <param name="value">The value the mod sets.</param>
    internal T Set(T current, T value)
    {
        if (!IsCurrent(current))
            m_Original = current;

        m_Active = true;
        m_Set = value;
        return value;
    }

    /// <summary>
    /// Stops overriding the value, and gives back the original if the value is still the mod's.
    /// </summary>
    /// <param name="current">The value now.</param>
    /// <param name="original">The value to put back, if this returns <see langword="true"/>.</param>
    /// <returns>Whether to put <paramref name="original"/> back: <see langword="false"/> if the game has set something else since.</returns>
    internal bool Restore(T current, out T original)
    {
        var restore = IsCurrent(current);
        original = m_Original;
        m_Active = false;
        return restore;
    }
}
