using AdaptiveArsenal.Animators;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="EquipItemPopup"/>, the HUD panel that shows the rounds in the weapon in the player's hands,
/// to play <see cref="AmmoSpriteAnimator"/>'s animation when a round is used.
/// </summary>
internal static class EquipItemPopupPatches
{
    /// <summary>Adds an <see cref="AmmoSpriteAnimator"/> to the panel when it's created.</summary>
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.Awake))]
    private static class AddAmmoSpriteAnimator
    {
        private static void Postfix(EquipItemPopup __instance) =>
            _ = __instance.GetComponent<AmmoSpriteAnimator>() ?? __instance.gameObject.AddComponent<AmmoSpriteAnimator>();
    }

    /// <summary>
    /// Plays the ejection animation whenever the rifle or revolver in the player's hands has one fewer round than the
    /// last time the panel updated.
    /// </summary>
    /// <remarks>
    /// Switching weapons only resets the count, so equipping a gun with fewer rounds doesn't play the animation. The
    /// flare gun is left out.
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

            if (roundWasUsed && slotIsShown)
            {
                var startPosition = __instance.m_ListAmmoSprites[roundsInClip].transform.localPosition;
                var spriteParent = __instance.m_ListAmmoSprites[0].transform.parent;
                MelonCoroutines.Start(ammoSpriteAnimator.AnimateCasingEjection(startPosition, spriteParent, gunItem.m_GunType));
            }

            s_LastRoundsInClip = roundsInClip;
        }
    }
}
