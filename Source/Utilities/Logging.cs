using System.Text;

namespace AdaptiveArsenal.Utilities;

internal abstract class Logging
{
    internal static void LogStarter()                                                     => Melon<Mod>.Logger.Msg($"Mod loaded with v{Properties.BuildInfo.Version}");
    internal static void LogSeperator(params object[] parameters)                         => Melon<Mod>.Logger.Msg("==============================================================================", parameters);
    internal static void LogIntraSeparator(string message, params object[] parameters)    => Melon<Mod>.Logger.Msg($"=========================   {message}   =========================", parameters);

    internal static void Log(string message, params object[] parameters)                  => Melon<Mod>.Logger.Msg(message, parameters);
    internal static void LogDebug(string message, params object[] parameters)             => Melon<Mod>.Logger.Msg($"[DEBUG] {message}", parameters);
    internal static void LogWarning(string message, params object[] parameters)           => Melon<Mod>.Logger.Warning(message, parameters);
    internal static void LogError(string message, params object[] parameters)             => Melon<Mod>.Logger.Error(message, parameters);
    internal static void LogException(string message, Exception exception, params object[] parameters)
    {
        StringBuilder sb = new();

        sb.Append("[EXCEPTION]");
        sb.Append(message);
        sb.AppendLine(exception.Message);

        Melon<Mod>.Logger.Error(sb.ToString(), Color.red, parameters);
    }
}