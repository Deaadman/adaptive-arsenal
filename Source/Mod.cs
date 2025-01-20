using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal;

internal sealed class Mod : MelonMod
{
    public override void OnUpdate()
    {
        if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.T))
        {
            // Static GearItem
            ChangeWeaponSkin(GameManager.GetPlayerManagerComponent().m_ItemInHands.m_GunItem.gameObject, "AdaptiveArsenal.WeaponSkins.FlareGun.FlareGun_Yellow.png");

            // First-Person View Model
            GameObject firstPersonWeapon = GameManager.GetVpFPSCamera().m_CurrentWeapon.m_FirstPersonWeaponRightHand.gameObject;
            ChangeFirstPersonWeaponSkin(firstPersonWeapon, "AdaptiveArsenal.WeaponSkins.FlareGun.FlareGun_Yellow.png");
        }
    }

    public void ChangeWeaponSkin(GameObject weapon, string skinResourceName)
    {
        Texture2D texture = WeaponSkinLoader.LoadEmbeddedTexture(skinResourceName);
        if (texture != null)
        {
            ApplySkin(weapon, texture);
        }
    }

    public void ApplySkin(GameObject weapon, Texture2D skinTexture)
    {
        Renderer renderer = weapon.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.mainTexture = skinTexture;
        }
        else
        {
            Logging.LogError("Weapon does not have a Renderer component.");
        }
    }

    public void ChangeFirstPersonWeaponSkin(GameObject weapon, string skinResourceName)
    {
        Texture2D texture = WeaponSkinLoader.LoadEmbeddedTexture(skinResourceName);
        if (texture != null)
        {
            ApplySkinToFirstPersonModel(weapon, texture);
        }
    }

    public void ApplySkinToFirstPersonModel(GameObject root, Texture2D skinTexture)
    {
        // Locate the 'GAME_DATA' object and its 'mesh' child
        Transform gameData = root.transform.Find("FPHAnd_FlareGun_rig/GAME_DATA/mesh");
        if (gameData == null)
        {
            Logging.LogError("'GAME_DATA/mesh' not found in first-person weapon model.");
            return;
        }

        // Iterate through all child objects under 'mesh'
        for (int i = 0; i < gameData.childCount; i++)
        {
            var child = gameData.GetChild(i); // Get the child Transform

            if (child != null)
            {
                // Skip the mesh named 'mesh_Shell'
                if (child.name == "mesh_Shell")
                {
                    Logging.Log($"Skipped applying texture to {child.name}");
                    continue;
                }

                var renderer = child.GetComponent<Renderer>(); // Use Il2CppRenderer instead of Renderer
                if (renderer != null)
                {
                    renderer.material.mainTexture = skinTexture;
                    Logging.Log($"Applied texture to {child.gameObject.name}"); // Ensure name is resolved properly
                }
                else
                {
                    Logging.LogWarning($"No renderer found for {child.gameObject.name}");
                }
            }
        }
    }
}
