namespace AdaptiveArsenal.Core;

/// <summary>
/// Updates settings saved by an older version of the mod, so players keep what they chose and get new defaults for
/// what they never changed.
/// </summary>
/// <remarks>
/// <para>
/// ModSettings saves only the fields the settings still have, so a file's version is worked out from which fields
/// it has: each version adds a check to <see cref="VersionOf"/> and a step to <see cref="Migrate"/>, which runs every
/// step from the file's version up to <see cref="CurrentVersion"/> in order.
/// </para>
/// <para>
/// Settings are passed as their numbers by field name, as read from the file before ModSettings loads it, so this
/// needs neither the file nor ModSettings and can be tested.
/// </para>
/// </remarks>
internal static class SettingsMigration
{
    /// <summary>The settings version this build saves: 2, from 1.2.0.</summary>
    internal const int CurrentVersion = 2;

    /// <summary>The field added in version 2, which marks a file saved by it or later.</summary>
    internal const string DamageFromSpeedField = "m_DamageFromSpeed";

    /// <summary>The field version 1 (1.1.0) saved its Long-range damage setting in.</summary>
    internal const string LongRangeDamageField = "m_MinDamageFraction";

    /// <summary>The field Worst spread is saved in.</summary>
    internal const string MaxInaccuracyDegreesField = "m_MaxInaccuracyDegrees";

    /// <summary>
    /// Version 1's accuracy settings and their defaults. Only a player who left all of them alone is taken to have
    /// never changed Worst spread, since a value of 10 on its own could have been chosen.
    /// </summary>
    private static readonly (string Field, float Default)[] Version1AccuracyDefaults =
    [
        ("m_BaseAccuracyMultiplier", 1.2f),
        ("m_HipFireAccuracyMultiplier", 0.7f),
        ("m_StandingAccuracyMultiplier", 0.8f),
        ("m_MovingAccuracyMultiplier", 0.9f),
        (MaxInaccuracyDegreesField, 10f)
    ];

    /// <summary>Works out which version of the settings saved a file, from the fields it has.</summary>
    /// <param name="saved">The file's numbers, by field name.</param>
    /// <returns><see cref="CurrentVersion"/> for a file with every current field, or the version that saved it.</returns>
    internal static int VersionOf(IReadOnlyDictionary<string, float> saved) => saved.ContainsKey(DamageFromSpeedField) ? 2 : 1;

    /// <summary>
    /// Works out the settings a file saved by an older version should have now, starting from the values ModSettings
    /// loaded from it.
    /// </summary>
    /// <param name="saved">
    /// The file's numbers by field name, as read before ModSettings loaded it, or <see langword="null"/> or empty if
    /// there was no file to read.
    /// </param>
    /// <param name="loaded">The values ModSettings loaded, which are the defaults for fields the file didn't have.</param>
    /// <returns>The updated values, or <see langword="null"/> if the file is current or there was none.</returns>
    internal static MigratedSettings? Migrate(IReadOnlyDictionary<string, float>? saved, MigratedSettings loaded)
    {
        if (saved is null || saved.Count == 0 || VersionOf(saved) >= CurrentVersion)
            return null;

        return FromVersion1(saved, loaded);
    }

    /// <summary>
    /// Updates version 1 (1.1.0) to 2: carries Long-range damage over to Damage from speed, and moves Worst spread to
    /// its new default if the player never changed the accuracy settings.
    /// </summary>
    /// <param name="saved">The file's numbers, by field name.</param>
    /// <param name="loaded">The values ModSettings loaded.</param>
    private static MigratedSettings FromVersion1(IReadOnlyDictionary<string, float> saved, MigratedSettings loaded)
    {
        var updated = loaded;

        if (saved.TryGetValue(LongRangeDamageField, out var longRangeDamage))
            updated = updated with { DamageFromSpeed = Ballistics.DamageFromSpeedForLongRangeDamage(longRangeDamage) };

        if (Version1AccuracyDefaults.All(setting => saved.TryGetValue(setting.Field, out var value) && value == setting.Default))
            updated = updated with { MaxInaccuracyDegrees = Ballistics.DefaultMaxInaccuracyDegrees };

        return updated;
    }
}

/// <summary>The settings <see cref="SettingsMigration"/> can change.</summary>
/// <param name="DamageFromSpeed">Damage from speed.</param>
/// <param name="MaxInaccuracyDegrees">Worst spread, in degrees.</param>
internal readonly record struct MigratedSettings(float DamageFromSpeed, float MaxInaccuracyDegrees);
