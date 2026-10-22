namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Works out where a shot starts and which way it goes, the way the game does for its own shots.
/// </summary>
internal static class ProjectileUtilities
{
    /// <summary>
    /// Works out where a bullet should be fired from, and in which direction, to go where the weapon is aimed.
    /// </summary>
    /// <remarks>
    /// The first-person weapon is drawn by its own weapon camera, so its sights aren't where they appear to be in the
    /// world. This projects them onto the screen with the weapon camera, then back into the world with the main
    /// camera, and aims along the line from the rear sight to the front sight. A weapon without sights aims along its
    /// bullet emission point instead.
    /// </remarks>
    /// <param name="shooter">The weapon's shooter.</param>
    /// <param name="position">The world-space position to fire from.</param>
    /// <param name="rotation">The world-space direction to fire in.</param>
    /// <returns>
    /// <see langword="false"/> if the weapon has neither sights nor a bullet emission point, so there's nowhere to fire
    /// from.
    /// </returns>
    internal static bool CalculateProjectileTransform(vp_FPSShooter shooter, out Vector3 position, out Quaternion rotation)
    {
        var weaponCamera = shooter.m_Camera.GetWeaponCamera();
        var mainCamera = GameManager.GetMainCamera();

        Transform? frontSight = null;
        Transform? rearSight = null;

        if (shooter.m_Weapon.m_FirstPersonWeaponRightHand && shooter.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint)
        {
            shooter.BulletEmissionLocator = shooter.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint.transform;
            frontSight = shooter.m_Weapon.m_FirstPersonWeaponRightHand.m_FrontSight;
            rearSight = shooter.m_Weapon.m_FirstPersonWeaponRightHand.m_RearSight;
        }
        else if (shooter.m_Weapon.m_FirstPersonWeaponShoulder && shooter.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint)
        {
            shooter.BulletEmissionLocator = shooter.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint.transform;
            frontSight = shooter.m_Weapon.m_FirstPersonWeaponShoulder.m_FrontSight;
            rearSight = shooter.m_Weapon.m_FirstPersonWeaponShoulder.m_RearSight;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (frontSight != null && rearSight != null)
        {
            var frontSightOnScreen = weaponCamera.WorldToScreenPoint(frontSight.position);
            var rearSightOnScreen = weaponCamera.WorldToScreenPoint(rearSight.position);
            var frontSightInWorld = mainCamera.ScreenToWorldPoint(frontSightOnScreen);
            var rearSightInWorld = mainCamera.ScreenToWorldPoint(rearSightOnScreen);
            var aimDirection = Vector3.Normalize(frontSightInWorld - rearSightInWorld);
            position = PlayerManager.MaybeAdjustShotPositionForNearShot(frontSight.position, frontSightInWorld, aimDirection);
            rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            return true;
        }

        if (shooter.BulletEmissionLocator != null)
        {
            var emissionPoint = shooter.BulletEmissionLocator.transform;
            var muzzleOnScreen = weaponCamera.WorldToScreenPoint(emissionPoint.position);
            var aheadOfMuzzleOnScreen = weaponCamera.WorldToScreenPoint(emissionPoint.position + emissionPoint.forward);
            var muzzleInWorld = mainCamera.ScreenToWorldPoint(muzzleOnScreen);
            var aimDirection = Vector3.Normalize(mainCamera.ScreenToWorldPoint(aheadOfMuzzleOnScreen) - muzzleInWorld);
            position = PlayerManager.MaybeAdjustShotPositionForNearShot(emissionPoint.position, muzzleInWorld, aimDirection);
            rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Points the shooter's <see cref="vp_FPSShooter.BulletEmissionLocator"/> at the bullet emission point of the
    /// weapon in the right hand, or failing that, the one on the shoulder.
    /// </summary>
    /// <param name="shooter">The weapon's shooter.</param>
    internal static void SetBulletEmissionLocator(vp_FPSShooter shooter)
    {
        if (shooter.m_Weapon.m_FirstPersonWeaponRightHand && shooter.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint)
        {
            shooter.BulletEmissionLocator = shooter.m_Weapon.m_FirstPersonWeaponRightHand.m_BulletEmissionPoint.transform;
        }
        else if (shooter.m_Weapon.m_FirstPersonWeaponShoulder && shooter.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint)
        {
            shooter.BulletEmissionLocator = shooter.m_Weapon.m_FirstPersonWeaponShoulder.m_BulletEmissionPoint.transform;
        }
    }
}
