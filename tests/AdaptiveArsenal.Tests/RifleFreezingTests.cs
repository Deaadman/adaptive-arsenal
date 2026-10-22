using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class RifleFreezingTests
{
    private const int Precision = 3;

    /// <summary>Puts back the tunable numbers, which are static, in case a test changed them.</summary>
    public RifleFreezingTests()
    {
        RifleFreezing.HoursToFreeze = 6f;
        RifleFreezing.HoursToThaw = 2f;
        RifleFreezing.MaxJamChance = 25f;
    }

    [Fact]
    public void A_rifle_freezes_solid_in_the_set_hours_at_full_cold() =>
        Assert.Equal(100f, RifleFreezing.Update(0f, 6f, -RifleFreezing.FullColdBelowFreezing), Precision);

    [Fact]
    public void Colder_than_full_cold_freezes_no_faster() =>
        Assert.Equal(RifleFreezing.Update(0f, 1f, -20f), RifleFreezing.Update(0f, 1f, -40f), Precision);

    [Fact]
    public void A_rifle_freezes_more_slowly_when_its_less_cold()
    {
        // Half as far below freezing, half as fast: 100 / 6 hours * 0.5.
        Assert.Equal(100f / 6f * 0.5f, RifleFreezing.Update(0f, 1f, -10f), Precision);
    }

    [Fact]
    public void A_rifle_thaws_in_the_set_hours_at_the_thaw_temperature() =>
        Assert.Equal(0f, RifleFreezing.Update(100f, 2f, RifleFreezing.ThawWarmthAboveFreezing), Precision);

    [Fact]
    public void A_rifle_still_thaws_just_above_freezing()
    {
        var thawed = 100f - RifleFreezing.Update(100f, 1f, 0f);

        Assert.Equal(100f / 2f * RifleFreezing.MinThawSpeed, thawed, Precision);
    }

    [Fact]
    public void A_fire_thaws_faster_but_only_up_to_the_limit()
    {
        var byAFire = 100f - RifleFreezing.Update(100f, 0.1f, 50f);
        var inAFurnace = 100f - RifleFreezing.Update(100f, 0.1f, 500f);

        Assert.Equal(100f / 2f * RifleFreezing.MaxThawSpeed * 0.1f, byAFire, Precision);
        Assert.Equal(byAFire, inAFurnace, Precision);
    }

    [Theory]
    [InlineData(-30f)]
    [InlineData(20f)]
    public void How_frozen_a_rifle_is_stays_between_0_and_100(float temperature)
    {
        var frozen = RifleFreezing.Update(50f, 1000f, temperature);

        Assert.InRange(frozen, 0f, 100f);
    }

    [Fact]
    public void No_time_passing_changes_nothing() =>
        Assert.Equal(40f, RifleFreezing.Update(40f, 0f, -30f));

    [Fact]
    public void Zero_hours_to_freeze_or_thaw_is_instant()
    {
        RifleFreezing.HoursToFreeze = 0f;
        RifleFreezing.HoursToThaw = 0f;

        Assert.Equal(100f, RifleFreezing.Update(0f, 0.01f, -1f));
        Assert.Equal(0f, RifleFreezing.Update(100f, 0.01f, 1f));
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(50f, 12.5f)]
    [InlineData(100f, 25f)]
    [InlineData(150f, 25f)]
    public void The_jam_chance_grows_with_how_frozen_the_rifle_is(float percentFrozen, float expected) =>
        Assert.Equal(expected, RifleFreezing.JamChance(percentFrozen), Precision);

    [Theory]
    [InlineData(0.8f, 0f)]
    [InlineData(1f, 0f)]
    [InlineData(0f, 20f)]
    [InlineData(0.25f, 12.5f)]
    public void The_games_jam_chance_rises_as_condition_falls_below_the_threshold(float condition, float expected) =>
        Assert.Equal(expected, RifleFreezing.GameJamChance(condition, threshold: 0.5f, minimumChance: 5f, maximumChance: 20f), Precision);

    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(0f, 30f, 30f)]
    [InlineData(50f, 50f, 75f)]
    [InlineData(100f, 10f, 100f)]
    public void Two_chances_combine_as_either_happening(float a, float b, float expected) =>
        Assert.Equal(expected, RifleFreezing.CombineChances(a, b), Precision);
}
