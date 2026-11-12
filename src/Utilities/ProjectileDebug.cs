namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Logging for the mod's bullets, which the Debug settings turn up: with <see cref="Settings.m_DebugLogShots"/> on,
/// every shot is logged whatever levels Complex Logger has on; off, the same lines go to its Debug level.
/// </summary>
/// <remarks>
/// Callers check <see cref="Enabled"/> before building a line, so shots cost nothing extra when no one will read it.
/// </remarks>
internal static class ProjectileDebug
{
    /// <summary>Whether shots are logged whatever Complex Logger's levels: <see cref="Settings.m_DebugLogShots"/>.</summary>
    internal static bool LogShots => Settings.Instance.m_DebugLogShots;

    /// <summary>Whether bullets log every physics step: <see cref="Settings.m_DebugLogSteps"/>.</summary>
    internal static bool LogSteps => Settings.Instance.m_DebugLogSteps;

    /// <summary>Whether bullets' paths are drawn: <see cref="Settings.m_DebugDrawPaths"/>.</summary>
    internal static bool DrawPaths => Settings.Instance.m_DebugDrawPaths;

    /// <summary>
    /// Whether a line from <see cref="Log"/> would be written: <see cref="LogShots"/> is on, or Complex Logger's Debug
    /// level is.
    /// </summary>
    internal static bool Enabled => LogShots || ComplexLogger.Main.CurrentLevel.HasFlag(FlaggedLoggingLevel.Debug);

    /// <summary>
    /// Logs a line about a shot: always with <see cref="LogShots"/> on, and at Complex Logger's Debug level without it.
    /// Check <see cref="Enabled"/> first, so the line isn't built for nothing.
    /// </summary>
    /// <param name="message">The line.</param>
    internal static void Log(string message) =>
        Write(message, LogShots ? FlaggedLoggingLevel.Always : FlaggedLoggingLevel.Debug);

    /// <summary>
    /// Logs a bullet's physics step, whatever Complex Logger's levels, since <see cref="LogSteps"/> was turned on to see
    /// them.
    /// </summary>
    /// <param name="message">The line.</param>
    internal static void LogStep(string message) => Write(message, FlaggedLoggingLevel.Always);

    /// <summary>Writes a line to the log.</summary>
    /// <param name="message">The line. Braces are swapped for brackets, since Complex Logger formats it.</param>
    /// <param name="level">The level to write it at.</param>
    private static void Write(string message, FlaggedLoggingLevel level) =>
        Mod.Logger.Log(message.Replace('{', '(').Replace('}', ')'), level);
}
