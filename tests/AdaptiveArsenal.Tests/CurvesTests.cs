using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class CurvesTests
{
    private const int Precision = 4;

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 10f)]
    [InlineData(0.5f, 7.5f)]
    public void Bezier_starts_and_ends_on_its_end_points_and_bends_towards_the_control(float t, float expected) =>
        Assert.Equal(expected, Curves.QuadraticBezier(0f, 10f, 10f, t), Precision);

    [Fact]
    public void Bezier_with_a_control_on_the_line_is_a_straight_line()
    {
        for (var t = 0f; t <= 1f; t += 0.125f)
            Assert.Equal(t * 8f, Curves.QuadraticBezier(0f, 4f, 8f, t), Precision);
    }

    [Fact]
    public void A_path_within_the_limit_keeps_every_point() =>
        Assert.Equal(0, Curves.PointsToDrop([0f, 50f, 100f, 150f], Distance, maxLength: 200f));

    [Fact]
    public void A_path_exactly_at_the_limit_keeps_every_point() =>
        Assert.Equal(0, Curves.PointsToDrop([0f, 100f, 200f], Distance, maxLength: 200f));

    [Fact]
    public void A_long_path_drops_the_segment_that_went_over_and_everything_older()
    {
        // From the newest point back: 50, 100, 150 and 200 m are within the limit; the segment to 0 makes it 250.
        float[] path = [0f, 50f, 100f, 150f, 200f, 250f];

        Assert.Equal(1, Curves.PointsToDrop(path, Distance, maxLength: 200f));
    }

    [Fact]
    public void What_is_left_is_never_longer_than_the_limit()
    {
        float[] path = [0f, 30f, 95f, 170f, 180f, 260f, 400f, 410f];

        var kept = path.Skip(Curves.PointsToDrop(path, Distance, maxLength: 200f)).ToArray();

        Assert.True(kept[^1] - kept[0] <= 200f);
        Assert.Equal(410f, kept[^1]);
    }

    [Fact]
    public void A_segment_longer_than_the_limit_leaves_only_the_newest_point() =>
        Assert.Equal(1, Curves.PointsToDrop([0f, 1000f], Distance, maxLength: 200f));

    [Fact]
    public void An_empty_or_single_point_path_drops_nothing()
    {
        Assert.Equal(0, Curves.PointsToDrop(Array.Empty<float>(), Distance, maxLength: 200f));
        Assert.Equal(0, Curves.PointsToDrop([5f], Distance, maxLength: 200f));
    }

    private static float Distance(float a, float b) => Math.Abs(a - b);
}
