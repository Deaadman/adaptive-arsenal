namespace AdaptiveArsenal;

class GunFireCustomProjectile
{
    [HarmonyPatch(typeof(vp_FPSShooter), nameof(vp_FPSShooter.Fire))]
    static class FireCustomProjectile
    {
        static void Prefix(vp_FPSShooter __instance)
        {
            if (Time.time < __instance.m_NextAllowedFireTime || __instance.m_Weapon.ReloadInProgress() || !GameManager.GetPlayerAnimationComponent().IsAllowedToFire(__instance.m_Weapon.m_GunItem.m_AllowHipFire) || GameManager.GetPlayerAnimationComponent().IsReloading() || __instance.m_Weapon.GetAmmoCount() < 1)
            {
                return;
            }

            SetBulletEmissionLocator(__instance);

            if (__instance.ProjectilePrefab.GetComponent<AmmoProjectile>())
            {
                CalculateProjectileTransform(__instance, out Vector3 position, out Quaternion rotation);
                AmmoProjectile.SpawnAndFire(__instance.ProjectilePrefab, position, rotation);
            }
        }

        static void SetBulletEmissionLocator(vp_FPSShooter __instance)
        {
            if (__instance.m_Weapon.m_FirstPersonWeaponRightHand && __instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint)
            {
                __instance.BulletEmissionLocator = __instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint.transform;
            }
            else if (__instance.m_Weapon.m_FirstPersonWeaponShoulder && __instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint)
            {
                __instance.BulletEmissionLocator = __instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint.transform;
            }
        }

        static void CalculateProjectileTransform(vp_FPSShooter __instance, out Vector3 position, out Quaternion rotation)
        {
            Camera weaponCamera = __instance.m_Camera.GetWeaponCamera();
            Camera mainCamera = GameManager.GetMainCamera();

            Transform transform = null;
            Transform transform2 = null;

            if (__instance.m_Weapon.m_FirstPersonWeaponRightHand && __instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint)
            {
                __instance.BulletEmissionLocator = __instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint.transform;
                transform = __instance.m_Weapon.m_FirstPersonWeaponRightHand.m_FrontSight;
                transform2 = __instance.m_Weapon.m_FirstPersonWeaponRightHand.m_RearSight;
            }
            else if (__instance.m_Weapon.m_FirstPersonWeaponShoulder && __instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint)
            {
                __instance.BulletEmissionLocator = __instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint.transform;
                transform = __instance.m_Weapon.m_FirstPersonWeaponShoulder.m_FrontSight;
                transform2 = __instance.m_Weapon.m_FirstPersonWeaponShoulder.m_RearSight;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (transform != null && transform2 != null)
            {
                Vector3 vector2 = weaponCamera.WorldToScreenPoint(transform.position);
                Vector3 vector3 = weaponCamera.WorldToScreenPoint(transform2.position);
                Vector3 vector4 = mainCamera.ScreenToWorldPoint(vector2);
                Vector3 vector5 = mainCamera.ScreenToWorldPoint(vector3);
                Vector3 vector6 = Vector3.Normalize(vector4 - vector5);
                position = PlayerManager.MaybeAdjustShotPositionForNearShot(transform.position, vector4, vector6);
                rotation = Quaternion.LookRotation(vector6, Vector3.up);
            }
            else if (__instance.BulletEmissionLocator != null)
            {
                Vector3 vector8 = weaponCamera.WorldToScreenPoint(__instance.BulletEmissionLocator.transform.position);
                Vector3 vector9 = weaponCamera.WorldToScreenPoint(__instance.BulletEmissionLocator.transform.position + __instance.BulletEmissionLocator.transform.forward);
                Vector3 vector10 = mainCamera.ScreenToWorldPoint(vector8);
                Vector3 vector11 = Vector3.Normalize(mainCamera.ScreenToWorldPoint(vector9) - vector10);
                position = PlayerManager.MaybeAdjustShotPositionForNearShot(__instance.BulletEmissionLocator.transform.position, vector10, vector11);
                rotation = Quaternion.LookRotation(vector11, Vector3.up);
            }
        }
    }
}