namespace AdaptiveArsenal.Core;

/// <summary>
/// The numbers behind sprinting while crouched and sliding.
/// </summary>
/// <remarks>
/// A slide starts at the speed the player was sprinting, a little faster, and slows down as friction and the slope
/// take their share each frame. Downhill it can speed up, to <see cref="MaxSlideSpeed"/>; uphill it stops sooner.
/// Speeds are in metres per second, slopes in metres up per metre along.
/// </remarks>
internal static class Movement
{
    /// <summary>The acceleration due to gravity, in metres per second squared.</summary>
    internal const float Gravity = 9.81f;

    /// <summary>The friction between the player and the ground while sliding, at a <see cref="SlideDistanceMultiplier"/> of 1.</summary>
    internal const float BaseSlideFriction = 0.35f;

    /// <summary>A slide starts this much faster than the player was sprinting.</summary>
    internal const float SlideStartBoost = 1.15f;

    /// <summary>The fastest a slide goes, however steep the hill.</summary>
    internal const float MaxSlideSpeed = 10f;

    /// <summary>The slowest the player can be moving and still start a slide.</summary>
    internal const float MinSlideStartSpeed = 3f;

    /// <summary>A slide ends when it slows below this, leaving the player crouched.</summary>
    internal const float SlideEndSpeed = 1.5f;

    /// <summary>The longest a slide lasts, in seconds, so a long hill doesn't carry the player down it forever.</summary>
    internal const float MaxSlideSeconds = 3f;

    /// <summary>How long a slide can be off the ground, in seconds, before it ends, so small bumps don't end it.</summary>
    internal const float MaxSlideAirborneSeconds = 0.2f;

    /// <summary>How far below the player's middle, beyond half their height, to look for the ground under a slide, in metres.</summary>
    internal const float GroundProbeDistance = 0.5f;

    /// <summary>The smallest sprint multiplier <see cref="CrouchSprintAccelerationMultiplier"/> divides by, so a broken one can't make it huge.</summary>
    private const float MinSprintMultiplier = 0.1f;

    /// <summary>How much faster than crouch-walking the player moves when sprinting while crouched.</summary>
    internal static float CrouchSprintSpeedMultiplier { get; set; } = 1.75f;

    /// <summary>How far a slide carries the player, compared to the mod's default. Friction is divided by this.</summary>
    internal static float SlideDistanceMultiplier { get; set; } = 1f;

    /// <summary>The friction while sliding: <see cref="BaseSlideFriction"/> divided by <see cref="SlideDistanceMultiplier"/>.</summary>
    internal static float SlideFriction => BaseSlideFriction / Math.Max(SlideDistanceMultiplier, 0.01f);

    /// <summary>
    /// The speed a slide starts at.
    /// </summary>
    /// <param name="entrySpeed">How fast the player was moving across the ground when the slide started.</param>
    internal static float SlideStartSpeed(float entrySpeed) => Math.Clamp(entrySpeed * SlideStartBoost, 0f, MaxSlideSpeed);

    /// <summary>
    /// How steep the ground is along a direction: how many metres it rises for each metre across. Negative is downhill.
    /// </summary>
    /// <param name="normalX">The ground's normal, X.</param>
    /// <param name="normalY">The ground's normal, Y, which is up.</param>
    /// <param name="normalZ">The ground's normal, Z.</param>
    /// <param name="directionX">The direction across the ground, X. With <paramref name="directionZ"/>, a unit vector.</param>
    /// <param name="directionZ">The direction across the ground, Z.</param>
    /// <returns>The rise per metre, or 0 for a wall or ceiling, whose normal doesn't point up.</returns>
    internal static float Rise(float normalX, float normalY, float normalZ, float directionX, float directionZ) =>
        normalY <= 0f ? 0f : -(normalX * directionX + normalZ * directionZ) / normalY;

