using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Freezes and thaws the rifles in the player's inventory with the temperature around the player, as
/// <see cref="RifleFreezing"/> works out, and keeps how frozen each is in its <see cref="WeaponData.m_PercentFrozen"/>.
/// </summary>
/// <remarks>
/// Only rifles the player carries change: one left on the ground or in a container stays as it was until it's picked
/// up again. <see cref="Mod"/> calls <see cref="Update"/> every frame, and it works out the in-game time since its last
/// update, so time passing while sleeping or waiting counts too.
/// </remarks>
internal static class FrozenRifles
{
    /// <summary>How often, in real seconds, the rifles are updated.</summary>
    private const float UpdateInterval = 1f;

    /// <summary>The in-game hours played at the last update, or -1 to start counting again.</summary>
    private static float s_LastHoursPlayed = -1f;

    /// <summary>When, in real seconds, the next update is.</summary>
    private static float s_NextUpdateTime;

    /// <summary>Whether frozen rifles are on.</summary>
    internal static bool Enabled => Settings.Instance.m_FrozenRiflesEnabled;

    /// <summary>
    /// Starts counting time again, when a save is loaded or the player leaves it, so the jump in hours played isn't
    /// taken as time passing.
    /// </summary>
    internal static void Reset() => s_LastHoursPlayed = -1f;

    /// <summary>Updates how frozen each rifle in the player's inventory is, once every <see cref="UpdateInterval"/>.</summary>
    internal static void Update()
    {
        if (Time.unscaledTime < s_NextUpdateTime)
            return;

        s_NextUpdateTime = Time.unscaledTime + UpdateInterval;

        if (!Enabled || GameManager.IsMainMenuActive() || GameManager.m_IsPaused)
        {
            Reset();
            return;
        }

        var timeOfDay = GameManager.GetTimeOfDayComponent();
        var weather = GameManager.GetWeatherComponent();
        var inventory = GameManager.GetInventoryComponent();
        if (timeOfDay == null || weather == null || inventory == null || inventory.m_Items == null)
            return;

        var hoursPlayed = timeOfDay.GetHoursPlayedNotPaused();
        var hours = hoursPlayed - s_LastHoursPlayed;
        var firstUpdate = s_LastHoursPlayed < 0f;
        s_LastHoursPlayed = hoursPlayed;

        if (firstUpdate || hours <= 0f)
            return;

        var temperature = weather.GetCurrentTemperature();

        foreach (var item in inventory.m_Items)
        {
            var gearItem = item?.m_GearItem;
            if (gearItem == null || !IsRifle(gearItem))
                continue;

            var hasData = WeaponDataManager.TryGet(gearItem, out var data);
            var percentFrozen = hasData ? data!.m_PercentFrozen : 0f;
            var newPercentFrozen = RifleFreezing.Update(percentFrozen, hours, temperature);

            // A rifle that's never been frozen doesn't need data saved for it.
            if (newPercentFrozen != percentFrozen)
                (hasData ? data! : WeaponDataManager.Get(gearItem)).m_PercentFrozen = newPercentFrozen;
        }
    }

    /// <summary>How frozen a rifle is, from 0 to 100, or 0 for anything else or when frozen rifles are off.</summary>
    /// <param name="gearItem">The item.</param>
    internal static float GetPercentFrozen(GearItem gearItem) =>
        Enabled && IsRifle(gearItem) && WeaponDataManager.TryGet(gearItem, out var data) ? data.m_PercentFrozen : 0f;

    /// <summary>Whether an item is a rifle, the only weapons that freeze.</summary>
    /// <param name="gearItem">The item.</param>
    private static bool IsRifle(GearItem gearItem) => gearItem.m_GunItem != null && gearItem.m_GunItem.m_GunType == GunType.Rifle;
}
