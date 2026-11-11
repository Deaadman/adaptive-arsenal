using AdaptiveArsenal.Core;

using ModSettings;

namespace AdaptiveArsenal;

/// <summary>
/// The mod's page in the game's Mod Settings menu, saved to <c>Mods\AdaptiveArsenal.json</c>. The defaults are the
/// mod's own values, so players who never open it play the mod as designed.
/// </summary>
/// <remarks>
/// ModSettings shows each public field as a setting, so the fields here are public. The numbers that
/// <see cref="Ballistics"/> uses are copied into it by <see cref="Apply"/> when the game starts and whenever the player
/// confirms a change. The rest are read where they're used.
/// </remarks>
internal sealed class Settings : JsonModSettings
{
    /// <summary>
    /// The numbers in the settings file as it was before ModSettings loaded it, by field name, or
    /// <see langword="null"/> if there was no file, and why it couldn't be read if it couldn't.
    /// </summary>
    /// <remarks>
    /// It's declared before <see cref="Instance"/> so it's read first, as static fields are set in order, in case
    /// loading the file also writes it back without fields the settings no longer have.
    /// <see cref="UpdateOldSettings"/> uses it.
    /// </remarks>
    private static readonly (IReadOnlyDictionary<string, float>? Numbers, string? Error) s_SavedFile = ReadSavedFile();

    /// <summary>The settings, loaded from the file when the mod starts.</summary>
    internal static readonly Settings Instance = new();

    /// <summary>Whether rifles and revolvers fire the mod's bullets. Off, they fire the game's own instant shots.</summary>
    [Section("Bullets")]
    [Name("Real bullets")]
    [Description("Rifles and revolvers fire bullets that fly through the air. Turn off to use the game's own instant shots. The settings below only apply to real bullets.")]
    public bool m_ProjectilesEnabled = true;

    /// <summary>Whether bullets leave a trail.</summary>
    [Name("Bullet trails")]
    [Description("A trail follows each bullet, so you can see where it went.")]
    public bool m_TrailsEnabled = true;

    /// <summary>The longest a bullet's trail gets, in metres.</summary>
    [Name("Trail length")]
    [Description("How far behind the bullet its trail reaches, in metres.")]
    [Slider(10f, 500f, 50)]
    public float m_TrailLength = 200f;

    /// <summary>
    /// Whether the wind pushes bullets and the cold slows them, read from the game's weather when each bullet is fired.
    /// </summary>
    [Name("Weather affects bullets")]
    [Description("The wind pushes bullets sideways, and cold air and cold powder slow them, so they drop a little more. Off, bullets fly as in still air at 15 °C.")]
    public bool m_WeatherAffectsBullets = true;

    /// <summary>What every weapon's muzzle velocity is multiplied by: <see cref="Ballistics.MuzzleVelocityMultiplier"/>.</summary>
    [Name("Bullet speed")]
    [Description("Multiplies how fast every bullet flies. Faster bullets reach their target sooner and drop less.")]
    [Slider(0.25f, 3f, 12)]
    public float m_MuzzleVelocityMultiplier = 1f;

    /// <summary>What every shot's accuracy is multiplied by: <see cref="Ballistics.BaseAccuracyMultiplier"/>.</summary>
    [Section("Accuracy")]
    [Name("Base accuracy")]
    [Description("Multiplies the accuracy of every shot. Higher is more accurate.")]
    [Slider(0.5f, 2f, 31)]
    public float m_BaseAccuracyMultiplier = 1.2f;

    /// <summary>What accuracy is multiplied by when firing from the hip: <see cref="Ballistics.HipFireAccuracyMultiplier"/>.</summary>
    [Name("Hip fire")]
    [Description("Multiplies accuracy when firing without aiming. 1 means no penalty.")]
    [Slider(0f, 1f, 21)]
    public float m_HipFireAccuracyMultiplier = 0.7f;

    /// <summary>What accuracy is multiplied by when standing: <see cref="Ballistics.StandingAccuracyMultiplier"/>.</summary>
    [Name("Standing")]
    [Description("Multiplies accuracy when standing rather than crouching. 1 means no penalty.")]
    [Slider(0f, 1f, 21)]
    public float m_StandingAccuracyMultiplier = 0.8f;

    /// <summary>What accuracy is multiplied by when moving: <see cref="Ballistics.MovingAccuracyMultiplier"/>.</summary>
    [Name("Moving")]
    [Description("Multiplies accuracy when moving. 1 means no penalty.")]
    [Slider(0f, 1f, 21)]
    public float m_MovingAccuracyMultiplier = 0.9f;

    /// <summary>How far a shot with no accuracy can land from where it was aimed: <see cref="Ballistics.MaxInaccuracyDegrees"/>.</summary>
    [Name("Worst spread")]
    [Description("How far, in degrees, the least accurate shot can land from where you aimed, on top of the gun's own small spread. Most shots land much closer.")]
    [Slider(0f, 20f, 41)]
    public float m_MaxInaccuracyDegrees = Ballistics.DefaultMaxInaccuracyDegrees;

    /// <summary>How much a bullet's speed when it hits sets its damage: <see cref="Ballistics.DamageFromSpeed"/>.</summary>
    [Section("Damage")]
    [Name("Damage from speed")]
    [Description("How much a bullet's damage falls as it slows down. At 1 it does damage for the energy it has left when it hits, as a real bullet does. 0 means no drop-off.")]
    [Slider(0f, 1f, 21)]
    public float m_DamageFromSpeed = 1f;

