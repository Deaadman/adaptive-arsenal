namespace AdaptiveArsenal;

/// <summary>
/// The mod's entry point for MelonLoader. Everything it does is in the Harmony patches in
/// <see cref="Patches"/>, which MelonLoader applies when it loads the mod.
/// </summary>
internal sealed class Mod : MelonMod
{
    /// <summary>
    /// The mod's logger. It writes to the MelonLoader console and <c>MelonLoader\Latest.log</c>, at the levels the
    /// player turns on in Complex Logger's settings.
    /// </summary>
    internal static readonly ComplexLogger.ComplexLogger<Mod> Logger = new();

    /// <inheritdoc />
    public override void OnInitializeMelon() =>
        Logger.Log($"{Properties.BuildInfo.Name} {Properties.BuildInfo.Version} initialized.", FlaggedLoggingLevel.Debug);
}
