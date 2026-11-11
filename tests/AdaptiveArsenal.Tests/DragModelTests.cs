using System.Numerics;

using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class DragModelTests
{
    private const int Precision = 4;

    [Theory]
    [InlineData(0f, 0.2629f)]
    [InlineData(1f, 0.4805f)]
    [InlineData(2f, 0.5934f)]
    [InlineData(5f, 0.4988f)]
    public void The_G1_curve_has_the_standard_values_at_its_points(float mach, float expected) =>
        Assert.Equal(expected, DragModel.G1DragCoefficient(mach), Precision);

    [Fact]
    public void The_G1_curve_is_interpolated_between_its_points() =>
        Assert.Equal((0.2032f + 0.2020f) / 2f, DragModel.G1DragCoefficient(0.525f), Precision);

    [Theory]
    [InlineData(-1f, 0.2629f)]
    [InlineData(8f, 0.4988f)]
    public void The_G1_curve_holds_its_ends_outside_them(float mach, float expected) =>
        Assert.Equal(expected, DragModel.G1DragCoefficient(mach), Precision);

    [Fact]
    public void Drag_rises_sharply_through_the_speed_of_sound() =>
        Assert.True(DragModel.G1DragCoefficient(1.2f) > 2f * DragModel.G1DragCoefficient(0.7f));

    [Fact]
    public void A_rifle_bullet_at_the_muzzle_slows_by_about_535_metres_per_second_squared()
    {
        // ρv²·Cd·π/(8·BC), with Cd 0.5715 at Mach 2.175 and the coefficient in kg/m².
        var acceleration = DragModel.G1(0.40f).Acceleration(new Vector3(740f, 0f, 0f), Atmosphere.Standard);

        Assert.Equal(-535.3f, acceleration.X, 0.5f);
        Assert.Equal(0f, acceleration.Y);
        Assert.Equal(0f, acceleration.Z);
    }

    [Fact]
    public void G1_drag_is_against_the_velocity_and_grows_faster_than_the_speed()
    {
        var drag = DragModel.G1(0.40f);
        var slow = drag.Acceleration(new Vector3(0f, 0f, 200f), Atmosphere.Standard);
        var fast = drag.Acceleration(new Vector3(0f, 0f, 400f), Atmosphere.Standard);

        Assert.True(slow.Z < 0f && fast.Z < 0f);
        Assert.True(fast.Z < 2f * slow.Z);
    }

    [Fact]
    public void A_higher_ballistic_coefficient_slows_less()
    {
        Vector3 velocity = new(500f, 0f, 0f);

        Assert.True(DragModel.G1(0.5f).Acceleration(velocity, Atmosphere.Standard).X > DragModel.G1(0.25f).Acceleration(velocity, Atmosphere.Standard).X);
    }

    [Fact]
    public void Linear_drag_gives_no_acceleration_since_the_step_applies_it() =>
        Assert.Equal(Vector3.Zero, DragModel.Linear(0.1f).Acceleration(new Vector3(300f, -40f, 10f), Atmosphere.Standard));

    [Fact]
    public void Nothing_slows_a_projectile_that_isnt_moving() =>
        Assert.Equal(Vector3.Zero, DragModel.G1(0.40f).Acceleration(Vector3.Zero, Atmosphere.Standard));

    [Fact]
    public void Denser_air_drags_harder_in_proportion()
    {
        Vector3 velocity = new(200f, 0f, 0f);
        Atmosphere dense = new(Atmosphere.Standard.Density * 1.2f, Atmosphere.Standard.SpeedOfSound, Vector3.Zero);

        Assert.Equal(1.2f * DragModel.G1(0.40f).Acceleration(velocity, Atmosphere.Standard).X,
            DragModel.G1(0.40f).Acceleration(velocity, dense).X, 0.01f);
    }

    [Fact]
    public void A_lower_speed_of_sound_reaches_the_sharp_rise_in_drag_sooner()
    {
        // 300 m/s is below the speed of sound at 15 °C but above it in much colder air.
        Vector3 velocity = new(300f, 0f, 0f);
        Atmosphere slowSound = new(Atmosphere.Standard.Density, 290f, Vector3.Zero);

        Assert.True(DragModel.G1(0.40f).Acceleration(velocity, slowSound).X < 1.5f * DragModel.G1(0.40f).Acceleration(velocity, Atmosphere.Standard).X);
    }

    [Fact]
    public void Negative_linear_drag_counts_as_none() =>
        Assert.Equal(0f, DragModel.Linear(-1f).LinearDrag);

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.4f)]
    public void A_ballistic_coefficient_of_0_or_less_gives_no_drag(float ballisticCoefficient)
    {
        var drag = DragModel.G1(ballisticCoefficient);

        Assert.False(drag.IsG1);
        Assert.Equal(0f, drag.LinearDrag);
    }

    [Fact]
    public void Each_kind_sets_only_its_own_value()
    {
        Assert.Equal(0f, DragModel.G1(0.40f).LinearDrag);
        Assert.Equal(0f, DragModel.Linear(0.1f).BallisticCoefficient);
    }
}
