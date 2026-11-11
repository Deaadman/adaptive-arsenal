using Vec3 = System.Numerics.Vector3;

namespace AdaptiveArsenal.Core;

/// <summary>The air a projectile flies through: how dense it is, the speed of sound in it, and the wind.</summary>
/// <remarks>
/// Cold air is denser, so it slows a projectile more, and sound is slower in it, so a bullet reaches the speed of
/// sound, where drag rises sharply, at a lower speed. The air is taken to be dry and at sea-level pressure, since the
/// game doesn't model altitude or humidity.
/// </remarks>
/// <param name="Density">The air's density, in kg/m³.</param>
/// <param name="SpeedOfSound">The speed of sound in it, in m/s.</param>
/// <param name="Wind">The wind's velocity, in m/s: the way the air moves.</param>
internal readonly record struct Atmosphere(float Density, float SpeedOfSound, Vec3 Wind)
{
    /// <summary>The temperature, in °C, of <see cref="Standard"/>: the one drag curves and zeroes are for.</summary>
    internal const float StandardTemperature = 15f;

    /// <summary>Air pressure at sea level, in pascals.</summary>
    private const float SeaLevelPressure = 101325f;

    /// <summary>The specific gas constant of dry air, in J/(kg·K).</summary>
    private const float DryAirGasConstant = 287.05f;

    /// <summary>The ratio of dry air's specific heats, which sets the speed of sound in it.</summary>
    private const float DryAirHeatCapacityRatio = 1.4f;

    /// <summary>0 °C in kelvin.</summary>
    private const float ZeroCelsius = 273.15f;

    /// <summary>
    /// The lowest temperature, in °C, that <see cref="FromTemperature"/> uses, far below anything the game reaches, so
    /// a broken reading can't make the air infinitely dense.
    /// </summary>
    private const float MinTemperature = -100f;

    /// <summary>Still air at 15 °C and sea level, which the G1 drag curve and zeroing assume.</summary>
    internal static Atmosphere Standard { get; } = FromTemperature(StandardTemperature, Vec3.Zero);

    /// <summary>Air at a temperature, with a wind.</summary>
    /// <param name="celsius">The air's temperature, in °C. Below −100 counts as −100.</param>
    /// <param name="wind">The wind's velocity, in m/s.</param>
    internal static Atmosphere FromTemperature(float celsius, Vec3 wind)
    {
        var kelvin = MathF.Max(celsius, MinTemperature) + ZeroCelsius;
        return new(
            SeaLevelPressure / (DryAirGasConstant * kelvin),
            MathF.Sqrt(DryAirHeatCapacityRatio * DryAirGasConstant * kelvin),
            wind);
    }

    /// <summary>How dense the air is compared with <see cref="Standard"/>, which scales drag.</summary>
    internal float DensityRatio => Density / Standard.Density;
}

/// <summary>
/// The weather a shot is fired in, read once for all its projectiles: the air they fly through, and the temperature
/// of the powder, taken to be the air's.
/// </summary>
/// <param name="Atmosphere">The air the projectiles fly through.</param>
/// <param name="AirTemperature">The air's temperature in °C, or <see langword="null"/> to leave the powder at 15 °C.</param>
internal readonly record struct ShotWeather(Atmosphere Atmosphere, float? AirTemperature)
{
    /// <summary>How many metres per second one mile per hour is, since the game gives wind speed in mph.</summary>
    internal const float MetresPerSecondPerMph = 0.44704f;

    /// <summary>No weather: still air at 15 °C, which a shot is fired in when weather doesn't affect it.</summary>
    internal static ShotWeather None { get; } = new(Atmosphere.Standard, null);

    /// <summary>The weather from the game's readings.</summary>
    /// <param name="celsius">The air's temperature, in °C.</param>
    /// <param name="windDirection">The game's wind direction, which needn't be of length 1. See <see cref="WindVelocity"/>.</param>
    /// <param name="windSpeedMph">The game's wind speed, in mph.</param>
    /// <param name="indoors">Whether the shot is fired indoors, where there's no wind.</param>
    internal static ShotWeather FromGame(float celsius, Vec3 windDirection, float windSpeedMph, bool indoors) =>
        new(Atmosphere.FromTemperature(celsius, indoors ? Vec3.Zero : WindVelocity(windDirection, windSpeedMph)), celsius);

    /// <summary>The wind's velocity, in m/s, from the game's wind direction and speed.</summary>
    /// <remarks>
    /// The game's direction is taken to be the way the wind blows towards, as with Unity's wind zones, which its
    /// particles use. That's not been checked in game: if bullets drift upwind, it's the way the wind blows from, and
    /// this is the one place to turn it round.
    /// </remarks>
    /// <param name="direction">The game's wind direction. A zero vector gives no wind.</param>
    /// <param name="speedMph">The game's wind speed, in mph. Below 0 counts as 0.</param>
    internal static Vec3 WindVelocity(Vec3 direction, float speedMph)
    {
        var length = direction.Length();
        if (length <= 0f || speedMph <= 0f)
            return Vec3.Zero;

        return direction / length * (speedMph * MetresPerSecondPerMph);
    }

    /// <summary>A muzzle velocity changed for the powder's temperature, if there is one.</summary>
    /// <param name="muzzleVelocity">The muzzle velocity at 15 °C, in m/s.</param>
    internal float MuzzleVelocity(float muzzleVelocity) =>
        AirTemperature is { } celsius ? Ballistics.MuzzleVelocityAtTemperature(muzzleVelocity, celsius) : muzzleVelocity;
}
