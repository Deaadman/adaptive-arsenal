using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class ProjectileGroupTests
{
    private int m_Reports;
    private int m_Fired;
    private List<string> m_Hits = [];

    private ProjectileGroup<string> Group(int count, float timeLimit = 0f) =>
        new(count, 0f, timeLimit, (fired, hits) =>
        {
            m_Reports++;
            m_Fired = fired;
            m_Hits = [.. hits];
        });

    [Fact]
    public void Reports_once_when_the_last_projectile_is_done()
    {
        var group = Group(3);

        group.ProjectileHit("deer", 0.1f);
        group.ProjectileMissed(0.1f);
        Assert.Equal(0, m_Reports);

        group.ProjectileHit("tree", 0.2f);
        Assert.Equal(1, m_Reports);
        Assert.Equal(3, m_Fired);
        Assert.Equal(["deer", "tree"], m_Hits);
    }

    [Fact]
    public void Reports_when_nothing_hit()
    {
        var group = Group(2);

        group.ProjectileMissed(1f);
        group.ProjectileMissed(2f);

        Assert.Equal(1, m_Reports);
        Assert.Empty(m_Hits);
    }

    [Fact]
    public void Reports_at_the_time_limit_with_what_has_hit_so_far()
    {
        var group = Group(3, timeLimit: 0.25f);

        group.ProjectileHit("deer", 0.1f);
        group.Update(0.2f);
        Assert.Equal(0, m_Reports);

        group.Update(0.25f);
        Assert.Equal(1, m_Reports);
        Assert.Equal(3, m_Fired);
        Assert.Equal(["deer"], m_Hits);
    }

    [Fact]
    public void A_hit_after_the_time_limit_counts_as_a_miss()
    {
        var group = Group(2, timeLimit: 0.25f);

        group.ProjectileHit("deer", 0.1f);
        group.ProjectileHit("wolf", 0.3f);

        Assert.Equal(1, m_Reports);
        Assert.Equal(["deer"], m_Hits);
        Assert.True(group.IsReported);
    }

    [Fact]
    public void Reports_before_the_time_limit_when_every_projectile_is_done()
    {
        var group = Group(2, timeLimit: 0.25f);

        group.ProjectileHit("deer", 0.05f);
        group.ProjectileHit("deer", 0.06f);
        Assert.Equal(1, m_Reports);

        group.Update(1f);
        Assert.Equal(1, m_Reports);
    }

    [Fact]
    public void Without_a_time_limit_it_waits_for_every_projectile()
    {
        var group = Group(2);

        group.ProjectileHit("deer", 0.1f);
        group.Update(100f);
        Assert.Equal(0, m_Reports);

        group.ProjectileMissed(100f);
        Assert.Equal(1, m_Reports);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Fewer_than_one_projectile_counts_as_one(int count)
    {
        var group = Group(count);

        group.ProjectileMissed(0f);

        Assert.Equal(1, m_Reports);
        Assert.Equal(1, m_Fired);
    }
}
