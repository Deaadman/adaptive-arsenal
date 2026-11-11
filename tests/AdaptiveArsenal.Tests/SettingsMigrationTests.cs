using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class SettingsMigrationTests
{
    private const int Precision = 4;

    /// <summary>What ModSettings loads from a 1.1.0 file: the new fields' defaults, and Worst spread as saved.</summary>
    private static readonly MigratedSettings Loaded = new(DamageFromSpeed: 1f, MaxInaccuracyDegrees: 10f);

    /// <summary>A settings file saved by 1.1.0 that the player never changed.</summary>
    private static Dictionary<string, float> Version1Defaults() => new()
    {
        ["m_TrailLength"] = 200f,
        ["m_MuzzleVelocityMultiplier"] = 1f,
        ["m_BaseAccuracyMultiplier"] = 1.2f,
        ["m_HipFireAccuracyMultiplier"] = 0.7f,
        ["m_StandingAccuracyMultiplier"] = 0.8f,
        ["m_MovingAccuracyMultiplier"] = 0.9f,
        ["m_MaxInaccuracyDegrees"] = 10f,
        ["m_DamageFalloffRange"] = 500f,
        ["m_MinDamageFraction"] = 0.1f
    };

    [Fact]
    public void A_file_without_damage_from_speed_is_version_1() =>
        Assert.Equal(1, SettingsMigration.VersionOf(Version1Defaults()));

    [Fact]
    public void A_file_with_damage_from_speed_is_current() =>
        Assert.Equal(SettingsMigration.CurrentVersion, SettingsMigration.VersionOf(new Dictionary<string, float> { ["m_DamageFromSpeed"] = 0.5f }));

    [Fact]
    public void No_file_needs_no_update()
    {
        Assert.Null(SettingsMigration.Migrate(null, Loaded));
        Assert.Null(SettingsMigration.Migrate(new Dictionary<string, float>(), Loaded));
    }

    [Fact]
    public void A_current_file_needs_no_update() =>
        Assert.Null(SettingsMigration.Migrate(new Dictionary<string, float> { ["m_DamageFromSpeed"] = 0.5f, ["m_MaxInaccuracyDegrees"] = 10f }, Loaded));

    [Fact]
    public void Untouched_1_1_0_settings_get_the_new_defaults()
    {
        var updated = SettingsMigration.Migrate(Version1Defaults(), Loaded);

        Assert.NotNull(updated);
        Assert.Equal(1f, updated.Value.DamageFromSpeed, Precision);
        Assert.Equal(Ballistics.DefaultMaxInaccuracyDegrees, updated.Value.MaxInaccuracyDegrees, Precision);
    }

    [Fact]
    public void Long_range_damage_turned_off_carries_over_as_no_drop_off()
    {
        var saved = Version1Defaults();
        saved["m_MinDamageFraction"] = 1f;

        Assert.Equal(0f, SettingsMigration.Migrate(saved, Loaded)!.Value.DamageFromSpeed, Precision);
    }

    [Theory]
    [InlineData("m_BaseAccuracyMultiplier", 1.5f)]
    [InlineData("m_HipFireAccuracyMultiplier", 1f)]
    [InlineData("m_MaxInaccuracyDegrees", 5f)]
    public void Worst_spread_stays_when_the_player_changed_any_accuracy_setting(string field, float value)
    {
        var saved = Version1Defaults();
        saved[field] = value;
        var loaded = Loaded with { MaxInaccuracyDegrees = saved["m_MaxInaccuracyDegrees"] };

        Assert.Equal(saved["m_MaxInaccuracyDegrees"], SettingsMigration.Migrate(saved, loaded)!.Value.MaxInaccuracyDegrees, Precision);
    }

    [Fact]
    public void A_1_1_0_file_missing_an_accuracy_setting_keeps_worst_spread()
    {
        var saved = Version1Defaults();
        saved.Remove("m_MovingAccuracyMultiplier");

        Assert.Equal(10f, SettingsMigration.Migrate(saved, Loaded)!.Value.MaxInaccuracyDegrees, Precision);
    }

    [Fact]
    public void A_1_1_0_file_without_long_range_damage_keeps_the_loaded_damage_from_speed()
    {
        var saved = Version1Defaults();
        saved.Remove("m_MinDamageFraction");

        Assert.Equal(1f, SettingsMigration.Migrate(saved, Loaded)!.Value.DamageFromSpeed, Precision);
    }
}
