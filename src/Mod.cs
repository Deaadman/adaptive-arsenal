namespace AdaptiveArsenal;

/// <summary>
/// The mod's entry point for MelonLoader. Most of what it does is in the Harmony patches in
/// <see cref="Patches"/>, which MelonLoader applies when it loads the mod. It sets up the settings and loads the
/// weapon skins.
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
    /// <remarks>Loads the weapon skins here, once Unity is running, so their textures can be made.</remarks>
    public override void OnLateInitializeMelon()
    {
        Utilities.WeaponSkins.Initialize();
        Utilities.SkinConsoleCommand.Register();
    }

    /// <inheritdoc />
    /// <remarks>Freezes and thaws the rifles the player carries.</remarks>
    public override void OnUpdate() => Utilities.FrozenRifles.Update();
}