    /// <summary>Whether the player can walk while aiming a revolver.</summary>
    [Section("Other")]
    [Name("Walk while aiming revolver")]
    [Description("You can walk while aiming a revolver, which the game doesn't otherwise allow.")]
    public bool m_RevolverWalkWhileAiming = true;

    /// <summary>Whether the HUD shows the rounds loaded in a rifle or revolver, and how many more the player is carrying.</summary>
    [Name("Show ammo count")]
    [Description("Shows how many rounds are loaded in your rifle or revolver on the HUD, and how many more you're carrying. Off, you'll have to keep count yourself.")]
    public bool m_ShowAmmoCount = true;

    /// <summary>The settings file's name, in the Mods folder, which ModSettings names after the mod's assembly.</summary>
    private const string SettingsFileName = "AdaptiveArsenal.json";

    /// <summary>The settings only real bullets use, hidden while <see cref="m_ProjectilesEnabled"/> is off.</summary>
    private static readonly string[] ProjectileOnlyFields =
    [
        nameof(m_TrailsEnabled),
        nameof(m_TrailLength),
        nameof(m_WeatherAffectsBullets),
        nameof(m_MuzzleVelocityMultiplier),
        nameof(m_BaseAccuracyMultiplier),
        nameof(m_HipFireAccuracyMultiplier),
        nameof(m_StandingAccuracyMultiplier),
        nameof(m_MovingAccuracyMultiplier),
        nameof(m_MaxInaccuracyDegrees),
        nameof(m_DamageFromSpeed)
    ];

    /// <summary>Adds the settings to the Mod Settings menu and puts the saved values into effect.</summary>
    internal static void Initialize()
    {
        Instance.UpdateOldSettings();
        Instance.AddToModSettings(Properties.BuildInfo.Name);
        Instance.RefreshFields();
        Instance.Apply();
    }

    /// <summary>
    /// Updates settings saved by an older version of the mod, once, with <see cref="SettingsMigration"/>, then saves
    /// them so they aren't updated again.
    /// </summary>
    /// <remarks>
    /// ModSettings looks to create the file with every setting the first time the game starts (it has a
    /// <c>LoadOrCreate</c>), which hasn't been checked. If so, a player who never opened the settings still has the old
    /// defaults saved, and they'd keep them without this.
    /// </remarks>
    private void UpdateOldSettings()
    {
        var (saved, error) = s_SavedFile;
        if (error is not null)
        {
            Mod.Logger.Log($"Couldn't read the settings file before loading it, so old settings aren't updated: {error}", FlaggedLoggingLevel.Warning);
            return;
        }

        var updated = SettingsMigration.Migrate(saved, new(m_DamageFromSpeed, m_MaxInaccuracyDegrees));
        if (updated is not { } values)
            return;

        Mod.Logger.Log(
            $"Updated settings saved by version {SettingsMigration.VersionOf(saved!)}: Damage from speed {m_DamageFromSpeed} -> {values.DamageFromSpeed:F2}, " +
            $"Worst spread {m_MaxInaccuracyDegrees} -> {values.MaxInaccuracyDegrees}.",
            FlaggedLoggingLevel.Debug);

        m_DamageFromSpeed = values.DamageFromSpeed;
        m_MaxInaccuracyDegrees = values.MaxInaccuracyDegrees;
        Save();
    }

    /// <summary>Reads the numbers in the settings file, by field name, for <see cref="s_SavedFile"/>.</summary>
    /// <returns>
    /// The numbers, or <see langword="null"/> for them if there's no file, and why the file couldn't be read if it
    /// couldn't. Nothing is logged here, since it runs before the mod's logger may be ready.
    /// </returns>
    private static (IReadOnlyDictionary<string, float>? Numbers, string? Error) ReadSavedFile()
    {
        try
        {
            var path = Path.Combine(MelonLoader.Utils.MelonEnvironment.ModsDirectory, SettingsFileName);
            if (!File.Exists(path))
                return (null, null);

            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (json.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                return (null, "it isn't a JSON object");

            Dictionary<string, float> numbers = [];
            foreach (var property in json.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Number && property.Value.TryGetSingle(out var value))
                    numbers[property.Name] = value;
            }

            return (numbers, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return (null, exception.Message);
        }
    }

    /// <inheritdoc />
    protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
    {
        if (field.Name == nameof(m_ProjectilesEnabled) && newValue is bool projectilesEnabled)
            RefreshFields(projectilesEnabled);
    }

    /// <inheritdoc />
    protected override void OnConfirm()
    {
        base.OnConfirm();
        Apply();
    }

    /// <summary>Copies the numbers <see cref="Ballistics"/> uses into it.</summary>
    private void Apply()
    {
        Ballistics.MuzzleVelocityMultiplier = m_MuzzleVelocityMultiplier;
        Ballistics.BaseAccuracyMultiplier = m_BaseAccuracyMultiplier;
        Ballistics.HipFireAccuracyMultiplier = m_HipFireAccuracyMultiplier;
        Ballistics.StandingAccuracyMultiplier = m_StandingAccuracyMultiplier;
        Ballistics.MovingAccuracyMultiplier = m_MovingAccuracyMultiplier;
        Ballistics.MaxInaccuracyDegrees = m_MaxInaccuracyDegrees;
        Ballistics.DamageFromSpeed = m_DamageFromSpeed;
    }

    /// <summary>Shows the settings only real bullets use when they're on, and hides them when they're off.</summary>
    /// <param name="projectilesEnabled">Whether real bullets are on, which may not be saved yet.</param>
    private void RefreshFields(bool? projectilesEnabled = null)
    {
        foreach (var fieldName in ProjectileOnlyFields)
            SetFieldVisible(fieldName, projectilesEnabled ?? m_ProjectilesEnabled);
    }
}
