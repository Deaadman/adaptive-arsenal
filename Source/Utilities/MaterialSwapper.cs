namespace AdaptiveArsenal.Utilities;

internal static class MaterialSwapper
{
    internal static Material? GetLineRendererMaterialFromGearItemPrefab(string gearItemName, string childName)
    {
        var childTransform = GearItem.LoadGearItemPrefab(gearItemName).transform.Find(childName);
        if (childTransform == null) return null;
        
        var lineRenderer = childTransform.GetComponent<LineRenderer>();
        return lineRenderer != null ? lineRenderer.material : null;
    }
}