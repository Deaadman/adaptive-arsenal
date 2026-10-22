namespace AdaptiveArsenal.Components;

/// <summary>
/// Swaps the texture on a weapon's model for a skin's, and back. It goes on a weapon's <see cref="GearItem"/>, and on
/// each first-person model of it, which every weapon of the same kind shares.
/// </summary>
/// <remarks>
/// <para>
/// The skin replaces the main texture of every material on the renderers it's given. The first time it does, each
/// renderer gets its own copies of its materials, so other weapons using the same materials keep their look, and the
/// original textures are kept to put back.
/// </para>
/// <para>
/// <see cref="Utilities.WeaponSkins"/> decides which skin each weapon wears and calls <see cref="Apply"/>.
/// </para>
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class WeaponSkin : MonoBehaviour
{
    /// <summary>The materials the skin is drawn on, each with its own texture to put back.</summary>
    private readonly List<(Material Material, Texture? Original)> m_Materials = [];

    /// <summary>The skin's texture on the model now, or <see langword="null"/> for its own look.</summary>
    private Texture2D? m_Texture;

    /// <summary>Whether <see cref="m_Materials"/> has been filled in.</summary>
    private bool m_HasMaterials;

    /// <summary>Draws a skin's texture on the model, or puts the model's own textures back.</summary>
    /// <param name="renderers">The model's renderers, used the first time it's called.</param>
    /// <param name="texture">The skin's texture, or <see langword="null"/> for the model's own look.</param>
    internal void Apply(IEnumerable<Renderer> renderers, Texture2D? texture)
    {
        // Nothing to undo, and nothing to copy the materials for.
        if (!m_HasMaterials && texture == null)
            return;

        if (!m_HasMaterials)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                // Reading Renderer.materials gives the renderer its own copies of its materials.
                foreach (var material in renderer.materials)
                {
                    if (material != null)
                        m_Materials.Add((material, material.mainTexture));
                }
            }

            m_HasMaterials = true;
        }

        if (texture == m_Texture)
            return;

        foreach (var (material, original) in m_Materials)
        {
            if (material != null)
                material.mainTexture = texture == null ? original : texture;
        }

        m_Texture = texture;
    }
}
