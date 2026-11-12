using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// The game's own bolt-action rifles: which guns they are, which one is in the player's hands, and the keys for its
/// safety (<see cref="RifleSafety"/>) and for working its bolt (<see cref="ManualBolt"/>).
/// </summary>
internal static class Rifles
{
    /// <summary>
    /// How long the HUD messages about rifles show, in seconds: briefly, so a quick run of them doesn't queue up and
    /// show something that's no longer true.
    /// </summary>
    private const float MessageSeconds = 1f;

    /// <summary>The bolt-action rifle in the player's hands, or null when there isn't one.</summary>
    internal static GunItem? InHands { get; private set; }

    /// <summary>Whether a gun is one of the game's rifles, which are all bolt actions.</summary>
    /// <param name="gunItem">The gun.</param>
    internal static bool IsBoltAction(GunItem gunItem) =>
        gunItem.m_GunType == GunType.Rifle && BoltCycle.IsBoltAction(gunItem.name);

    /// <summary>
    /// Follows the rifle in the player's hands, setting its safety each time one is taken out, and handles the safety
    /// and bolt keys. Called every frame.
    /// </summary>
    /// <remarks>
    /// It reads what's in the player's hands every frame rather than hooking the game's equip method, so it follows the
    /// rifle however it got there: equipped, loaded with a save, or given back after an interaction, and whenever the
    /// game finishes putting it in the player's hands.
    /// </remarks>
    internal static void Update()
    {
        var playerManager = GameManager.GetPlayerManagerComponent();
        var itemInHands = playerManager != null ? playerManager.m_ItemInHands : null;
        var gunItem = itemInHands != null ? itemInHands.m_GunItem : null;

        // Unity objects need == null, not ?., to tell when they've been destroyed.
        if (gunItem == null || !IsBoltAction(gunItem))
        {
            InHands = null;
            return;
        }

        if (gunItem != InHands)
        {
            InHands = gunItem;
            RifleSafety.Equipped();
        }

        if (!CanTakeInput())
            return;

        if (Input.GetKeyDown(Settings.Instance.m_SafetyKey))
            RifleSafety.Toggle();

        if (Input.GetKeyDown(Settings.Instance.m_CycleBoltKey))
            ManualBolt.CycleByHand(gunItem);
    }

    /// <summary>Shows a short, high-priority message on the HUD.</summary>
    /// <param name="message">The message.</param>
    internal static void ShowMessage(string message) => HUDMessage.AddMessage(message, MessageSeconds, highPriority: true);

    /// <summary>
    /// Whether the player is in control in the game world, rather than in a menu, the pause screen or the console.
    /// </summary>
    private static bool CanTakeInput() =>
        !GameManager.m_IsPaused
        && !InterfaceManager.IsOverlayActiveCached()
        && !uConsole.IsOn()
        && InputManager.HasContext(InputManager.m_CurrentContext);
}
