using AdaptiveArsenal.Animators;

namespace AdaptiveArsenal.Patches;

internal static class EquipItemPopupPatches
{
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.Awake))]
    private static class AttachAmmoSpriteAnimatorComponent
    {
        private static void Postfix(EquipItemPopup __instance) => _ = __instance.GetComponent<AmmoSpriteAnimator>() ?? __instance.gameObject.AddComponent<AmmoSpriteAnimator>();
    }
    
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.UpdateAmmoStatus))]
    public static class UpdateAmmoStatusPatch
    {
        private static int lastRoundsInClip = -1;
        private static GunItem? lastGunItem;
    
        private static void Postfix(EquipItemPopup __instance)
        {
            var ammoSpriteAnimator = __instance.GetComponent<AmmoSpriteAnimator>();
            
            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            if (itemInHands == null || itemInHands.m_GunItem == null || itemInHands.m_GunItem.m_GunType == GunType.FlareGun) 
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