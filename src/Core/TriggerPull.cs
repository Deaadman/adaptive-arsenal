namespace AdaptiveArsenal.Core;

/// <summary>
/// Follows the trigger from frame to frame: whether a pull is new or still held from before, and whether it has been
/// spent, so it mustn't fire.
/// </summary>
/// <remarks>
/// The game may ask to fire on every frame the trigger is held, so a pull only counts as new when the trigger wasn't
/// pulled on the frame before. A pull is spent once it has fired, cycled the bolt, or been blocked by the safety, and
/// only a new pull can fire after that. Frames are Unity frame counts, passed in so that this has no Unity types.
/// </remarks>
internal sealed class TriggerPull
{
    /// <summary>The last frame the trigger was pulled on, or null before the first pull.</summary>
    private int? m_LastFrame;

    /// <summary>Whether the current pull started on its frame, rather than being held from the frame before.</summary>
    internal bool IsNew { get; private set; }

    /// <summary>Whether the current pull has fired, cycled the bolt or been blocked, so it mustn't fire.</summary>
    internal bool IsSpent { get; private set; }

    /// <summary>Records that the trigger is pulled on a frame, starting a new pull if it wasn't pulled the frame before.</summary>
    /// <param name="frame">The current frame.</param>
    internal void Pull(int frame)
    {
        IsNew = m_LastFrame is not { } lastFrame || frame - lastFrame > 1;
        m_LastFrame = frame;

        if (IsNew)
            IsSpent = false;
    }

    /// <summary>Marks the current pull as spent, so it can't fire however long it's held.</summary>
    internal void Spend() => IsSpent = true;
}
