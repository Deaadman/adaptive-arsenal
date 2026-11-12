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

    /// <summary>What accuracy is multiplied by when lying prone: <see cref="Ballistics.ProneAccuracyMultiplier"/>.</summary>
    [Name("Prone")]
    [Description("Multiplies accuracy when lying prone, on top of not standing. 1 means no bonus.")]
    [Slider(1f, 2f, 21)]
    public float m_ProneAccuracyMultiplier = 1.2f;

    /// <summary>What accuracy is multiplied by when moving: <see cref="Ballistics.MovingAccuracyMultiplier"/>.</summary>
    [Name("Moving")]
    [Description("Multiplies accuracy when moving. 1 means no penalty.")]
    [Slider(0f, 1f, 21)]
    public float m_MovingAccuracyMultiplier = 0.9f;

    /// <summary>How far a shot with no accuracy can land from where it was aimed: <see cref="Ballistics.MaxInaccuracyDegrees"/>.</summary>
    [Name("Worst spread")]
    [Description("How far, in degrees, the least accurate shot can land from where you aimed.")]
    [Slider(0f, 20f, 41)]
    public float m_MaxInaccuracyDegrees = 10f;

    /// <summary>The distance over which damage falls to its lowest: <see cref="Ballistics.DamageFalloffRange"/>.</summary>
    [Section("Damage")]
    [Name("Drop-off distance")]
    [Description("How far, in metres, a bullet flies before its damage has fallen as far as it will.")]
    [Slider(100f, 1000f, 19)]
    public float m_DamageFalloffRange = 500f;

    /// <summary>The share of its damage a bullet keeps at long range: <see cref="Ballistics.MinDamageFraction"/>.</summary>
    [Name("Long-range damage")]
    [Description("How much of its damage a bullet still does at the drop-off distance and beyond. 1 means no drop-off.")]
    [Slider(0f, 1f, 21)]
    public float m_MinDamageFraction = 0.1f;

    /// <summary>Whether the player can sprint while crouched.</summary>
    [Section("Movement")]
    [Name("Sprint while crouched")]
    [Description("Sprinting while crouched keeps you crouched and moves you faster, using stamina as sprinting does. Off, sprinting stands you up as usual.")]
    public bool m_CrouchSprintEnabled = true;

    /// <summary>How much faster than crouch-walking sprinting crouched is: <see cref="Movement.CrouchSprintSpeedMultiplier"/>.</summary>
    [Name("Crouched sprint speed")]
    [Description("How many times faster than crouch-walking you move when sprinting while crouched.")]
    [Slider(1f, 3f, 21)]
    public float m_CrouchSprintSpeedMultiplier = 1.75f;

    /// <summary>Whether crouching while sprinting starts a slide.</summary>
    [Name("Sliding")]
    [Description("Crouching while sprinting slides you along the ground. Slides go further downhill and stop sooner uphill.")]
    public bool m_SlideEnabled = true;

    /// <summary>How far a slide carries the player: <see cref="Movement.SlideDistanceMultiplier"/>.</summary>
    [Name("Slide distance")]
    [Description("Multiplies how far a slide carries you.")]
    [Slider(0.5f, 2f, 16)]
    public float m_SlideDistanceMultiplier = 1f;

    /// <summary>Whether the player can lie prone.</summary>
    [Name("Prone")]
    [Description("A key lies you down, lower than crouching: slower, harder for animals to see and hear, and steadier to shoot from. Sprinting or standing up ends it.")]
    public bool m_ProneEnabled = true;

    /// <summary>The key that lies the player down and gets them back up to a crouch.</summary>
    [Name("Prone key")]
    [Description("Lies you down, or gets you back up to a crouch if there's room.")]
    public KeyCode m_ProneKey = KeyCode.Z;

    /// <summary>How fast the player moves prone, compared to crouch-walking: <see cref="Movement.ProneSpeedMultiplier"/>.</summary>
    [Name("Prone speed")]
    [Description("Multiplies your crouch-walking speed while prone.")]
    [Slider(0.1f, 1f, 19)]
    public float m_ProneSpeedMultiplier = 0.4f;

    /// <summary>Whether the player can walk while aiming a revolver.</summary>
    [Section("Other")]
    [Name("Walk while aiming revolver")]
    [Description("You can walk while aiming a revolver, which the game doesn't otherwise allow.")]
    public bool m_RevolverWalkWhileAiming = true;

    /// <summary>Whether the HUD shows the rounds loaded in a rifle or revolver, and how many more the player is carrying.</summary>
    [Name("Show ammo count")]
    [Description("Shows how many rounds are loaded in your rifle or revolver on the HUD, and how many more you're carrying. Off, you'll have to keep count yourself.")]
    public bool m_ShowAmmoCount = true;

    /// <summary>
    /// The settings that only matter while a switch is on, hidden while it's off: each switch, how to read it, and the
    /// settings that depend on it.
    /// </summary>
    private static readonly (string Switch, Func<Settings, bool> IsOn, string[] Dependents)[] DependentFields =
    [
        (nameof(m_ProjectilesEnabled), settings => settings.m_ProjectilesEnabled,
        [
            nameof(m_TrailsEnabled),
            nameof(m_TrailLength),
            nameof(m_MuzzleVelocityMultiplier),
            nameof(m_BaseAccuracyMultiplier),
            nameof(m_HipFireAccuracyMultiplier),
            nameof(m_StandingAccuracyMultiplier),
            nameof(m_ProneAccuracyMultiplier),
            nameof(m_MovingAccuracyMultiplier),
            nameof(m_MaxInaccuracyDegrees),
            nameof(m_DamageFalloffRange),
            nameof(m_MinDamageFraction)
        ]),
        (nameof(m_CrouchSprintEnabled), settings => settings.m_CrouchSprintEnabled, [nameof(m_CrouchSprintSpeedMultiplier)]),
        (nameof(m_SlideEnabled), settings => settings.m_SlideEnabled, [nameof(m_SlideDistanceMultiplier)]),
        (nameof(m_ProneEnabled), settings => settings.m_ProneEnabled, [nameof(m_ProneKey), nameof(m_ProneSpeedMultiplier)])
    ];

    /// <summary>Adds the settings to the Mod Settings menu and puts the saved values into effect.</summary>
    internal static void Initialize()
    {
        Instance.AddToModSettings(Properties.BuildInfo.Name);
        foreach (var (_, isOn, dependents) in DependentFields)
            Instance.SetFieldsVisible(dependents, isOn(Instance));
        Instance.Apply();
    }

    /// <inheritdoc />
    protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
    {
        if (newValue is not bool isOn)
            return;

        foreach (var (switchName, _, dependents) in DependentFields)
        {
            if (field.Name == switchName)
                SetFieldsVisible(dependents, isOn);
        }
    }

    /// <inheritdoc />
    protected override void OnConfirm()
    {
        base.OnConfirm();
        Apply();
    }

    /// <summary>Copies the numbers <see cref="Ballistics"/> and <see cref="Movement"/> use into them.</summary>
    private void Apply()
    {
        Ballistics.MuzzleVelocityMultiplier = m_MuzzleVelocityMultiplier;
        Ballistics.BaseAccuracyMultiplier = m_BaseAccuracyMultiplier;
        Ballistics.HipFireAccuracyMultiplier = m_HipFireAccuracyMultiplier;
        Ballistics.StandingAccuracyMultiplier = m_StandingAccuracyMultiplier;
        Ballistics.ProneAccuracyMultiplier = m_ProneAccuracyMultiplier;
        Ballistics.MovingAccuracyMultiplier = m_MovingAccuracyMultiplier;
        Ballistics.MaxInaccuracyDegrees = m_MaxInaccuracyDegrees;
        Ballistics.DamageFalloffRange = m_DamageFalloffRange;
        Ballistics.MinDamageFraction = m_MinDamageFraction;
        Movement.CrouchSprintSpeedMultiplier = m_CrouchSprintSpeedMultiplier;
        Movement.SlideDistanceMultiplier = m_SlideDistanceMultiplier;
        Movement.ProneSpeedMultiplier = m_ProneSpeedMultiplier;
    }

    /// <summary>Shows or hides some settings.</summary>
    /// <param name="fieldNames">The settings' field names.</param>
    /// <param name="visible">Whether to show them.</param>
    private void SetFieldsVisible(string[] fieldNames, bool visible)
    {
        foreach (var fieldName in fieldNames)
            SetFieldVisible(fieldName, visible);
    }
}
