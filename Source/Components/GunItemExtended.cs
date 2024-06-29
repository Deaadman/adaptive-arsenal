namespace AdaptiveArsenal.Components;

[RegisterTypeInIl2Cpp(false)]
public class GunItemExtended : MonoBehaviour
{
    #nullable disable
    internal GunStatistics GunStats;
    #nullable enable
    
    private static readonly Dictionary<string, GunStatistics> GunData = new()
    {
        {"GEAR_Rifle_Barbs", new GunStatistics(744, 175, 325)},
        {"GEAR_Rifle_Curators", new GunStatistics(991, 200, 400)},
        {"GEAR_Rifle_Vaughns", new GunStatistics(707, 150, 300)},
        {"GEAR_Rifle", new GunStatistics(744, 150, 300)},
        {"GEAR_RevolverFancy", new GunStatistics(822, 100, 200)},
        {"GEAR_RevolverGreen", new GunStatistics(411, 75, 150)},
        {"GEAR_RevolverStubNosed", new GunStatistics(275, 50, 100)},
        {"GEAR_Revolver", new GunStatistics(411, 75, 150)}
    };

    internal class GunStatistics(int muzzleVelocity, int effectiveRange, int maxRange)
    {
        internal readonly int MuzzleVelocity = muzzleVelocity;
        internal readonly int EffectiveRange = effectiveRange;
        internal readonly int MaxRange = maxRange;
    }

    private void Awake()
    {
        var gearItemGoName = GetComponent<GearItem>().gameObject.name;
        GunStats = GunData.FirstOrDefault(kv => gearItemGoName.Contains(kv.Key)).Value;
    }
}