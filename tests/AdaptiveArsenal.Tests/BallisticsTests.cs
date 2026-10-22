using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class BallisticsTests
{
    private const int Precision = 4;

    [Theory]
    [InlineData(0f, 100f)]
    [InlineData(250f, 55f)]
    [InlineData(500f, 10f)]
    [InlineData(1000f, 10f)]
    public void Damage_falls_steadily_to_a_tenth_at_500_metres(float distance, float expected) =>
        Assert.Equal(expected, Ballistics.DamageAtDistance(distance), Precision);

    [Fact]
    public void Damage_never_rises_with_distance()
    {
        var previous = float.MaxValue;
        for (var distance = 0f; distance <= 600f; distance += 10f)
        {
            var damage = Ballistics.DamageAtDistance(distance);
            Assert.True(damage <= previous, $"damage rose at {distance} m");
            previous = damage;
        }
    }

    [Fact]
    public void Aiming_while_crouched_and_still_only_gets_the_base_multiplier() =>
        Assert.Equal(1.2f, Ballistics.AccuracyMultiplier(isHipFire: false, isStanding: false, isMoving: false), Precision);

    [Fact]
    public void Hip_fire_standing_and_moving_each_lower_accuracy()
    {
        var best = Ballistics.AccuracyMultiplier(false, false, false);

        Assert.Equal(best * 0.7f, Ballistics.AccuracyMultiplier(true, false, false), Precision);
        Assert.Equal(best * 0.8f, Ballistics.AccuracyMultiplier(false, true, false), Precision);
        Assert.Equal(best * 0.9f, Ballistics.AccuracyMultiplier(false, false, true), Precision);
        Assert.Equal(1.2f * 0.7f * 0.8f * 0.9f, Ballistics.AccuracyMultiplier(true, true, true), Precision);
    }

    [Theory]
    [InlineData(0, 40f)]
    [InlineData(1, 50f)]
    [InlineData(2, 60f)]
    [InlineData(3, 80f)]
    [InlineData(4, 100f)]
    public void Revolver_accuracy_rises_with_each_skill_tier(int tier, float expected) =>
        Assert.Equal(expected, Ballistics.RevolverSkillAccuracy(tier));

    [Theory]
    [InlineData(-50f, 10f)]
    [InlineData(0f, 10f)]
    [InlineData(50f, 5f)]
    [InlineData(100f, 0f)]
    [InlineData(150f, 0f)]
    public void Spread_shrinks_from_10_degrees_to_none_at_full_accuracy(float accuracy, float expected) =>
        Assert.Equal(expected, Ballistics.MaxSpreadDegrees(accuracy), Precision);

    [Theory]
    [InlineData("GEAR_Rifle", 800)]
    [InlineData("GEAR_Rifle_Barbs", 800)]
    [InlineData("GEAR_Rifle_Curators", 1000)]
    [InlineData("GEAR_Rifle_Traders", 1100)]
    [InlineData("GEAR_Rifle_Vaughns", 700)]
    [InlineData("GEAR_Revolver", 400)]
    [InlineData("GEAR_RevolverFancy", 600)]
    [InlineData("GEAR_RevolverGreen", 400)]
    [InlineData("GEAR_RevolverStubNosed", 300)]
    public void Each_weapon_has_its_muzzle_velocity(string gearName, int expected) =>
        Assert.Equal(expected, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_Rifle_Traders(Clone)", 1100)]
    [InlineData("GEAR_RevolverStubNosed(Clone)", 300)]
    public void Specific_weapons_match_before_the_plain_rifle_and_revolver(string gearName, int expected) =>
        Assert.Equal(expected, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_FlareGun")]
    [InlineData("GEAR_Bow")]
    [InlineData("")]
    public void An_unlisted_weapon_gets_no_muzzle_velocity(string gearName) =>
        Assert.Equal(0, Ballistics.GetMuzzleVelocity(gearName));
}
