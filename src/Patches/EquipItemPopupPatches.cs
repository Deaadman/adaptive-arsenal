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
    /// Hides the rounds loaded in the rifle or revolver in the player's hands, unless
    /// <see cref="Settings.m_ShowAmmoCount"/> is on, so the player has to keep count.
    /// </summary>
    /// <remarks>
    /// The game sets up the round sprites every time it updates the panel, so they're hidden again after each update,
    /// and come back by themselves when the setting is turned on. The ammo type and the rounds in the player's inventory
    /// still show. Other weapons, like the flare gun and bow, are left as they are.
    /// </remarks>
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.UpdateAmmoStatus))]
    private static class HideLoadedRounds
    {
        private static void Postfix(EquipItemPopup __instance)
        {
            if (Settings.Instance.m_ShowAmmoCount)
                return;

            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            if (itemInHands == null || itemInHands.m_GunItem == null)
                return;

            var gunType = itemInHands.m_GunItem.m_GunType;
            if (gunType != GunType.Rifle && gunType != GunType.Revolver)
                return;

            foreach (var roundSprite in __instance.m_ListAmmoSprites)
            {
                if (roundSprite != null)
                    roundSprite.gameObject.SetActive(false);
            }
        }
    }
}
