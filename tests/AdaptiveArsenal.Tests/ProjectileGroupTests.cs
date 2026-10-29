using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class ProjectileGroupTests
{
    [Fact]
    public void Calls_back_once_when_the_last_projectile_is_done()
    {
        var calls = 0;
        ProjectileGroup group = new(3, _ => calls++);

        group.ProjectileFinished(false);
        group.ProjectileFinished(true);
        Assert.Equal(0, calls);

        group.ProjectileFinished(false);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Says_it_hit_when_any_projectile_hit()
    {
        bool? result = null;
        ProjectileGroup group = new(3, hit => result = hit);

        group.ProjectileFinished(false);
        group.ProjectileFinished(true);
        group.ProjectileFinished(false);

        Assert.True(result);
    }

    [Fact]
    public void Says_it_missed_when_no_projectile_hit()
    {
        bool? result = null;
        ProjectileGroup group = new(2, hit => result = hit);

        group.ProjectileFinished(false);
        group.ProjectileFinished(false);

        Assert.False(result);
    }

    [Fact]
    public void Ignores_projectiles_finishing_after_the_last()
    {
        var calls = 0;
        ProjectileGroup group = new(1, _ => calls++);

        group.ProjectileFinished(true);
        group.ProjectileFinished(true);

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Fewer_than_one_projectile_counts_as_one(int count)
    {
        var calls = 0;
        ProjectileGroup group = new(count, _ => calls++);

        group.ProjectileFinished(false);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Works_without_a_callback()
    {
        ProjectileGroup group = new(2, null);

        group.ProjectileFinished(true);
        group.ProjectileFinished(false);
    }
}
