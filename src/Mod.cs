namespace AdaptiveArsenal;

/// <summary>
/// The mod's entry point for MelonLoader. Most of what it does is in the Harmony patches in
/// <see cref="Patches"/>, which MelonLoader applies when it loads the mod. Each frame, it updates
/// <see cref="Utilities.Rifles"/> and <see cref="Utilities.ManualBolt"/>.
/// </summary>
internal sealed class Mod : MelonMod
{
    /// <summary>
    /// The mod's logger. It writes to the MelonLoader console and <c>MelonLoader\Latest.log</c>, at the levels the
    /// player turns on in Complex Logger's settings.
    /// </summary>
    internal static readonly ComplexLogger.ComplexLogger<Mod> Logger = new();

    /// <inheritdoc />
    public override void OnInitializeMelon()
    {
        Settings.Initialize();
        Logger.Log($"{Properties.BuildInfo.Name} {Properties.BuildInfo.Version} initialized.", FlaggedLoggingLevel.Debug);
    }

    /// <inheritdoc />
    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => Utilities.ManualBolt.SceneLoaded();

    /// <inheritdoc />
    public override void OnUpdate()
    {
        Utilities.Rifles.Update();
        Utilities.ManualBolt.Update();
    }
}
