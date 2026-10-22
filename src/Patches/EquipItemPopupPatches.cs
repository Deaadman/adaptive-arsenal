using AdaptiveArsenal.Animators;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="EquipItemPopup"/>, the HUD panel that shows the rounds in the weapon in the player's hands,
/// to hide them unless <see cref="Settings.m_ShowAmmoCount"/> is on, and to play <see cref="AmmoSpriteAnimator"/>'s
/// animation when a round is used.
/// </summary>
internal static class EquipItemPopupPatches
{
    /// <summary>Adds an <see cref="AmmoSpriteAnimator"/> to the panel when it's created.</summary>
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.Awake))]
    private static class AddAmmoSpriteAnimator
    {
        private static void Postfix(EquipItemPopup __instance)
        {
            // Unity objects need == null, not ??, to tell when they've been destroyed.
            if (__instance.GetComponent<AmmoSpriteAnimator>() == null)
                __instance.gameObject.AddComponent<AmmoSpriteAnimator>();
        }
    }

    /// <summary>
    /// Plays the ejection animation whenever the rifle or revolver in the player's hands has one fewer round than the
    /// last time the panel updated.
    /// </summary>
    /// <remarks>
    /// Switching weapons only resets the count, so equipping a gun with fewer rounds doesn't play the animation. The
    /// flare gun is left out, and so is everything while the rounds are hidden, since the animation would count them.
    /// </remarks>
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.UpdateAmmoStatus))]
    private static class EjectRoundOnShot
    {
        /// <summary>The rounds in <see cref="s_LastGunItem"/> at the last update, or -1 when there's no gun.</summary>
        private static int s_LastRoundsInClip = -1;

        /// <summary>The gun in the player's hands at the last update.</summary>
        private static GunItem? s_LastGunItem;

        private static void Postfix(EquipItemPopup __instance)
        {
            var ammoSpriteAnimator = __instance.GetComponent<AmmoSpriteAnimator>();

            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            if (itemInHands == null || itemInHands.m_GunItem == null || itemInHands.m_GunItem.m_GunType == GunType.FlareGun)
            {
                s_LastGunItem = null;
                s_LastRoundsInClip = -1;
                return;
            }

            var gunItem = itemInHands.m_GunItem;
            var roundsInClip = gunItem.NumRoundsInClip();

            if (gunItem != s_LastGunItem)
            {
                s_LastGunItem = gunItem;
                s_LastRoundsInClip = roundsInClip;
                return;
            }

            // The used round's slot is the one at the new count, as long as the panel has a sprite for it.
            var roundWasUsed = roundsInClip < s_LastRoundsInClip;
            var slotIsShown = roundsInClip < gunItem.m_ClipSize && roundsInClip < __instance.m_ListAmmoSprites.Length;

            if (roundWasUsed && slotIsShown && Settings.Instance.m_ShowAmmoCount)
            {
                var startPosition = __instance.m_ListAmmoSprites[roundsInClip].transform.localPosition;
                var spriteParent = __instance.m_ListAmmoSprites[0].transform.parent;
                MelonCoroutines.Start(ammoSpriteAnimator.AnimateCasingEjection(startPosition, spriteParent, gunItem.m_GunType));
            }

            s_LastRoundsInClip = roundsInClip;
        }
    }

    /// <summary>
    /// Hides the rounds loaded in the rifle or revolver in the player's hands, and how many more the player is
    /// carrying, unless <see cref="Settings.m_ShowAmmoCount"/> is on, so the player has to keep count.
    /// </summary>
    /// <remarks>
    /// The round sprites are made see-through rather than turned off, since the game doesn't turn them back on each
    /// time it updates the panel. They get their own alpha back when the setting is turned on, or for any other
    /// weapon, like the flare gun, which shares them. The carried count is emptied after each update, and since the
    /// game only sets its text when the count changes, it's told to set it again once the count shows. The icon for
    /// the type of ammo still shows.
    /// </remarks>
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.UpdateAmmoStatus))]
    private static class HideAmmoCount
    {
        /// <summary>The alpha of each round sprite made see-through, by its pointer, to put back.</summary>
        private static readonly Dictionary<IntPtr, float> s_HiddenSpriteAlphas = [];

        /// <summary>Whether the carried count was emptied, so it needs the game to set it again.</summary>
        private static bool s_ReserveEmptied;

        private static void Postfix(EquipItemPopup __instance)
        {
            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            var gunType = itemInHands != null && itemInHands.m_GunItem != null ? itemInHands.m_GunItem.m_GunType : (GunType?)null;
            var hide = !Settings.Instance.m_ShowAmmoCount && gunType is GunType.Rifle or GunType.Revolver;

            if (!hide && s_HiddenSpriteAlphas.Count == 0 && !s_ReserveEmptied)
                return;

            foreach (var roundSprite in __instance.m_ListAmmoSprites)
            {
                if (roundSprite == null)
                    continue;

                if (hide)
                {
                    s_HiddenSpriteAlphas.TryAdd(roundSprite.Pointer, roundSprite.alpha);
                    roundSprite.alpha = 0f;
                }
                else if (s_HiddenSpriteAlphas.Remove(roundSprite.Pointer, out var alpha))
                {
                    roundSprite.alpha = alpha;
                }
            }

            if (hide && __instance.m_LabelAmmoReserve != null)
            {
                __instance.m_LabelAmmoReserve.text = string.Empty;
                s_ReserveEmptied = true;
            }
            else if (!hide && s_ReserveEmptied)
            {
                // The game only sets the carried count when it changes from this, so a count it can't be sets it again.
                __instance.m_LastReserveValue = -1;
                s_ReserveEmptied = false;
            }
        }
    }
}
