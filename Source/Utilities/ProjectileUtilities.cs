namespace AdaptiveArsenal.Utilities;

internal static class ProjectileUtilities
{
    /// <summary>
    /// Calculates the position and rotation for a projectile based on the weapon's configuration and aiming setup.
    /// </summary>
    /// <remarks>This method determines the projectile's transform by evaluating the weapon's bullet emission
    /// point, front and rear sights, and the associated weapon and main cameras. If both front and rear sights are
    /// available, the method calculates the transform based on their alignment. Otherwise, it uses the bullet emission
    /// point as the reference.</remarks>
    /// <param name="instance">The instance of the <see cref="vp_FPSShooter"/> containing weapon and camera information.</param>
    /// <param name="position">When this method returns, contains the calculated world-space position for the projectile.</param>
    /// <param name="rotation">When this method returns, contains the calculated world-space rotation for the projectile.</param>
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

        // TODO Consider renaming these variables to more descriptive names, Dragons be here.

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
        else if (instance.BulletEmissionLocator != null)
        {
            Vector3 vector8 = weaponCamera.WorldToScreenPoint(instance.BulletEmissionLocator.transform.position);
            Vector3 vector9 = weaponCamera.WorldToScreenPoint(instance.BulletEmissionLocator.transform.position + instance.BulletEmissionLocator.transform.forward);
            Vector3 vector10 = mainCamera.ScreenToWorldPoint(vector8);
            Vector3 vector11 = Vector3.Normalize(mainCamera.ScreenToWorldPoint(vector9) - vector10);
            position = PlayerManager.MaybeAdjustShotPositionForNearShot(instance.BulletEmissionLocator.transform.position, vector10, vector11);
            rotation = Quaternion.LookRotation(vector11, Vector3.up);
        }
    }
    
    /// <summary>
    /// Sets the bullet emission locator for the specified shooter instance based on the available weapon configuration.
    /// </summary>
    /// <remarks>The method determines the bullet emission point by checking the weapon's first-person
    /// right-hand  or shoulder configuration. </remarks>
    /// <param name="instance">The shooter instance for which the bullet emission locator is to be set.  This parameter cannot be <see
    /// langword="null"/>.</param>
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