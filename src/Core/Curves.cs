namespace AdaptiveArsenal.Core;

/// <summary>
/// Curves and paths, one axis or point type at a time, so they work without Unity's vectors.
/// </summary>
internal static class Curves
{
    /// <summary>
    /// Gets one axis of a point on a quadratic Bézier curve.
    /// </summary>
    /// <param name="start">Where the curve starts.</param>
    /// <param name="control">The control point, which the curve bends towards but doesn't pass through.</param>
    /// <param name="end">Where the curve ends.</param>
    /// <param name="t">How far along the curve, from 0 at <paramref name="start"/> to 1 at <paramref name="end"/>.</param>
    internal static float QuadraticBezier(float start, float control, float end, float t)
    {
        var u = 1 - t;
        return u * u * start + 2 * u * t * control + t * t * end;
    }

    /// <summary>
    /// How many of the oldest points to drop so a path is no longer than <paramref name="maxLength"/>.
    /// </summary>
    /// <remarks>
    /// Walks back from the newest point, adding up the length of each segment. Once the total is past
    /// <paramref name="maxLength"/>, the segment that took it over goes, along with every older point. So the path
    /// left is never longer than <paramref name="maxLength"/>, and one segment longer than that leaves only the
    /// newest point.
    /// </remarks>
    /// <typeparam name="TPoint">The type of point, like Unity's <c>Vector3</c>.</typeparam>
    /// <param name="points">The path, oldest point first.</param>
    /// <param name="distance">The distance between two points.</param>
    /// <param name="maxLength">The longest the path can be.</param>
    /// <returns>How many points to remove from the start of <paramref name="points"/>.</returns>
    internal static int PointsToDrop<TPoint>(IReadOnlyList<TPoint> points, Func<TPoint, TPoint, float> distance, float maxLength)
    {
        var length = 0f;
        for (var i = points.Count - 1; i > 0; i--)
        {
            length += distance(points[i], points[i - 1]);
            if (length > maxLength)
                return i;
        }

        return 0;
    }
}
