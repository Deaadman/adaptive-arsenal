using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// A safety on bolt-action rifles, which the player puts on and off with <see cref="Settings.m_SafetyKey"/>. While it's
/// on, the trigger does nothing, and the bolt can't be worked: the rifle doesn't fire, and its bolt can't be cycled.
/// </summary>
/// <remarks>
/// Only the rifle in the player's hands (<see cref="Rifles.InHands"/>) has a safety state. Each time a rifle is
/// equipped, its safety starts on or off as <see cref="Settings.m_SafetyOnWhenEquipped"/> says. Only while
/// <see cref="Settings.m_RifleSafety"/> is on.
/// </remarks>
internal static class RifleSafety
{
    /// <summary>Whether the safety of the rifle in the player's hands is on.</summary>
    private static bool s_IsOn;

    /// <summary>Whether the safety of a gun is on, so its trigger and bolt do nothing.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool IsOn(GunItem gunItem) =>
        s_IsOn && Settings.Instance.m_RifleSafety && gunItem == Rifles.InHands;

    /// <summary>Sets the safety of a rifle the player has just equipped, as the settings say.</summary>
    internal static void Equipped() => s_IsOn = Settings.Instance.m_SafetyOnWhenEquipped;

    /// <summary>Puts the safety on or off, if rifles have one.</summary>
    internal static void Toggle()
    {
        if (!Settings.Instance.m_RifleSafety)
            return;

        s_IsOn = !s_IsOn;
        Rifles.ShowMessage(s_IsOn ? "Safety on" : "Safety off");
        Mod.Logger.Log($"Put the rifle's safety {(s_IsOn ? "on" : "off")}.", FlaggedLoggingLevel.Trace);
    }

    /// <summary>
    /// Whether the safety stops a trigger pull. A blocked pull is spent, so taking the safety off while still holding
    /// the trigger doesn't fire.
    /// </summary>
    /// <remarks>
    /// The message only shows on a new pull the game would otherwise fire, while aiming, so the player isn't told about
    /// the safety for pulls that would do nothing anyway.
    /// </remarks>
    /// <param name="shooter">The shooter whose trigger was pulled.</param>
    /// <param name="pull">The trigger, already updated for this frame.</param>
    internal static bool BlocksTrigger(vp_FPSShooter shooter, TriggerPull pull)
    {
        var gunItem = shooter.m_Weapon != null ? shooter.m_Weapon.m_GunItem : null;
        if (gunItem == null || !IsOn(gunItem))
            return false;

        if (pull.IsNew && GameManager.GetPlayerAnimationComponent().IsAllowedToFire(gunItem.m_AllowHipFire, gunItem.m_GunType))
            Rifles.ShowMessage("Safety on");

        pull.Spend();
        return true;
    }
}
