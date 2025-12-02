using AdaptiveArsenal.Animators;

namespace AdaptiveArsenal.Patches
{
    internal static class EquipItemPopupPatches
    {
        /// <summary>
        /// A Harmony patch class that ensures an <see cref="AmmoSpriteAnimator"/> component is attached to the <see
        /// cref="EquipItemPopup"/> GameObject during its initialization.
        /// </summary>
        /// <remarks>This patch is applied to the <see cref="EquipItemPopup.Awake"/> method. If the <see
        /// cref="AmmoSpriteAnimator"/> component is not already present on the GameObject, it will be added
        /// automatically.</remarks>
        [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.Awake))]
        private static class AttachAmmoSpriteAnimatorComponent
        {
            private static void Postfix(EquipItemPopup __instance) => _ = __instance.GetComponent<AmmoSpriteAnimator>() ?? __instance.gameObject.AddComponent<AmmoSpriteAnimator>();
        }

        /// <summary>
        /// Provides a Harmony patch for the <see cref="EquipItemPopup.UpdateAmmoStatus"/> method to enhance the
        /// behavior of ammo status updates.
        /// </summary>
        /// <remarks>This patch modifies the behavior of the <see cref="EquipItemPopup.UpdateAmmoStatus"/>
        /// method to include additional logic for handling ammo sprite animations </remarks>
        [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.UpdateAmmoStatus))]
        public static class UpdateAmmoStatusPatch
        {
            private static int lastRoundsInClip = -1;
            private static GunItem? lastGunItem;

            private static void Postfix(EquipItemPopup __instance)
            {
                AmmoSpriteAnimator ammoSpriteAnimator = __instance.GetComponent<AmmoSpriteAnimator>();

                GearItem itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;

                if (itemInHands == null ||
                    itemInHands.m_GunItem == null ||
                    itemInHands.m_GunItem.m_GunType == GunType.FlareGun)
                {
                    lastGunItem = null;
                    lastRoundsInClip = -1;
                    return;
                }

                var currentGunItem = itemInHands.m_GunItem;
                var roundsInClip = currentGunItem.NumRoundsInClip();

                if (currentGunItem != lastGunItem)
                {
                    lastGunItem = currentGunItem;
                    lastRoundsInClip = roundsInClip;
                    return;
                }

                // TODO Should merge with the nested condition
                if (roundsInClip < currentGunItem.m_ClipSize && roundsInClip < __instance.m_ListAmmoSprites.Length)
                {
                    if (roundsInClip < lastRoundsInClip)
                    {
                        var startPosition = __instance.m_ListAmmoSprites[roundsInClip].transform.localPosition;
                        MelonCoroutines.Start(ammoSpriteAnimator.CasingEjectionAnimation(startPosition, __instance.m_ListAmmoSprites[0].transform.parent, currentGunItem.m_GunType));
                    }
                }

                lastRoundsInClip = roundsInClip;
            }
        }
    }
}