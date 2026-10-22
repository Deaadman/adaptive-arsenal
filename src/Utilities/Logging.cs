using System.Text;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Writes to the MelonLoader console and <c>MelonLoader\Latest.log</c>, under the mod's name.
/// </summary>
internal static class Logging
{
    /// <summary>Logs a message.</summary>
    /// <param name="message">The message, which may have format items like <c>{0}</c>.</param>
    /// <param name="parameters">The values for the format items.</param>
    internal static void Log(string message, params object[] parameters) => Melon<Mod>.Logger.Msg(message, parameters);

    /// <summary>Logs a message marked <c>[DEBUG]</c>.</summary>
    /// <inheritdoc cref="Log" path="/param"/>
    internal static void LogDebug(string message, params object[] parameters) => Melon<Mod>.Logger.Msg($"[DEBUG] {message}", parameters);

    /// <summary>Logs a warning.</summary>
    /// <inheritdoc cref="Log" path="/param"/>
    internal static void LogWarning(string message, params object[] parameters) => Melon<Mod>.Logger.Warning(message, parameters);

    /// <summary>Logs an error.</summary>
    /// <inheritdoc cref="Log" path="/param"/>
    internal static void LogError(string message, params object[] parameters) => Melon<Mod>.Logger.Error(message, parameters);

    /// <summary>Logs an error marked <c>[EXCEPTION]</c>, followed by the exception's message.</summary>
    /// <param name="message">What was happening when the exception was thrown.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="parameters">The values for any format items in <paramref name="message"/>.</param>
    internal static void LogException(string message, Exception exception, params object[] parameters)
    {
        StringBuilder text = new();

        text.Append("[EXCEPTION] ");
        text.Append(message);
        text.Append(": ");
        text.AppendLine(exception.Message);

        Melon<Mod>.Logger.Error(text.ToString(), Color.red, parameters);
    }
}