    /// <summary>
    /// The speed of a slide after one frame.
    /// </summary>
    /// <remarks>
    /// The slope pulls the player back uphill or on downhill, and friction slows them on any slope. Never below 0 or
    /// above <see cref="MaxSlideSpeed"/>.
    /// </remarks>
    /// <param name="speed">The speed at the start of the frame.</param>
    /// <param name="rise">How steep the ground is along the slide, as from <see cref="Rise"/>.</param>
    /// <param name="deltaTime">The frame's length, in seconds.</param>
    internal static float NextSlideSpeed(float speed, float rise, float deltaTime)
    {
        var length = MathF.Sqrt(1f + rise * rise);
        var sin = rise / length;
        var cos = 1f / length;
        var acceleration = -Gravity * (sin + SlideFriction * cos);
        return Math.Clamp(speed + acceleration * deltaTime, 0f, MaxSlideSpeed);
    }

    /// <summary>Whether a slide is over: it's slowed below <see cref="SlideEndSpeed"/> or lasted <see cref="MaxSlideSeconds"/>.</summary>
    /// <param name="speed">The slide's speed now.</param>
    /// <param name="seconds">How long the slide has lasted.</param>
    internal static bool SlideHasEnded(float speed, float seconds) => speed < SlideEndSpeed || seconds >= MaxSlideSeconds;

    /// <summary>Whether a slide has been off the ground too long, more than <see cref="MaxSlideAirborneSeconds"/>.</summary>
    /// <param name="airborneSeconds">How long the slide has been off the ground.</param>
    internal static bool SlideLeftGround(float airborneSeconds) => airborneSeconds > MaxSlideAirborneSeconds;

    /// <summary>
    /// The slide's speed after a frame's move: unchanged, unless it hit something to the side, when it's slowed to
    /// however far it actually got.
    /// </summary>
    /// <remarks>
    /// Only a hit to the side counts, so the ground under the slide, which often turns the move a little, doesn't
    /// slow it.
    /// </remarks>
    /// <param name="speed">The speed the slide moved at.</param>
    /// <param name="movedAcross">How far it actually moved across the ground.</param>
    /// <param name="deltaTime">The frame's length, in seconds.</param>
    /// <param name="hitSide">Whether the move hit something to the side, like a wall or a tree.</param>
    internal static float SlideSpeedAfterMove(float speed, float movedAcross, float deltaTime, bool hitSide) =>
        hitSide && deltaTime > 0f ? Math.Min(speed, movedAcross / deltaTime) : speed;

    /// <summary>
    /// What the controller's acceleration is multiplied by while sprinting crouched, so the player moves
    /// <see cref="CrouchSprintSpeedMultiplier"/> times as fast as crouch-walking.
    /// </summary>
    /// <remarks>
    /// The game counts sprinting crouched as sprinting, so it multiplies the speed by its own sprint multiplier too.
    /// This divides that back out.
    /// </remarks>
    /// <param name="gameSprintMultiplier">The game's sprint speed multiplier, which it's already applying.</param>
    internal static float CrouchSprintAccelerationMultiplier(float gameSprintMultiplier) =>
        CrouchSprintSpeedMultiplier / Math.Max(gameSprintMultiplier, MinSprintMultiplier);

    /// <summary>
    /// Whether the player's movement input is enough to sprint: mostly forward, and not too much to the side.
    /// </summary>
    /// <param name="forward">The forward input, from -1 (back) to 1.</param>
    /// <param name="strafe">The sideways input, from -1 (left) to 1.</param>
    /// <param name="minForward">The least forward input that sprints.</param>
    /// <param name="maxStrafe">The most sideways input, either way, that sprints.</param>
    internal static bool InputCanSprint(float forward, float strafe, float minForward, float maxStrafe) =>
        forward >= minForward && Math.Abs(strafe) <= maxStrafe;

    /// <summary>How much stamina sprinting uses in a frame.</summary>
    /// <param name="usagePerSecond">The stamina sprinting uses each second.</param>
    /// <param name="deltaTime">The frame's length, in seconds.</param>
    internal static float StaminaUsed(float usagePerSecond, float deltaTime) => Math.Max(usagePerSecond * deltaTime, 0f);
}
