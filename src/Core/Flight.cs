using Vec3 = System.Numerics.Vector3;

namespace AdaptiveArsenal.Core;

/// <summary>Where a projectile is and how fast it's going, in metres and metres per second.</summary>
/// <param name="Position">Where it is.</param>
/// <param name="Velocity">Its velocity.</param>
internal readonly record struct FlightState(Vec3 Position, Vec3 Velocity);

/// <summary>
/// Flies a projectile under gravity and drag, one physics step at a time, and works out the angle that zeroes a gun's
/// sights.
/// </summary>
/// <remarks>
/// The game moves its bullets with <see cref="Step"/>, and <see cref="ZeroAngle"/> simulates a shot with the same steps,
/// so a bullet really does cross the line of sight at the zero distance. It uses
/// <see cref="System.Numerics.Vector3"/>, so it works without Unity.
/// </remarks>
internal static class Flight
{
    /// <summary>The highest angle <see cref="ZeroAngle"/> tries, in degrees, where a shot without drag reaches furthest.</summary>
    private const int MaxZeroAngle = 45;

    /// <summary>How many times <see cref="ZeroAngle"/> halves its range of angles, which leaves it far finer than needed.</summary>
    private const int ZeroIterations = 30;

    /// <summary>The longest <see cref="ZeroAngle"/> follows a shot, in seconds, before deciding it can't reach.</summary>
    private const float MaxZeroFlightTime = 10f;

    /// <summary>The zero angles worked out so far, since every shot from the same gun and settings gets the same one.</summary>
    private static readonly Dictionary<(float MuzzleVelocity, DragModel Drag, float Gravity, float ZeroDistance, float DeltaTime), float> s_ZeroAngles = [];

    /// <summary>Moves a projectile on by one step under gravity and drag.</summary>
    /// <remarks>
    /// <para>
    /// G1 drag uses the midpoint method: the velocity halfway through the step sets how far it moves. That's much
    /// closer than moving at the starting velocity for the whole step, which matters at a rifle bullet's speed, when it
    /// covers about 15 m in each of Unity's default 0.02 s steps. Drag can at most stop the projectile within a step,
    /// never turn it around, however strong it is.
    /// </para>
    /// <para>
    /// Linear drag moves it the way PhysX, which Unity uses, moves a rigidbody: gravity is added to the velocity, the
    /// velocity is multiplied by 1 − drag × step (but not below 0), then the projectile moves at that velocity. So shots
    /// with linear drag fly as they did when they were rigidbodies, in <see cref="Atmosphere.Standard"/>.
    /// </para>
    /// <para>
    /// Drag works on the projectile's velocity through the air, its velocity less the wind's, so the wind pushes it
    /// along and a headwind slows it more. Denser air scales linear drag by its density, as it does G1 drag.
    /// </para>
    /// </remarks>
    /// <param name="state">Where the projectile is and its velocity.</param>
    /// <param name="gravity">Gravity, in m/s².</param>
    /// <param name="drag">How the air slows it.</param>
    /// <param name="atmosphere">The air it flies through.</param>
    /// <param name="deltaTime">The length of the step, in seconds.</param>
    /// <returns>Where the projectile is and its velocity at the end of the step.</returns>
    internal static FlightState Step(FlightState state, Vec3 gravity, DragModel drag, Atmosphere atmosphere, float deltaTime)
    {
        var wind = atmosphere.Wind;
        if (!drag.IsG1)
        {
            var damping = MathF.Max(0f, 1f - drag.LinearDrag * atmosphere.DensityRatio * deltaTime);
            var dampedVelocity = wind + (state.Velocity + gravity * deltaTime - wind) * damping;
            return new(state.Position + dampedVelocity * deltaTime, dampedVelocity);
        }

        var halfStepVelocity = state.Velocity + gravity * (deltaTime / 2f) + LimitedDrag(drag, state.Velocity - wind, atmosphere, deltaTime / 2f);
        var velocity = state.Velocity + gravity * deltaTime + LimitedDrag(drag, halfStepVelocity - wind, atmosphere, deltaTime);
        return new(state.Position + halfStepVelocity * deltaTime, velocity);
    }

    /// <summary>
    /// The velocity part way through a step, such as where the projectile hit something, interpolated between the
    /// velocities at its start and end.
    /// </summary>
    /// <param name="start">The velocity at the start of the step.</param>
    /// <param name="end">The velocity at the end of the step.</param>
    /// <param name="fraction">How far through the step, from 0 at the start to 1 at the end. It's clamped to that.</param>
    internal static Vec3 VelocityPartWay(Vec3 start, Vec3 end, float fraction) =>
        Vec3.Lerp(start, end, Math.Clamp(fraction, 0f, 1f));

    /// <summary>
    /// The change in velocity G1 drag makes over <paramref name="deltaTime"/>, limited so it can at most bring the
    /// projectile to a stop in the air, moving with the wind, rather than turn it around.
    /// </summary>
    /// <remarks>
    /// Drag strong enough to stop a projectile within one step, like a ballistic coefficient near 0 from another mod,
    /// would otherwise reverse its velocity and make the next step's drag even stronger.
    /// </remarks>
    /// <param name="drag">How the air slows the projectile.</param>
    /// <param name="airVelocity">The projectile's velocity through the air, which drag works on.</param>
    /// <param name="atmosphere">The air.</param>
    /// <param name="deltaTime">How long it works for, in seconds.</param>
    private static Vec3 LimitedDrag(DragModel drag, Vec3 airVelocity, Atmosphere atmosphere, float deltaTime)
    {
        var change = drag.Acceleration(airVelocity, atmosphere) * deltaTime;
        var speed = airVelocity.Length();
        var changeSize = change.Length();
        return changeSize > speed ? change * (speed / changeSize) : change;
    }

