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
    public override void OnInitializeMelon()
    {
        Settings.Initialize();
        Utilities.RuntimeAddressables.RegisterTypes();
        Logger.Log($"{Properties.BuildInfo.Name} {Properties.BuildInfo.Version} initialized.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>
    /// Once Addressables has started, which it has by the first scene, makes the mod's own items and lets the game
    /// load them. Both only happen once.
    /// </summary>
    /// <inheritdoc />
    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        Utilities.RuntimeAddressables.AddLocator();
        Utilities.TracerAmmo.Build();
    }
}
