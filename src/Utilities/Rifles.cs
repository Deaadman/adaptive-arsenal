using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>Which guns are the game's own bolt-action rifles, for the safety and the manual bolt.</summary>
internal static class Rifles
{
    /// <summary>Whether a gun is one of the game's rifles, which are all bolt actions.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool IsBoltAction(GunItem gunItem) =>
        gunItem.m_GunType == GunType.Rifle && BoltCycle.IsBoltAction(gunItem.name);
}
