using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// A safety on bolt-action rifles, which the player puts on and off with <see cref="Settings.m_SafetyKey"/>. While it's
/// on, the trigger does nothing: the rifle doesn't fire, and its bolt can't be cycled with the trigger.
/// </summary>
/// <remarks>
/// Only the rifle in the player's hands has a safety state. Each time a rifle is equipped, its safety starts on or off
/// as <see cref="Settings.m_SafetyOnWhenEquipped"/> says. Only while <see cref="Settings.m_RifleSafety"/> is on.
/// </remarks>
internal static class RifleSafety
{
    /// <summary>
    /// How long the safety's HUD messages show, in seconds: briefly, so a quick run of them doesn't queue up and show
    /// a state that's no longer true.
    /// </summary>
    private const float MessageSeconds = 1f;

    /// <summary>The rifle in the player's hands, or null when there isn't one with a safety.</summary>
    private static GunItem? s_GunItem;

    /// <summary>Whether <see cref="s_GunItem"/>'s safety is on.</summary>
    private static bool s_IsOn;

    /// <summary>Whether a gun has a safety: the game's own rifles, while the setting is on.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool AppliesTo(GunItem gunItem) => Settings.Instance.m_RifleSafety && Rifles.IsBoltAction(gunItem);

    /// <summary>Starts following a rifle the player has just equipped, with its safety set as the settings say.</summary>
    /// <param name="gearItem">The item the player equipped.</param>
    internal static void Equipped(GearItem? gearItem)
    {
        var gunItem = gearItem != null ? gearItem.m_GunItem : null;
        if (gunItem == null || !AppliesTo(gunItem))
        {
            s_GunItem = null;
            return;
        }

        if (gunItem == s_GunItem)
            return;

        s_GunItem = gunItem;
        s_IsOn = Settings.Instance.m_SafetyOnWhenEquipped;
    }

    /// <summary>
    /// Toggles the safety when the player presses the key, and stops following the rifle once it's out of their hands.
    /// Called every frame, and does nothing unless a rifle is equipped.
    /// </summary>
    internal static void Update()
    {
        // Unity objects need == null, not ?., to tell when they've been destroyed.
        if (s_GunItem == null)
            return;

        var playerManager = GameManager.GetPlayerManagerComponent();
        if (playerManager == null
            || playerManager.m_ItemInHands == null
            || playerManager.m_ItemInHands.m_GunItem != s_GunItem
            || !AppliesTo(s_GunItem))
        {
            s_GunItem = null;
            return;
        }

        if (!Input.GetKeyDown(Settings.Instance.m_SafetyKey) || !CanTakeInput())
            return;

        s_IsOn = !s_IsOn;
        ShowMessage(s_IsOn ? "Safety on" : "Safety off");
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
        if (!s_IsOn || gunItem == null || gunItem != s_GunItem || !AppliesTo(gunItem))
            return false;

        if (pull.IsNew && GameManager.GetPlayerAnimationComponent().IsAllowedToFire(gunItem.m_AllowHipFire, gunItem.m_GunType))
            ShowMessage("Safety on");

        pull.Spend();
        return true;
    }

    /// <summary>
    /// Whether the player is in control in the game world, rather than in a menu, the pause screen or the console.
    /// </summary>
    private static bool CanTakeInput() =>
        !GameManager.m_IsPaused
        && !InterfaceManager.IsOverlayActiveCached()
        && !uConsole.IsOn()
        && InputManager.HasContext(InputManager.m_CurrentContext);

    /// <summary>Shows a short, high-priority message on the HUD.</summary>
    /// <param name="message">The message.</param>
    private static void ShowMessage(string message) => HUDMessage.AddMessage(message, MessageSeconds, highPriority: true);
}
