using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class BallisticsTests
{
    private const int Precision = 4;

    [Theory]
    [InlineData(400f, 80f)]
    [InlineData(300f, 45f)]
    [InlineData(200f, 20f)]
    [InlineData(0f, 0f)]
    public void Damage_is_the_share_of_the_muzzle_energy_left(float impactSpeed, float expected) =>
        Assert.Equal(expected, Ballistics.DamageAtSpeed(impactSpeed, muzzleSpeed: 400f, muzzleDamage: 80f), Precision);

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void A_weapon_without_damage_uses_the_default(float muzzleDamage) =>
        Assert.Equal(Ballistics.DefaultMuzzleDamage, Ballistics.DamageAtSpeed(400f, 400f, muzzleDamage), Precision);

    [Theory]
    [InlineData(0f)]
    [InlineData(-10f)]
    public void A_bullet_with_no_muzzle_speed_does_full_damage(float muzzleSpeed) =>
        Assert.Equal(80f, Ballistics.DamageAtSpeed(100f, muzzleSpeed, muzzleDamage: 80f), Precision);

    [Fact]
    public void A_bullet_that_sped_up_does_no_more_than_full_damage() =>
        Assert.Equal(80f, Ballistics.DamageAtSpeed(500f, muzzleSpeed: 400f, muzzleDamage: 80f), Precision);

    [Fact]
    public void Damage_never_rises_as_the_bullet_slows()
    {
        var previous = float.MaxValue;
        for (var speed = 800f; speed >= 0f; speed -= 20f)
        {
            var damage = Ballistics.DamageAtSpeed(speed, muzzleSpeed: 800f, muzzleDamage: 100f);
            Assert.True(damage <= previous, $"damage rose at {speed} m/s");
            previous = damage;
        }
    }

    [Theory]
    [InlineData(1f, 20f)]
    [InlineData(0.5f, 50f)]
    [InlineData(0f, 80f)]
    public void Damage_from_speed_blends_towards_full_damage(float damageFromSpeed, float expected) =>
        WithDamageFromSpeed(damageFromSpeed, () =>
            Assert.Equal(expected, Ballistics.DamageAtSpeed(200f, muzzleSpeed: 400f, muzzleDamage: 80f), Precision));

    [Theory]
    [InlineData(0f, 80f)]
    [InlineData(250f, 44f)]
    [InlineData(500f, 8f)]
    [InlineData(1000f, 8f)]
    public void Linear_drag_damage_falls_with_distance_as_in_1_1_0(float distance, float expected) =>
        Assert.Equal(expected, Ballistics.DamageAtDistance(distance, muzzleDamage: 80f), Precision);

    [Fact]
    public void Damage_from_speed_of_0_turns_off_the_linear_drag_drop_off_too() =>
        WithDamageFromSpeed(0f, () => Assert.Equal(80f, Ballistics.DamageAtDistance(500f, muzzleDamage: 80f), Precision));

    [Fact]
    public void G1_damage_goes_by_speed()
    {
        var damage = Ballistics.DamageOnHit(DragModel.G1(0.4f), lowestSpeed: 200f, muzzleSpeed: 400f, distance: 0f, muzzleDamage: 80f);

        Assert.Equal(20f, damage, Precision);
    }

    [Fact]
    public void Linear_drag_damage_goes_by_distance()
    {
        var damage = Ballistics.DamageOnHit(DragModel.Linear(0.1f), lowestSpeed: 400f, muzzleSpeed: 400f, distance: 500f, muzzleDamage: 80f);

        Assert.Equal(8f, damage, Precision);
    }

    [Theory]
    [InlineData(0.1f, 1f)]
    [InlineData(1f, 0f)]
    [InlineData(0.55f, 0.5f)]
    [InlineData(0f, 1f)]
    public void Old_long_range_damage_carries_over_to_damage_from_speed(float longRangeDamage, float expected) =>
        Assert.Equal(expected, Ballistics.DamageFromSpeedForLongRangeDamage(longRangeDamage), Precision);

    /// <summary>Runs a check with <see cref="Ballistics.DamageFromSpeed"/> set, and puts it back afterwards.</summary>
    private static void WithDamageFromSpeed(float damageFromSpeed, Action check)
    {
        var before = Ballistics.DamageFromSpeed;
        try
        {
            Ballistics.DamageFromSpeed = damageFromSpeed;
            check();
        }
        finally
        {
            Ballistics.DamageFromSpeed = before;
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
    [InlineData("GEAR_Rifle", 740f)]
    [InlineData("GEAR_Rifle_Barbs", 740f)]
    [InlineData("GEAR_Rifle_Curators", 780f)]
    [InlineData("GEAR_Rifle_Traders", 820f)]
    [InlineData("GEAR_Rifle_Vaughns", 700f)]
    [InlineData("GEAR_Revolver", 270f)]
    [InlineData("GEAR_RevolverFancy", 330f)]
    [InlineData("GEAR_RevolverGreen", 270f)]
    [InlineData("GEAR_RevolverStubNosed", 240f)]
    public void Each_weapon_has_its_muzzle_velocity(string gearName, float expected) =>
        Assert.Equal(expected, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_Rifle_Traders(Clone)", 820f)]
    [InlineData("GEAR_RevolverStubNosed(Clone)", 240f)]
    public void Specific_weapons_match_before_the_plain_rifle_and_revolver(string gearName, float expected) =>
        Assert.Equal(expected, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_FlareGun")]
    [InlineData("GEAR_Bow")]
    [InlineData("")]
    public void An_unlisted_weapon_gets_no_muzzle_velocity(string gearName) =>
        Assert.Equal(0f, Ballistics.GetMuzzleVelocity(gearName));

    [Theory]
    [InlineData("GEAR_RifleAmmoSingle", 0.40f)]
    [InlineData("GEAR_RevolverAmmoSingle", 0.15f)]
    [InlineData("GEAR_RifleAmmoSingle(Clone)", 0.40f)]
    public void Each_ammo_has_its_ballistic_coefficient(string gearName, float expected) =>
        Assert.Equal(expected, Ballistics.GetBallisticCoefficient(gearName));

    [Theory]
    [InlineData("GEAR_FlareGunAmmoSingle")]
    [InlineData("")]
    public void Unlisted_ammo_gets_no_ballistic_coefficient(string gearName) =>
        Assert.Equal(0f, Ballistics.GetBallisticCoefficient(gearName));
}
