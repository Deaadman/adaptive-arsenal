using System.Numerics;

using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class FlightTests
{
    /// <summary>Unity's gravity, in m/s².</summary>
    private const float Gravity = 9.81f;

    /// <summary>Unity's default physics step, in seconds.</summary>
    private const float Step = 0.02f;

    private static readonly DragModel Rifle = DragModel.G1(0.40f);
    private static readonly DragModel Revolver = DragModel.G1(0.15f);

    [Fact]
    public void With_almost_no_drag_a_projectile_falls_as_gravity_says()
    {
        // A huge coefficient leaves G1 drag negligible, and the midpoint method is exact under constant gravity.
        FlightState state = new(Vector3.Zero, new Vector3(100f, 0f, 0f));
        for (var i = 0; i < 50; i++)
            state = Flight.Step(state, new Vector3(0f, -Gravity, 0f), DragModel.G1(1e9f), Step);

        // After 1 s: 100 m along, ½gt² down, and falling at g.
        Assert.Equal(100f, state.Position.X, 0.001f);
        Assert.Equal(-Gravity / 2f, state.Position.Y, 0.001f);
        Assert.Equal(-Gravity, state.Velocity.Y, 0.001f);
    }

    [Fact]
    public void Linear_drag_moves_a_projectile_as_a_rigidbody_does()
    {
        FlightState start = new(new Vector3(1f, 2f, 3f), new Vector3(400f, 50f, 0f));
        var state = Flight.Step(start, new Vector3(0f, -Gravity, 0f), DragModel.Linear(0.1f), Step);

        // Gravity first, then the velocity damped by 1 − drag × step, then the move.
        var velocity = (start.Velocity + new Vector3(0f, -Gravity * Step, 0f)) * (1f - 0.1f * Step);
        Assert.Equal(velocity, state.Velocity);
        Assert.Equal(start.Position + velocity * Step, state.Position);
    }

    [Fact]
    public void Linear_drag_loses_its_share_of_the_speed_each_step()
    {
        FlightState state = new(Vector3.Zero, new Vector3(400f, 0f, 0f));
        for (var i = 0; i < 50; i++)
            state = Flight.Step(state, Vector3.Zero, DragModel.Linear(0.1f), Step);

        Assert.Equal(400f * MathF.Pow(1f - 0.1f * Step, 50), state.Velocity.X, 0.01f);
    }

    [Theory]
    [InlineData(100f)]
    [InlineData(1000f)]
    public void Linear_drag_strong_enough_to_stop_a_projectile_in_one_step_stops_it(float drag)
    {
        var state = Flight.Step(new(Vector3.Zero, new Vector3(400f, 0f, 0f)), Vector3.Zero, DragModel.Linear(drag), Step);

        Assert.Equal(Vector3.Zero, state.Velocity);
    }

    [Theory]
    [InlineData(0.001f)]
    [InlineData(0.00001f)]
    public void G1_drag_never_turns_a_projectile_around(float ballisticCoefficient)
    {
        FlightState state = new(Vector3.Zero, new Vector3(400f, 0f, 0f));
        for (var i = 0; i < 20; i++)
        {
            var next = Flight.Step(state, Vector3.Zero, DragModel.G1(ballisticCoefficient), Step);
            Assert.True(next.Velocity.X >= 0f, $"reversed at step {i}");
            Assert.True(next.Velocity.X <= state.Velocity.X, $"sped up at step {i}");
            state = next;
        }
    }

    [Fact]
    public void A_rifle_bullet_keeps_about_three_fifths_of_its_speed_at_500_metres()
    {
        // Published tables for a .303 bullet with a coefficient of about 0.4 give about 57% at 500 m.
        var speed = SpeedAtDistance(740f, Rifle, 500f);

        Assert.InRange(speed / 740f, 0.52f, 0.63f);
    }

    [Fact]
    public void A_revolver_bullet_keeps_most_of_its_speed_at_50_metres()
    {
        var speed = SpeedAtDistance(270f, Revolver, 50f);

        Assert.InRange(speed / 270f, 0.94f, 0.99f);
    }

    [Theory]
    [InlineData(740f, 0.40f, 100f)]
    [InlineData(820f, 0.40f, 100f)]
    [InlineData(185f, 0.40f, 100f)]
    [InlineData(270f, 0.15f, 25f)]
    [InlineData(60f, 0.15f, 25f)]
    public void A_zeroed_shot_crosses_the_line_of_sight_at_the_zero_distance(float muzzleVelocity, float ballisticCoefficient,
        float zeroDistance)
    {
        var drag = DragModel.G1(ballisticCoefficient);
        var angle = Flight.ZeroAngle(muzzleVelocity, drag, Gravity, zeroDistance, Step);

        Assert.True(angle > 0f);
        Assert.Equal(0f, Flight.HeightAtDistance(angle, muzzleVelocity, drag, Gravity, zeroDistance, Step), 0.001f);
    }

    [Fact]
    public void A_rifle_zeroed_at_100_metres_is_raised_by_a_small_fraction_of_a_degree()
    {
        // A rifle bullet drops about 10 cm over 100 m, so the barrel points about that far above the target.
        var angle = Flight.ZeroAngle(740f, Rifle, Gravity, 100f, Step);

        Assert.InRange(angle, 0.04f, 0.08f);
    }

    [Fact]
    public void A_rifle_zeroed_at_100_metres_is_close_to_the_line_of_sight_out_to_150()
    {
        var angle = Flight.ZeroAngle(740f, Rifle, Gravity, 100f, Step);

        // Above the line on the way up, and below it after the zero, but by no more than about 10 cm either way.
        Assert.InRange(Flight.HeightAtDistance(angle, 740f, Rifle, Gravity, 50f, Step), 0.01f, 0.05f);
        Assert.InRange(Flight.HeightAtDistance(angle, 740f, Rifle, Gravity, 150f, Step), -0.12f, -0.05f);
    }

    [Fact]
    public void A_slower_shot_needs_a_higher_zero_angle() =>
        Assert.True(Flight.ZeroAngle(400f, Rifle, Gravity, 100f, Step) > Flight.ZeroAngle(800f, Rifle, Gravity, 100f, Step));

    [Theory]
    [InlineData(0f, Gravity, 100f, Step)]
    [InlineData(740f, 0f, 100f, Step)]
    [InlineData(740f, Gravity, 0f, Step)]
    [InlineData(740f, Gravity, 100f, 0f)]
    public void Nothing_to_zero_gives_no_angle(float muzzleVelocity, float gravity, float zeroDistance, float deltaTime) =>
        Assert.Equal(0f, Flight.ZeroAngle(muzzleVelocity, Rifle, gravity, zeroDistance, deltaTime));

    [Theory]
    [InlineData(0f, 400f)]
    [InlineData(0.25f, 375f)]
    [InlineData(1f, 300f)]
    [InlineData(-1f, 400f)]
    [InlineData(2f, 300f)]
    public void The_velocity_part_way_through_a_step_is_between_its_ends(float fraction, float expected) =>
        Assert.Equal(new Vector3(expected, 0f, 0f), Flight.VelocityPartWay(new Vector3(400f, 0f, 0f), new Vector3(300f, 0f, 0f), fraction));

    [Fact]
    public void A_shot_whose_furthest_reach_is_below_45_degrees_still_gets_zeroed()
    {
        // With this much drag a 45° shot never gets 40 m along, but a flatter one does.
        var drag = DragModel.Linear(2f);
        Assert.Equal(float.NegativeInfinity, Flight.HeightAtDistance(45f, 100f, drag, Gravity, 40f, Step));

        var angle = Flight.ZeroAngle(100f, drag, Gravity, 40f, Step);

        Assert.InRange(angle, 0.01f, 30f);
        Assert.Equal(0f, Flight.HeightAtDistance(angle, 100f, drag, Gravity, 40f, Step), 0.001f);
    }

    [Fact]
    public void The_same_shot_gets_the_same_zero_angle_every_time() =>
        Assert.Equal(Flight.ZeroAngle(740f, Rifle, Gravity, 100f, Step), Flight.ZeroAngle(740f, Rifle, Gravity, 100f, Step));

    [Fact]
    public void A_shot_too_slow_to_reach_the_zero_distance_gives_no_angle() =>
        Assert.Equal(0f, Flight.ZeroAngle(5f, Rifle, Gravity, 100f, Step));

    [Fact]
    public void A_shot_that_never_gets_there_has_no_height() =>
        Assert.Equal(float.NegativeInfinity, Flight.HeightAtDistance(0f, 5f, Rifle, Gravity, 100f, Step));

    /// <summary>Flies a level shot until it has travelled <paramref name="distance"/>, and returns its speed then.</summary>
    private static float SpeedAtDistance(float muzzleVelocity, DragModel drag, float distance)
    {
        FlightState state = new(Vector3.Zero, new Vector3(muzzleVelocity, 0f, 0f));
        while (state.Position.X < distance)
            state = Flight.Step(state, new Vector3(0f, -Gravity, 0f), drag, Step);

        return state.Velocity.Length();
    }
}
