using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal;

internal sealed class Mod : MelonMod
{
    public static Mod Instance { get; private set; }

    private int currentSkinIndex = 0;
    private readonly Dictionary<GunType, string[]> weaponSkins = new()
    {
        { GunType.Revolver, [
                "AdaptiveArsenal.WeaponSkins.Revolver.Brown.png", "AdaptiveArsenal.WeaponSkins.Revolver.Orange.png", "AdaptiveArsenal.WeaponSkins.Revolver.Red.png"
            ]
        },
        { GunType.Rifle, [
                "AdaptiveArsenal.WeaponSkins.Rifle.Olive.png", "AdaptiveArsenal.WeaponSkins.Rifle.Noir.png", "AdaptiveArsenal.WeaponSkins.Rifle.Red.png"
            ]
        },
        { GunType.FlareGun, [
                "AdaptiveArsenal.WeaponSkins.FlareGun.Blue.png", "AdaptiveArsenal.WeaponSkins.FlareGun.Yellow.png", "AdaptiveArsenal.WeaponSkins.FlareGun.Green.png", "AdaptiveArsenal.WeaponSkins.FlareGun.Red.png"
            ]
        },
    };

    public override void OnInitializeMelon()
    {
        Instance = this;
    }

    public override void OnUpdate()
    {
        if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.T))
        {
            var gunItem = GameManager.GetPlayerManagerComponent().m_ItemInHands?.m_GunItem;
            if (gunItem == null) return;

            GunType gunType = gunItem.m_GunType;
            if (!weaponSkins.ContainsKey(gunType)) return;

            currentSkinIndex = (currentSkinIndex + 1) % weaponSkins[gunType].Length;
            string selectedSkin = weaponSkins[gunType][currentSkinIndex];

            ChangeWeaponSkin(gunItem.gameObject, selectedSkin);
            ChangeFirstPersonWeaponSkin(selectedSkin, gunType);
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
    }

    public void ChangeFirstPersonWeaponSkin(string skinResourceName, GunType gunType)
    {
        Texture2D texture = WeaponSkinLoader.LoadEmbeddedTexture(skinResourceName);
        if (texture != null)
        {
            if (gunType == GunType.Rifle)
            {
                ApplySkinToSpecificRifleMeshes(GameManager.GetVpFPSCamera().m_CurrentWeapon.m_FirstPersonWeaponShoulder.gameObject, texture);
            }
            else if (gunType == GunType.Revolver)
            {
                ApplySkinToSpecificRevolverMeshes(GameManager.GetVpFPSCamera().m_CurrentWeapon.m_FirstPersonWeaponShoulder.gameObject, texture);
            }
            else
            {
                ApplySkinToFirstPersonModel(GameManager.GetVpFPSCamera().m_CurrentWeapon.m_FirstPersonWeaponRightHand.gameObject, texture);
            }
        }
    }

    public void ApplySkinToSpecificRevolverMeshes(GameObject root, Texture2D skinTexture)
    {
        Transform gameData = root.transform.Find("FPH_Revolver_44Mag_Rig/GAME_DATA/mesh/FPH_Revolver_44Mag:OBJ_Revolver_44Mag");
        if (gameData == null) return;

        for (int i = 0; i < gameData.childCount; i++)
        {
            var child = gameData.GetChild(i);

            if (child != null)
            {
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = skinTexture;
                }
            }
        }
    }
    
    public void ApplySkinToSpecificRifleMeshes(GameObject root, Texture2D skinTexture)
    {
        Transform gameData = root.transform.Find("GAME_DATA/Meshes");
        if (gameData == null) return;

        string[] rifleMeshes = { "mesh_bolt", "mesh_rifle", "mesh_trigger" };

        foreach (var meshName in rifleMeshes)
        {
            Transform child = gameData.Find(meshName);
            if (child != null)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = skinTexture;
                }
            }
        }
    }

    public void ApplySkinToFirstPersonModel(GameObject root, Texture2D skinTexture)
    {
        Transform gameData = root.transform.Find("FPHAnd_FlareGun_rig/GAME_DATA/mesh");
        if (gameData == null) return;

        for (int i = 0; i < gameData.childCount; i++)
        {
            var child = gameData.GetChild(i);

            if (child != null && child.name != "mesh_Shell")
            {
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = skinTexture;
                }
            }
        }
    }
}
