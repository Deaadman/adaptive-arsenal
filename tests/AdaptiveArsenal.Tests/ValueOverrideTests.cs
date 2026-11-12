using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class ValueOverrideTests
{
    [Fact]
    public void Restoring_gives_back_the_value_before_the_override()
    {
        var value = new ValueOverride<float>();
        var current = value.Set(current: 10f, value: 4f);

        Assert.True(value.Restore(current, out var original));
        Assert.Equal(10f, original);
        Assert.False(value.IsActive);
    }

    [Fact]
    public void Setting_again_keeps_the_first_original()
    {
        var value = new ValueOverride<float>();
        var current = value.Set(current: 10f, value: 4f);
        current = value.Set(current, current * 0.5f);

        Assert.True(value.Restore(current, out var original));
        Assert.Equal(10f, original);
    }

    [Fact]
    public void A_value_the_game_changed_since_isnt_put_back()
    {
        var value = new ValueOverride<float>();
        value.Set(current: 10f, value: 4f);

        Assert.False(value.Restore(current: 7f, out _));
        Assert.False(value.IsActive);
    }

    [Fact]
    public void A_value_the_game_changed_becomes_the_new_original()
    {
        var value = new ValueOverride<float>();
        value.Set(current: 10f, value: 4f);
        Assert.False(value.IsCurrent(7f));

        var current = value.Set(current: 7f, value: 3f);

        Assert.True(value.Restore(current, out var original));
        Assert.Equal(7f, original);
    }

    [Fact]
    public void Tuples_compare_every_part()
    {
        var value = new ValueOverride<(float Height, float Center)>();
        value.Set(current: (1.2f, 0.6f), value: (0.6f, 0.3f));

        Assert.True(value.IsCurrent((0.6f, 0.3f)));
        Assert.False(value.IsCurrent((0.6f, 0.4f)));
    }

    [Fact]
    public void An_unused_override_restores_nothing()
    {
        var value = new ValueOverride<float>();

        Assert.False(value.IsCurrent(0f));
        Assert.False(value.Restore(0f, out _));
    }
}
