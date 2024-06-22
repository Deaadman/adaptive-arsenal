namespace AdaptiveArsenal.Utilities;

internal static class MaterialSwapper
{
    internal static Material? GetLineRendererMaterialFromGearItemPrefab(string gearItemName, string childName)
    {
        var gearItemPrefab = GearItem.LoadGearItemPrefab(gearItemName);
        if (gearItemPrefab == null) return null;

        var childTransform = gearItemPrefab.transform.Find(childName);
        if (childTransform == null) return null;
        var lineRenderer = childTransform.GetComponent<LineRenderer>();
        return lineRenderer != null ? lineRenderer.material : null;
    }
}