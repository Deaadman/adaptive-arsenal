using System.Numerics;

using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class AtmosphereTests
{
    private const float Gravity = 9.81f;
    private const float Step = 0.02f;
    private static readonly DragModel Rifle = DragModel.G1(0.40f);

    [Fact]
    public void Standard_air_is_the_sea_level_air_at_15_degrees()
    {
        Assert.Equal(1.225f, Atmosphere.Standard.Density, 0.001f);
        Assert.Equal(340.3f, Atmosphere.Standard.SpeedOfSound, 0.2f);
        Assert.Equal(Vector3.Zero, Atmosphere.Standard.Wind);
        Assert.Equal(1f, Atmosphere.Standard.DensityRatio);
    }

    [Fact]
    public void Air_at_minus_30_is_about_a_fifth_denser()
    {
        var cold = Atmosphere.FromTemperature(-30f, Vector3.Zero);

        Assert.Equal(288.15f / 243.15f, cold.DensityRatio, 0.001f);
        Assert.True(cold.SpeedOfSound < Atmosphere.Standard.SpeedOfSound);
    }

    [Fact]
    public void A_broken_temperature_reading_is_held_at_minus_100() =>
        Assert.Equal(Atmosphere.FromTemperature(-100f, Vector3.Zero), Atmosphere.FromTemperature(-500f, Vector3.Zero));

    [Theory]
    [InlineData(15f, 740f)]
    [InlineData(-30f, 716.69f)]
    [InlineData(35f, 750.36f)]
    public void Cold_powder_lowers_the_muzzle_velocity(float celsius, float expected) =>
        Assert.Equal(expected, Ballistics.MuzzleVelocityAtTemperature(740f, celsius), 0.01f);

    [Fact]
    public void Powder_never_loses_more_than_half_its_speed() =>
        Assert.Equal(370f, Ballistics.MuzzleVelocityAtTemperature(740f, -10000f), 0.01f);

    [Fact]
    public void A_crosswind_pushes_a_rifle_bullet_about_half_a_metre_at_300_metres()
    {
        var air = Atmosphere.FromTemperature(Atmosphere.StandardTemperature, new Vector3(0f, 0f, 10f));
        var state = FlyTo(300f, 740f, Rifle, air);

        Assert.InRange(state.Position.Z, 0.35f, 0.75f);
    }

    [Fact]
    public void A_headwind_slows_a_bullet_more()
    {
        var still = FlyTo(300f, 740f, Rifle, Atmosphere.Standard);
        var headwind = FlyTo(300f, 740f, Rifle, Atmosphere.FromTemperature(Atmosphere.StandardTemperature, new Vector3(-10f, 0f, 0f)));

        Assert.True(headwind.Velocity.X < still.Velocity.X);
    }

    [Fact]
    public void Cold_air_slows_a_bullet_more()
    {
        var mild = FlyTo(500f, 740f, Rifle, Atmosphere.Standard);
        var cold = FlyTo(500f, 740f, Rifle, Atmosphere.FromTemperature(-30f, Vector3.Zero));

        Assert.True(cold.Velocity.X < mild.Velocity.X - 10f);
    }

    [Fact]
    public void The_wind_carries_a_linear_drag_projectile_too()
    {
        var air = Atmosphere.FromTemperature(Atmosphere.StandardTemperature, new Vector3(0f, 0f, 10f));
        var state = FlyTo(100f, 300f, DragModel.Linear(0.5f), air);

        Assert.True(state.Position.Z > 0.05f);
    }

    [Fact]
    public void Linear_drag_is_damped_towards_the_wind_not_to_a_stop()
    {
        var air = Atmosphere.FromTemperature(Atmosphere.StandardTemperature, new Vector3(5f, 0f, 0f));
        var state = Flight.Step(new(Vector3.Zero, new Vector3(100f, 0f, 0f)), Vector3.Zero, DragModel.Linear(1000f), air, Step);

        Assert.Equal(new Vector3(5f, 0f, 0f), state.Velocity);
    }

    [Fact]
    public void No_weather_is_still_air_at_15_degrees_with_the_powder_unchanged()
    {
        Assert.Equal(Atmosphere.Standard, ShotWeather.None.Atmosphere);
        Assert.Equal(740f, ShotWeather.None.MuzzleVelocity(740f));
    }

    [Fact]
    public void The_games_wind_is_converted_from_mph_along_its_direction() =>
        Assert.Equal(new Vector3(4.4704f, 0f, 0f), ShotWeather.WindVelocity(new Vector3(2f, 0f, 0f), 10f));

    [Theory]
    [InlineData(0f, 0f, 0f, 10f)]
    [InlineData(1f, 0f, 0f, 0f)]
    [InlineData(1f, 0f, 0f, -5f)]
    public void No_direction_or_speed_gives_no_wind(float x, float y, float z, float mph) =>
        Assert.Equal(Vector3.Zero, ShotWeather.WindVelocity(new Vector3(x, y, z), mph));

    [Fact]
    public void Indoors_there_is_no_wind()
    {
        var weather = ShotWeather.FromGame(-10f, new Vector3(1f, 0f, 0f), 30f, indoors: true);

        Assert.Equal(Vector3.Zero, weather.Atmosphere.Wind);
        Assert.Equal(-10f, weather.AirTemperature);
    }

    [Fact]
    public void Outdoors_the_weather_has_the_wind_and_the_cold()
    {
        var weather = ShotWeather.FromGame(-30f, new Vector3(0f, 0f, 1f), 10f, indoors: false);

        Assert.Equal(new Vector3(0f, 0f, 4.4704f), weather.Atmosphere.Wind);
        Assert.Equal(Atmosphere.FromTemperature(-30f, Vector3.Zero).Density, weather.Atmosphere.Density);
        Assert.Equal(716.69f, weather.MuzzleVelocity(740f), 0.01f);
    }

    /// <summary>Flies a level shot until it has travelled <paramref name="distance"/> along X.</summary>
    private static FlightState FlyTo(float distance, float muzzleVelocity, DragModel drag, Atmosphere air)
    {
        FlightState state = new(Vector3.Zero, new Vector3(muzzleVelocity, 0f, 0f));
        while (state.Position.X < distance)
            state = Flight.Step(state, new Vector3(0f, -Gravity, 0f), drag, air, Step);

        return state;
    }
}
