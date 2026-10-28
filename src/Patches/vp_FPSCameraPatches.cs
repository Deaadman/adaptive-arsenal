using AdaptiveArsenal.Components;

namespace AdaptiveArsenal.Patches;

/// <summary>
/// Patches on <see cref="vp_FPSCamera"/>, which shows the first-person model of the item in the player's hands.
/// </summary>
internal static class vp_FPSCameraPatches
{
    /// <summary>
    /// Puts <see cref="LeftHandedView"/> on the weapon camera, which mirrors the first-person view when the player is
    /// left-handed. It's also tried on each equip, in case the weapon camera wasn't there when the camera started.
    /// </summary>
    [HarmonyPatch(typeof(vp_FPSCamera), nameof(vp_FPSCamera.Start))]
    [HarmonyPatch(typeof(vp_FPSCamera), nameof(vp_FPSCamera.SetWeapon))]
    private static class AttachLeftHandedView
    {
        private static void Postfix(vp_FPSCamera __instance) => LeftHandedView.AttachTo(__instance.GetWeaponCamera());
    }

    /// <summary>
    /// Keeps the field of view unzoomed while aiming a rifle, when <see cref="Settings.m_RifleAimZoom"/> is off: both the
    /// world's, and the weapon camera's, which otherwise zooms in on the rifle's sights.
    /// </summary>
    /// <remarks>
    /// <c>UpdateZoom</c> blends the field of view towards the zoomed one as the player aims, in the camera's fixed
    /// update, and <see cref="vp_FPSWeapon"/> does the same for the weapon camera. They and the methods that call it are compiled into each other, so patches on them never run. Instead the
    /// field of view is put back in <c>LateUpdate</c>, which Unity calls every frame, after the fixed update and before
    /// the frame is drawn. The unzoomed field of view is read from its field, not <c>GetUnzoomedFieldOfView</c>, whose
    /// compiled body is shared with other getters, so which field it returns isn't certain.
    /// </remarks>
    [HarmonyPatch(typeof(vp_FPSCamera), nameof(vp_FPSCamera.LateUpdate))]
    private static class KeepRifleAimUnzoomed
    {
        /// <summary>Whether the field of view was kept unzoomed last time, so it's only logged when that starts.</summary>
        private static bool s_Keeping;

        /// <summary>TEMPORARY: the last state logged by <see cref="LogState"/>, to find why the patch doesn't work.</summary>
        private static string? s_LastState;

        private static void Postfix(vp_FPSCamera __instance)
        {
            LogState(__instance);

            var keep = !Settings.Instance.m_RifleAimZoom && __instance.m_Camera != null && IsRifleInHands();
            if (keep)
            {
                var unzoomed = __instance.m_UnzoomedFieldOfView;
                if (!s_Keeping)
                    Mod.Logger.Log($"Keeping the field of view at {unzoomed} while aiming a rifle, instead of {__instance.m_Camera!.fieldOfView}.", FlaggedLoggingLevel.Debug);

                Utils.SetCameraFOVSafe(__instance.m_Camera, unzoomed);

                var weaponCamera = __instance.GetWeaponCamera();
                if (weaponCamera != null && __instance.CurrentWeapon != null)
                    Utils.SetCameraFOVSafe(weaponCamera, __instance.CurrentWeapon.RenderingFieldOfViewVertical);
            }

            s_Keeping = keep;
        }

        /// <summary>TEMPORARY: logs what the patch sees whenever it changes, to find why it doesn't work.</summary>
        /// <param name="camera">The camera the patch ran on.</param>
        private static void LogState(vp_FPSCamera camera)
        {
            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            var item = itemInHands == null ? "nothing" : itemInHands.name;
            var gunType = itemInHands == null || itemInHands.m_GunItem == null ? "no gun" : itemInHands.m_GunItem.m_GunType.ToString();
            var fov = camera.m_Camera == null ? "no camera" : Math.Round(camera.m_Camera.fieldOfView).ToString();
            var state = $"zoom setting {Settings.Instance.m_RifleAimZoom}, holding {item} ({gunType}), camera FOV {fov}, unzoomed FOV {camera.m_UnzoomedFieldOfView}, zoomed {camera.IsZoomed}, weapon FOV {WeaponFov(camera)}";
            if (state == s_LastState)
                return;

            s_LastState = state;
            Mod.Logger.Log($"[Zoom test] {state}", FlaggedLoggingLevel.Debug);
        }

        /// <summary>TEMPORARY: the weapon camera's field of view and the weapon's unzoomed one, for <see cref="LogState"/>.</summary>
        /// <param name="camera">The camera the patch ran on.</param>
        /// <returns>The two fields of view, rounded.</returns>
        private static string WeaponFov(vp_FPSCamera camera)
        {
            var weaponCamera = camera.GetWeaponCamera();
            var current = weaponCamera == null ? "no camera" : Math.Round(weaponCamera.fieldOfView).ToString();
            var unzoomed = camera.CurrentWeapon == null ? "no weapon" : Math.Round(camera.CurrentWeapon.RenderingFieldOfViewVertical).ToString();
            return $"{current} (unzoomed {unzoomed})";
        }

        /// <summary>Whether the player is holding a rifle.</summary>
        private static bool IsRifleInHands()
        {
            var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
            return itemInHands != null && itemInHands.m_GunItem != null && itemInHands.m_GunItem.m_GunType == GunType.Rifle;
        }
    }
}