    /// <summary>
    /// Works out how far above the line of sight to aim the barrel, in degrees, so a shot crosses it again at
    /// <paramref name="zeroDistance"/>: the angle a real gun's sights are set at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The angle is for a level shot in still air at 15 °C (<see cref="Atmosphere.Standard"/>), as a gun is zeroed once
    /// on a flat range, so wind and cold still move the shot off the point of aim. The shot starts on the line of sight. It
    /// tries angles a degree apart until one reaches the line at that distance, then halves the gap between it and the
    /// one before until the shot lands on the line. That finds the low, flat path, as real sights give, rather than a
    /// high lob.
    /// </para>
    /// <para>
    /// With strong drag a shot reaches furthest below 45°, so the search stops once a higher angle gets less far than
    /// a lower one. Each answer is remembered, since every shot from a gun with the same settings gets the same one.
    /// </para>
    /// </remarks>
    /// <param name="muzzleVelocity">How fast the shot leaves the muzzle, in m/s.</param>
    /// <param name="drag">How the air slows it.</param>
    /// <param name="gravity">How strong gravity is, in m/s².</param>
    /// <param name="zeroDistance">Where the shot should cross the line of sight, in metres.</param>
    /// <param name="deltaTime">The length of each physics step the shot will fly with, in seconds.</param>
    /// <returns>
    /// The angle, or 0 if any of the numbers is 0 or less, or the shot can't reach the line of sight at the zero distance
    /// at any angle up to <see cref="MaxZeroAngle"/>.
    /// </returns>
    internal static float ZeroAngle(float muzzleVelocity, DragModel drag, float gravity, float zeroDistance, float deltaTime)
    {
        if (muzzleVelocity <= 0f || gravity <= 0f || zeroDistance <= 0f || deltaTime <= 0f)
            return 0f;

        var key = (muzzleVelocity, drag, gravity, zeroDistance, deltaTime);
        if (!s_ZeroAngles.TryGetValue(key, out var angle))
        {
            angle = FindZeroAngle(muzzleVelocity, drag, gravity, zeroDistance, deltaTime);
            s_ZeroAngles[key] = angle;
        }

        return angle;
    }

    /// <summary>Does the search for <see cref="ZeroAngle"/>, which remembers what it finds.</summary>
    /// <param name="muzzleVelocity">How fast the shot leaves the muzzle, in m/s.</param>
    /// <param name="drag">How the air slows it.</param>
    /// <param name="gravity">How strong gravity is, in m/s².</param>
    /// <param name="zeroDistance">Where the shot should cross the line of sight, in metres.</param>
    /// <param name="deltaTime">The length of each physics step, in seconds.</param>
    /// <returns>The angle, or 0 if no angle up to <see cref="MaxZeroAngle"/> reaches the line of sight.</returns>
    private static float FindZeroAngle(float muzzleVelocity, DragModel drag, float gravity, float zeroDistance, float deltaTime)
    {
        float Height(float angle) => HeightAtDistance(angle, muzzleVelocity, drag, gravity, zeroDistance, deltaTime);

        // The highest angle found that's still below the line, and the lowest at or above it.
        var low = 0f;
        var lowHeight = Height(low);
        float? high = null;

        for (var degrees = 1; degrees <= MaxZeroAngle; degrees++)
        {
            var height = Height(degrees);
            if (height >= 0f)
            {
                high = degrees;
                break;
            }

            // Past the angle that reaches furthest, so no higher one will reach the line either.
            if (height < lowHeight)
                break;

            low = degrees;
            lowHeight = height;
        }

        if (high is not { } upper)
            return 0f;

        for (var i = 0; i < ZeroIterations; i++)
        {
            var middle = (low + upper) / 2f;
            if (Height(middle) < 0f)
                low = middle;
            else
                upper = middle;
        }

        return (low + upper) / 2f;
    }

    /// <summary>
    /// How far above the line of sight a level shot is when it has travelled <paramref name="distance"/>, flying with
    /// <see cref="Step"/> and interpolated between steps.
    /// </summary>
    /// <param name="angle">How far above level the shot is fired, in degrees.</param>
    /// <param name="muzzleVelocity">How fast it leaves the muzzle, in m/s.</param>
    /// <param name="drag">How the air slows it.</param>
    /// <param name="gravity">How strong gravity is, in m/s².</param>
    /// <param name="distance">How far along the line of sight, in metres.</param>
    /// <param name="deltaTime">The length of each step, in seconds.</param>
    /// <returns>The height in metres, or negative infinity if the shot never gets that far.</returns>
    internal static float HeightAtDistance(float angle, float muzzleVelocity, DragModel drag, float gravity, float distance,
        float deltaTime)
    {
        var radians = angle * MathF.PI / 180f;
        FlightState state = new(Vec3.Zero, new(muzzleVelocity * MathF.Cos(radians), muzzleVelocity * MathF.Sin(radians), 0f));
        Vec3 gravityVector = new(0f, -gravity, 0f);

        for (var time = 0f; time < MaxZeroFlightTime; time += deltaTime)
        {
            var next = Step(state, gravityVector, drag, Atmosphere.Standard, deltaTime);
            if (next.Position.X >= distance)
            {
                var t = (distance - state.Position.X) / (next.Position.X - state.Position.X);
                return state.Position.Y + (next.Position.Y - state.Position.Y) * t;
            }

            if (next.Velocity.X <= 0f)
                break;

            state = next;
        }

        return float.NegativeInfinity;
    }
}
