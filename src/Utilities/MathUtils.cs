using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// <see cref="Curves"/> for Unity's vectors.
/// </summary>
internal static class MathUtils
{
    /// <summary>
    /// Gets a point on a quadratic Bézier curve.
    /// </summary>
    /// <param name="start">Where the curve starts.</param>
    /// <param name="control">The control point, which the curve bends towards but doesn't pass through.</param>
    /// <param name="end">Where the curve ends.</param>
    /// <param name="t">How far along the curve, from 0 at <paramref name="start"/> to 1 at <paramref name="end"/>.</param>
    /// <returns>The point at <paramref name="t"/>.</returns>
    internal static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t) =>
        new(
            Curves.QuadraticBezier(start.x, control.x, end.x, t),
            Curves.QuadraticBezier(start.y, control.y, end.y, t),
            Curves.QuadraticBezier(start.z, control.z, end.z, t));
}
