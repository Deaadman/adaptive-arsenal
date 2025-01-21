using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal;

internal sealed class Mod : MelonMod
{
    public static Mod Instance { get; private set; }
    
    private int currentSkinIndex = 0;
    private readonly string[] weaponSkins = 
    {
        "AdaptiveArsenal.WeaponSkins.FlareGun.Blue.png",
        "AdaptiveArsenal.WeaponSkins.FlareGun.Yellow.png",
        "AdaptiveArsenal.WeaponSkins.FlareGun.Green.png",
        "AdaptiveArsenal.WeaponSkins.FlareGun.Red.png"
    };

    public override void OnInitializeMelon()
    {
        Instance = this;
    }
    
    public override void OnUpdate()
    {
        if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.T))
        {
            currentSkinIndex = (currentSkinIndex + 1) % weaponSkins.Length;

            string selectedSkin = weaponSkins[currentSkinIndex];
            ChangeWeaponSkin(GameManager.GetPlayerManagerComponent().m_ItemInHands.gameObject, selectedSkin);

            GameObject firstPersonWeapon = GameManager.GetVpFPSCamera().m_CurrentWeapon.m_FirstPersonWeaponRightHand.gameObject;
            ChangeFirstPersonWeaponSkin(firstPersonWeapon, selectedSkin);
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

    [HarmonyPatch(typeof(Utils), nameof(Utils.GetInventoryIconTexture), typeof(GearItem))]
    private static class GenericIconTextureSwap
    {
        private static bool Prefix(GearItem gi, ref Texture2D __result)
        {
            if (gi.name == "GEAR_FlareGun" && InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.T))
            {
                string iconResourceName = $"AdaptiveArsenal.WeaponSkins.FlareGun.Icons.{GetCurrentColor()}.png";
                Texture2D customIcon = WeaponSkinLoader.LoadEmbeddedTexture(iconResourceName);
                if (customIcon != null)
                {
                    __result = customIcon;
                    return false;
                }
            }
            return true;
        }

        private static string GetCurrentColor()
        {
            switch (Mod.Instance.currentSkinIndex)
            {
                case 0: return "Blue";
                case 1: return "Yellow";
                case 2: return "Green";
                case 3: return "Red";
                default: return "Blue";
            }
        }
    }
}
