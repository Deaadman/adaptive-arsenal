using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class MovementTests
{
    private const int Precision = 4;

    [Fact]
    public void A_slide_starts_a_little_faster_than_the_sprint() =>
        Assert.Equal(6f * Movement.SlideStartBoost, Movement.SlideStartSpeed(6f), Precision);

    [Fact]
    public void A_slide_never_starts_faster_than_the_limit() =>
        Assert.Equal(Movement.MaxSlideSpeed, Movement.SlideStartSpeed(50f), Precision);

    [Theory]
    [InlineData(0f, 1f, 0f, 1f, 0f, 0f)]
    [InlineData(-0.70710678f, 0.70710678f, 0f, 1f, 0f, 1f)]
    [InlineData(0.70710678f, 0.70710678f, 0f, 1f, 0f, -1f)]
    [InlineData(0f, 0.70710678f, 0.70710678f, 1f, 0f, 0f)]
    [InlineData(1f, 0f, 0f, 1f, 0f, 0f)]
    public void Rise_is_the_slope_along_the_direction(float nx, float ny, float nz, float dx, float dz, float expected) =>
        Assert.Equal(expected, Movement.Rise(nx, ny, nz, dx, dz), Precision);

    [Fact]
    public void Friction_slows_a_slide_on_flat_ground() =>
        Assert.Equal(6f - Movement.Gravity * Movement.BaseSlideFriction * 0.1f, Movement.NextSlideSpeed(6f, rise: 0f, deltaTime: 0.1f), Precision);

    [Fact]
    public void A_slide_slows_faster_uphill_than_on_the_flat() =>
        Assert.True(Movement.NextSlideSpeed(6f, rise: 0.3f, deltaTime: 0.1f) < Movement.NextSlideSpeed(6f, rise: 0f, deltaTime: 0.1f));

    [Fact]
    public void A_steep_hill_speeds_a_slide_up_but_never_past_the_limit()
    {
        Assert.True(Movement.NextSlideSpeed(6f, rise: -1f, deltaTime: 0.1f) > 6f);
        Assert.Equal(Movement.MaxSlideSpeed, Movement.NextSlideSpeed(Movement.MaxSlideSpeed, rise: -1f, deltaTime: 1f), Precision);
    }

    [Fact]
    public void A_slide_never_goes_backwards() =>
        Assert.Equal(0f, Movement.NextSlideSpeed(0.5f, rise: 1f, deltaTime: 1f), Precision);

    [Fact]
    public void On_the_flat_a_sprint_slide_ends_after_a_few_metres()
    {
        var speed = Movement.SlideStartSpeed(6f);
        var distance = 0f;
        var seconds = 0f;
        const float frame = 1f / 60f;
        while (!Movement.SlideHasEnded(speed, seconds))
        {
            speed = Movement.NextSlideSpeed(speed, rise: 0f, frame);
            distance += speed * frame;
            seconds += frame;
        }

        Assert.InRange(distance, 3f, 10f);
        Assert.True(seconds < Movement.MaxSlideSeconds);
    }

    [Fact]
    public void A_longer_slide_distance_means_less_friction()
    {
        try
        {
            Movement.SlideDistanceMultiplier = 2f;
            Assert.Equal(Movement.BaseSlideFriction / 2f, Movement.SlideFriction, Precision);
        }
        finally
        {
            Movement.SlideDistanceMultiplier = 1f;
        }
    }

    [Theory]
    [InlineData(1f, 0f, true)]
    [InlineData(Movement.SlideEndSpeed, 0f, false)]
    [InlineData(5f, Movement.MaxSlideSeconds, true)]
    public void A_slide_ends_when_slow_or_long(float speed, float seconds, bool expected) =>
        Assert.Equal(expected, Movement.SlideHasEnded(speed, seconds));

    [Theory]
    [InlineData(0.1f, false)]
    [InlineData(Movement.MaxSlideAirborneSeconds, false)]
    [InlineData(0.3f, true)]
    public void A_slide_survives_small_bumps_but_not_leaving_the_ground(float airborneSeconds, bool expected) =>
        Assert.Equal(expected, Movement.SlideLeftGround(airborneSeconds));

    [Fact]
    public void The_ground_turning_a_slide_doesnt_slow_it() =>
        Assert.Equal(7f, Movement.SlideSpeedAfterMove(7f, movedAcross: 0.05f, deltaTime: 0.1f, hitSide: false), Precision);

    [Fact]
    public void Hitting_something_slows_a_slide_to_how_far_it_got() =>
        Assert.Equal(0.5f, Movement.SlideSpeedAfterMove(7f, movedAcross: 0.05f, deltaTime: 0.1f, hitSide: true), Precision);

    [Fact]
    public void A_glancing_hit_that_still_moved_the_full_distance_keeps_the_speed() =>
        Assert.Equal(7f, Movement.SlideSpeedAfterMove(7f, movedAcross: 1f, deltaTime: 0.1f, hitSide: true), Precision);

    [Theory]
    [InlineData(1f, 1.75f)]
    [InlineData(1.75f, 1f)]
    [InlineData(0f, 17.5f)]
    public void Crouched_sprinting_divides_out_the_games_sprint_multiplier(float gameSprintMultiplier, float expected) =>
        Assert.Equal(expected, Movement.CrouchSprintAccelerationMultiplier(gameSprintMultiplier), Precision);

    [Theory]
    [InlineData(1f, 0f, true)]
    [InlineData(0.5f, 0.5f, true)]
    [InlineData(0.4f, 0f, false)]
    [InlineData(1f, -0.6f, false)]
    public void Sprinting_needs_mostly_forward_input(float forward, float strafe, bool expected) =>
        Assert.Equal(expected, Movement.InputCanSprint(forward, strafe, minForward: 0.5f, maxStrafe: 0.5f));

    [Fact]
    public void Prone_is_half_the_crouched_height() =>
        Assert.Equal(0.6f, Movement.ProneHeight(crouchedHeight: 1.2f, radius: 0.2f), Precision);

    [Fact]
    public void A_prone_capsule_is_never_shorter_than_its_two_end_spheres() =>
        Assert.Equal(2f * 0.4f + 0.01f, Movement.ProneHeight(crouchedHeight: 1.2f, radius: 0.4f), Precision);

    [Fact]
    public void Prone_is_never_taller_than_crouched() =>
        Assert.Equal(0.5f, Movement.ProneHeight(crouchedHeight: 0.5f, radius: 0.4f), Precision);

    [Fact]
    public void The_prone_capsule_keeps_its_bottom_where_the_crouched_one_was()
    {
        const float crouchedHeight = 1.2f;
        const float crouchedCenterY = 0.6f;
        var proneHeight = Movement.ProneHeight(crouchedHeight, radius: 0.2f);
        var proneCenterY = Movement.ProneCenterY(crouchedCenterY, crouchedHeight, proneHeight);

        Assert.Equal(crouchedCenterY - crouchedHeight / 2f, proneCenterY - proneHeight / 2f, Precision);
    }

    [Theory]
    [InlineData(1.2f, 0.6f, 0.6f)]
    [InlineData(0.5f, 0.5f, 0f)]
    [InlineData(0.5f, 0.8f, 0f)]
    public void Getting_up_needs_the_difference_in_height(float crouchedHeight, float proneHeight, float expected) =>
        Assert.Equal(expected, Movement.HeadroomToCrouch(crouchedHeight, proneHeight), Precision);

    [Fact]
    public void The_prone_camera_is_lower_than_crouched() =>
        Assert.Equal(0.4f, Movement.ProneEyeHeight(crouchedEyeHeight: 1f), Precision);

    [Fact]
    public void Stamina_used_is_the_rate_times_the_frame_and_never_negative()
    {
        Assert.Equal(0.25f, Movement.StaminaUsed(usagePerSecond: 2.5f, deltaTime: 0.1f), Precision);
        Assert.Equal(0f, Movement.StaminaUsed(usagePerSecond: -1f, deltaTime: 0.1f), Precision);
    }
}
