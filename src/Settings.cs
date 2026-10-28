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

    /// <summary>Whether rifles freeze in the cold and jam more when frozen: <see cref="Utilities.FrozenRifles"/>.</summary>
    [Section("Frozen rifles")]
    [Name("Frozen rifles")]
    [Description("Rifles you carry freeze in the cold, and the more frozen they are, the more likely they are to jam. They thaw when it's warm, like indoors or by a fire.")]
    public bool m_FrozenRiflesEnabled = true;

    /// <summary>How long a rifle takes to freeze solid: <see cref="RifleFreezing.HoursToFreeze"/>.</summary>
    [Name("Hours to freeze")]
    [Description("How many in-game hours a rifle takes to freeze solid at -20°C or colder. It freezes more slowly when it's less cold.")]
    [Slider(1f, 24f, 47)]
    public float m_HoursToFreeze = 6f;

    /// <summary>How long a frozen rifle takes to thaw: <see cref="RifleFreezing.HoursToThaw"/>.</summary>
    [Name("Hours to thaw")]
    [Description("How many in-game hours a frozen-solid rifle takes to thaw at 10°C. It thaws more slowly just above freezing, and faster by a fire.")]
    [Slider(0.5f, 12f, 24)]
    public float m_HoursToThaw = 2f;

    /// <summary>The extra jam chance of a frozen-solid rifle: <see cref="RifleFreezing.MaxJamChance"/>.</summary>
    [Name("Frozen jam chance")]
    [Description("The extra chance, in percent, that a frozen-solid rifle jams on each shot. A partly frozen rifle gets part of it.")]
    [Slider(0f, 100f, 21)]
    public float m_FrozenJamChance = 25f;

    /// <summary>Whether the player can walk while aiming a revolver.</summary>
    [Section("Other")]
    [Name("Walk while aiming revolver")]
    [Description("You can walk while aiming a revolver, which the game doesn't otherwise allow.")]
    public bool m_RevolverWalkWhileAiming = true;

    /// <summary>Whether the HUD shows the rounds loaded in a rifle or revolver, and how many more the player is carrying.</summary>
    [Name("Show ammo count")]
    [Description("Shows how many rounds are loaded in your rifle or revolver on the HUD, and how many more you're carrying. Off, you'll have to keep count yourself.")]
    public bool m_ShowAmmoCount = false;

    /// <summary>Whether the first-person view is mirrored so the player holds things in their left hand: <see cref="Components.LeftHandedView"/>.</summary>
    [Name("Left-handed")]
    [Description("Mirrors your hands and what's in them, so you hold weapons and tools in your left hand. Writing on them reads backwards.")]
    public bool m_LeftHanded = false;

    /// <summary>Whether the view zooms in when aiming a rifle, as the game does. Off, the field of view stays as it is.</summary>
    [Name("Zoom when aiming rifle")]
    [Description("The view zooms in when you aim a rifle, as the game does. Turn off to keep your field of view while aiming.")]
    public bool m_RifleAimZoom = true;

    /// <summary>The settings only real bullets use, hidden while <see cref="m_ProjectilesEnabled"/> is off.</summary>
    private static readonly string[] ProjectileOnlyFields =
    [
        nameof(m_TrailsEnabled),
        nameof(m_TrailLength),
        nameof(m_MuzzleVelocityMultiplier),
        nameof(m_BaseAccuracyMultiplier),
        nameof(m_HipFireAccuracyMultiplier),
        nameof(m_StandingAccuracyMultiplier),
        nameof(m_MovingAccuracyMultiplier),
        nameof(m_MaxInaccuracyDegrees),
        nameof(m_DamageFalloffRange),
        nameof(m_MinDamageFraction)
    ];

    /// <summary>The settings only frozen rifles use, hidden while <see cref="m_FrozenRiflesEnabled"/> is off.</summary>
    private static readonly string[] FrozenRifleOnlyFields =
    [
        nameof(m_HoursToFreeze),
        nameof(m_HoursToThaw),
        nameof(m_FrozenJamChance)
    ];

    /// <summary>Adds the settings to the Mod Settings menu and puts the saved values into effect.</summary>
    internal static void Initialize()
    {
        Instance.AddToModSettings(Properties.BuildInfo.Name);
        Instance.RefreshFields();
        Instance.Apply();
    }

    /// <inheritdoc />
    protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
    {
        if (field.Name == nameof(m_ProjectilesEnabled) && newValue is bool projectilesEnabled)
            RefreshFields(projectilesEnabled: projectilesEnabled);
        else if (field.Name == nameof(m_FrozenRiflesEnabled) && newValue is bool frozenRiflesEnabled)
            RefreshFields(frozenRiflesEnabled: frozenRiflesEnabled);
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
        Ballistics.DamageFalloffRange = m_DamageFalloffRange;
        Ballistics.MinDamageFraction = m_MinDamageFraction;
        RifleFreezing.HoursToFreeze = m_HoursToFreeze;
        RifleFreezing.HoursToThaw = m_HoursToThaw;
        RifleFreezing.MaxJamChance = m_FrozenJamChance;
    }

    /// <summary>
    /// Shows the settings only real bullets and frozen rifles use when they're on, and hides them when they're off.
    /// </summary>
    /// <param name="projectilesEnabled">Whether real bullets are on, which may not be saved yet.</param>
    /// <param name="frozenRiflesEnabled">Whether frozen rifles are on, which may not be saved yet.</param>
    private void RefreshFields(bool? projectilesEnabled = null, bool? frozenRiflesEnabled = null)
    {
        foreach (var fieldName in ProjectileOnlyFields)
            SetFieldVisible(fieldName, projectilesEnabled ?? m_ProjectilesEnabled);

        foreach (var fieldName in FrozenRifleOnlyFields)
            SetFieldVisible(fieldName, frozenRiflesEnabled ?? m_FrozenRiflesEnabled);
    }
}
