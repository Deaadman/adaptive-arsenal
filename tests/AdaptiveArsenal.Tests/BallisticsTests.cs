using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class BallisticsTests
{
    private const int Precision = 4;

    [Theory]
    [InlineData(0f, 80f)]
    [InlineData(250f, 44f)]
    [InlineData(500f, 8f)]
    [InlineData(1000f, 8f)]
    public void Damage_falls_steadily_to_a_tenth_of_the_weapons_at_500_metres(float distance, float expected) =>
        Assert.Equal(expected, Ballistics.DamageAtDistance(distance, muzzleDamage: 80f), Precision);

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void A_weapon_without_damage_uses_the_default(float muzzleDamage) =>
        Assert.Equal(Ballistics.DefaultMuzzleDamage, Ballistics.DamageAtDistance(0f, muzzleDamage), Precision);

    [Fact]
    public void Damage_never_rises_with_distance()
    {
        var previous = float.MaxValue;
        for (var distance = 0f; distance <= 600f; distance += 10f)
        {
            var damage = Ballistics.DamageAtDistance(distance, muzzleDamage: 100f);
            Assert.True(damage <= previous, $"damage rose at {distance} m");
            previous = damage;
        }
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(0.125f, 1f)]
    [InlineData(0.6f, 0.25f)]
    [InlineData(0.9f, 0.81f)]
    public void Spread_lands_inside_the_circle(float random1, float random2)
    {
        var (pitch, yaw) = Ballistics.SpreadOffset(maxSpread: 10f, random1, random2);

        Assert.True(MathF.Sqrt(pitch * pitch + yaw * yaw) <= 10f + 1e-4f);
    }

    [Fact]
    public void Spread_at_the_edge_is_the_same_distance_in_every_direction()
    {
        for (var random1 = 0f; random1 < 1f; random1 += 0.05f)
        {
            var (pitch, yaw) = Ballistics.SpreadOffset(maxSpread: 10f, random1, random2: 1f);
            Assert.Equal(10f, MathF.Sqrt(pitch * pitch + yaw * yaw), Precision);
        }
    }

    [Fact]
    public void Spread_reaches_half_the_radius_at_a_quarter_of_the_second_number()
    {
        // Half the radius encloses a quarter of the circle's area, so a quarter of shots land within it.
        var (pitch, yaw) = Ballistics.SpreadOffset(maxSpread: 10f, random1: 0.3f, random2: 0.25f);

        Assert.Equal(5f, MathF.Sqrt(pitch * pitch + yaw * yaw), Precision);
    }

    [Fact]
    public void No_spread_lands_exactly_on_aim() =>
        Assert.Equal((0f, 0f), Ballistics.SpreadOffset(maxSpread: 0f, random1: 0.7f, random2: 0.9f));

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
    [InlineData("GEAR_Rifle", 400f)]
    [InlineData("GEAR_Rifle_Barbs", 400f)]
    [InlineData("GEAR_Rifle_Curators", 500f)]
    [InlineData("GEAR_Rifle_Traders", 550f)]
    [InlineData("GEAR_Rifle_Vaughns", 350f)]
    [InlineData("GEAR_Revolver", 200f)]
    [InlineData("GEAR_RevolverFancy", 300f)]
    [InlineData("GEAR_RevolverGreen", 200f)]
    [InlineData("GEAR_RevolverStubNosed", 150f)]
    public void Each_weapon_has_its_muzzle_velocity(string gearName, float expected) =>
        Assert.Equal(expected, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_Rifle_Traders(Clone)", 550f)]
    [InlineData("GEAR_RevolverStubNosed(Clone)", 150f)]
    public void Specific_weapons_match_before_the_plain_rifle_and_revolver(string gearName, float expected) =>
        Assert.Equal(expected, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_FlareGun")]
    [InlineData("GEAR_Bow")]
    [InlineData("")]
    public void An_unlisted_weapon_gets_no_muzzle_velocity(string gearName) =>
        Assert.Equal(0f, Ballistics.GetMuzzleVelocity(gearName));
}
