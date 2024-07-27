using AdaptiveArsenal.Animators;

namespace AdaptiveArsenal.Patches;

internal static class EquipItemPopupPatches
{
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.Awake))]
    private static class AttachAmmoSpriteAnimatorComponent
    {
        private static void Postfix(EquipItemPopup __instance)
        {
            _ = __instance.GetComponent<AmmoSpriteAnimator>() ?? __instance.gameObject.AddComponent<AmmoSpriteAnimator>();
        }
    }
    
    [HarmonyPatch(typeof(EquipItemPopup), nameof(EquipItemPopup.UpdateAmmoStatus))]
    public static class UpdateAmmoStatusPatch
    {
        private static int lastRoundsInClip = -1;
    
        private static void Postfix(EquipItemPopup __instance)
        {
            var ammoSpriteAnimator = __instance.GetComponent<AmmoSpriteAnimator>();
            
            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            if (itemInHands == null || itemInHands.m_GunItem == null || itemInHands.m_GunItem.m_GunType == GunType.FlareGun) return;
            
            var roundsInClip = itemInHands.m_GunItem.NumRoundsInClip();
            if (roundsInClip < itemInHands.m_GunItem.m_ClipSize && roundsInClip < __instance.m_ListAmmoSprites.Length)
            {
                if (roundsInClip < lastRoundsInClip)
                {
                    var startPosition = __instance.m_ListAmmoSprites[roundsInClip].transform.localPosition;
                    MelonCoroutines.Start(ammoSpriteAnimator.CasingEjectionAnimation(startPosition, __instance.m_ListAmmoSprites[0].transform.parent, itemInHands.m_GunItem.m_GunType));
                }
            }
        
            lastRoundsInClip = roundsInClip;
        }
    }
}