namespace AdaptiveArsenal.Core;

/// <summary>
/// How rifles freeze in the cold and thaw in the warm, and how much more likely a frozen rifle is to jam.
/// </summary>
/// <remarks>
/// How frozen a rifle is goes from 0 (not at all) to 100 (frozen solid), like the game's frozen clothing. Below
/// <see cref="FreezingPoint"/> it freezes, faster the colder it is, and above it it thaws, faster the warmer it is. The
/// temperature is the game's, which already counts being indoors and near a fire.
/// </remarks>
internal static class RifleFreezing
{
    /// <summary>The temperature, in °C, below which rifles freeze and above which they thaw.</summary>
    internal const float FreezingPoint = 0f;

    /// <summary>How far below <see cref="FreezingPoint"/>, in °C, a rifle freezes as fast as it can.</summary>
    internal const float FullColdBelowFreezing = 20f;

    /// <summary>How far above <see cref="FreezingPoint"/>, in °C, a rifle thaws in <see cref="HoursToThaw"/>.</summary>
    internal const float ThawWarmthAboveFreezing = 10f;

    /// <summary>
    /// The least a rifle's thawing is slowed, as a share of its speed at <see cref="ThawWarmthAboveFreezing"/>, so it
    /// still thaws just above freezing.
    /// </summary>
    internal const float MinThawSpeed = 0.25f;

    /// <summary>The most a rifle's thawing is sped up, like right by a fire.</summary>
    internal const float MaxThawSpeed = 3f;

    /// <summary>
    /// How many in-game hours a rifle takes to freeze solid at <see cref="FullColdBelowFreezing"/> or colder. Set from
    /// <c>Settings.m_HoursToFreeze</c>.
    /// </summary>
    internal static float HoursToFreeze = 6f;

    /// <summary>
    /// How many in-game hours a frozen-solid rifle takes to thaw at <see cref="ThawWarmthAboveFreezing"/>. Set from
    /// <c>Settings.m_HoursToThaw</c>.
    /// </summary>
    internal static float HoursToThaw = 2f;

    /// <summary>
    /// The extra chance, in percent, that a frozen-solid rifle jams on each shot. A rifle that's partly frozen gets
    /// that share of it. Set from <c>Settings.m_FrozenJamChance</c>.
    /// </summary>
    internal static float MaxJamChance = 25f;

    /// <summary>How frozen a rifle is after some time at a temperature.</summary>
    /// <param name="percentFrozen">How frozen it was, from 0 to 100.</param>
    /// <param name="hours">The in-game hours that have passed.</param>
    /// <param name="temperature">The temperature around it, in °C.</param>
    internal static float Update(float percentFrozen, float hours, float temperature)
    {
        if (hours <= 0f)
            return Clamp(percentFrozen, 0f, 100f);

        float change;
        if (temperature < FreezingPoint)
        {
            var coldness = Clamp((FreezingPoint - temperature) / FullColdBelowFreezing, 0f, 1f);
            change = HoursToFreeze > 0f ? 100f / HoursToFreeze * coldness * hours : 100f;
        }
        else
        {
            var warmth = Clamp((temperature - FreezingPoint) / ThawWarmthAboveFreezing, MinThawSpeed, MaxThawSpeed);
            change = -(HoursToThaw > 0f ? 100f / HoursToThaw * warmth * hours : 100f);
        }

        return Clamp(percentFrozen + change, 0f, 100f);
    }

    /// <summary>The extra chance, in percent, that a rifle this frozen jams on a shot.</summary>
    /// <param name="percentFrozen">How frozen it is, from 0 to 100.</param>
    internal static float JamChance(float percentFrozen) => MaxJamChance * Clamp(percentFrozen / 100f, 0f, 1f);

    /// <summary>
    /// The game's own chance, in percent, that a gun jams on a shot: none at or above the condition threshold, then
    /// rising from the minimum chance just under it to the maximum at no condition.
    /// </summary>
    /// <remarks>
    /// This is worked out from the names of the shooter's settings (<c>JamConditionThreshold</c>,
    /// <c>JamMinimumChance</c> and <c>JamMaximumChance</c>), since the game's code for it can't be read. It's only used
    /// for frozen rifles, to add their chance to the game's.
    /// </remarks>
    /// <param name="condition">The gun's condition, from 0 to 1.</param>
    /// <param name="threshold">The condition, from 0 to 1, below which the gun can jam.</param>
    /// <param name="minimumChance">The chance just below <paramref name="threshold"/>.</param>
    /// <param name="maximumChance">The chance at no condition.</param>
    internal static float GameJamChance(float condition, float threshold, float minimumChance, float maximumChance)
    {
        if (threshold <= 0f || condition >= threshold)
            return 0f;

        var t = Clamp(condition / threshold, 0f, 1f);
        return maximumChance + (minimumChance - maximumChance) * t;
    }

    /// <summary>The chance, in percent, that at least one of two independent chances happens.</summary>
    /// <param name="a">One chance, in percent.</param>
    /// <param name="b">The other chance, in percent.</param>
    internal static float CombineChances(float a, float b)
    {
        var missA = 1f - Clamp(a, 0f, 100f) / 100f;
        var missB = 1f - Clamp(b, 0f, 100f) / 100f;
        return (1f - missA * missB) * 100f;
    }

    private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
}
