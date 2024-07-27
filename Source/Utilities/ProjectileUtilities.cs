namespace AdaptiveArsenal.Utilities;

internal static class ProjectileUtilities
{
    internal static void CalculateProjectileTransform(vp_FPSShooter instance, out Vector3 position, out Quaternion rotation)
    {
        var weaponCamera = instance.m_Camera.GetWeaponCamera();
        var mainCamera = GameManager.GetMainCamera();

        Transform? transform = null;
        Transform? transform2 = null;

        if (instance.m_Weapon.m_FirstPersonWeaponRightHand && instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint)
        {
            instance.BulletEmissionLocator = instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint.transform;
            transform = instance.m_Weapon.m_FirstPersonWeaponRightHand.m_FrontSight;
            transform2 = instance.m_Weapon.m_FirstPersonWeaponRightHand.m_RearSight;
            
        }
        else if (instance.m_Weapon.m_FirstPersonWeaponShoulder && instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint)
        {
            instance.BulletEmissionLocator = instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint.transform;
            transform = instance.m_Weapon.m_FirstPersonWeaponShoulder.m_FrontSight;
            transform2 = instance.m_Weapon.m_FirstPersonWeaponShoulder.m_RearSight;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (transform != null && transform2 != null)
        {
            var vector2 = weaponCamera.WorldToScreenPoint(transform.position);
            var vector3 = weaponCamera.WorldToScreenPoint(transform2.position);
            var vector4 = mainCamera.ScreenToWorldPoint(vector2);
            var vector5 = mainCamera.ScreenToWorldPoint(vector3);
            var vector6 = Vector3.Normalize(vector4 - vector5);
            position = PlayerManager.MaybeAdjustShotPositionForNearShot(transform.position, vector4, vector6);
            rotation = Quaternion.LookRotation(vector6, Vector3.up);
        }
        else if (instance.BulletEmissionLocator != null)
        {
            var vector8 = weaponCamera.WorldToScreenPoint(instance.BulletEmissionLocator.transform.position);
            var vector9 = weaponCamera.WorldToScreenPoint(instance.BulletEmissionLocator.transform.position + instance.BulletEmissionLocator.transform.forward);
            var vector10 = mainCamera.ScreenToWorldPoint(vector8);
            var vector11 = Vector3.Normalize(mainCamera.ScreenToWorldPoint(vector9) - vector10);
            position = PlayerManager.MaybeAdjustShotPositionForNearShot(instance.BulletEmissionLocator.transform.position, vector10, vector11);
            rotation = Quaternion.LookRotation(vector11, Vector3.up);
        }
    }
    
    internal static void SetBulletEmissionLocator(vp_FPSShooter instance)
    {
        if (instance.m_Weapon.m_FirstPersonWeaponRightHand && instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint)
        {
            instance.BulletEmissionLocator = instance.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint.transform;
        }
        else if (instance.m_Weapon.m_FirstPersonWeaponShoulder && instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint)
        {
            instance.BulletEmissionLocator = instance.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint.transform;
        }
    }
}