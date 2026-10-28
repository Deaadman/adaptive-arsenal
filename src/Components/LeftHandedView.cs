namespace AdaptiveArsenal.Components;

/// <summary>
/// Mirrors what the weapon camera draws from left to right while <see cref="Settings.m_LeftHanded"/> is on, so the
/// player's hands and whatever they hold are on the left. It goes on the weapon camera, and the world camera is left
/// alone.
/// </summary>
/// <remarks>
/// <para>
/// The game sets up the weapon camera's projection every time it draws it, with its own field of view and, with
/// temporal anti-aliasing, a jittered matrix and an unjittered one for see-through things. So the mirror goes on in <see cref="OnPreCull"/>, after that, and comes
/// off in <see cref="OnPostRender"/>. Mirroring turns every triangle around, so culling is inverted while it draws, or
/// the models would be drawn inside out.
/// </para>
/// <para>
/// Outside drawing the camera isn't mirrored, so anything that projects through it must mirror the result itself:
/// <see cref="MirrorScreenPoint"/>.
/// </para>
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class LeftHandedView : MonoBehaviour
{
    /// <summary>Flips x, which mirrors the picture when it's applied after the projection.</summary>
    private static readonly Matrix4x4 s_Mirror = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));

    /// <summary>The camera this is on.</summary>
    private Camera? m_Camera;

    /// <summary>Whether this draw is mirrored, so <see cref="OnPostRender"/> knows to undo it.</summary>
    private bool m_Mirrored;

    /// <summary>Whether the first-person view is mirrored.</summary>
    internal static bool IsLeftHanded => Settings.Instance.m_LeftHanded;

    /// <summary>Puts the component on the weapon camera, unless it's there already.</summary>
    /// <param name="weaponCamera">The weapon camera, or <see langword="null"/> if it isn't there yet.</param>
    internal static void AttachTo(Camera? weaponCamera)
    {
        if (weaponCamera != null && weaponCamera.GetComponent<LeftHandedView>() == null)
            weaponCamera.gameObject.AddComponent<LeftHandedView>();
    }

    /// <summary>
    /// Mirrors a point the weapon camera projected onto the screen, so it's where the player sees it while the view is
    /// mirrored.
    /// </summary>
    /// <param name="camera">The weapon camera that projected the point.</param>
    /// <param name="screenPoint">The point, from <see cref="Camera.WorldToScreenPoint(Vector3)"/>.</param>
    /// <returns>The point mirrored across the middle of the camera's view, or as it was when right-handed.</returns>
    internal static Vector3 MirrorScreenPoint(Camera camera, Vector3 screenPoint)
    {
        if (!IsLeftHanded)
            return screenPoint;

        var rect = camera.pixelRect;
        screenPoint.x = rect.xMin + rect.xMax - screenPoint.x;
        return screenPoint;
    }

    private void Awake() => m_Camera = GetComponent<Camera>();

    private void OnPreCull()
    {
        if (!IsLeftHanded || m_Camera == null)
            return;

        // Temporal anti-aliasing draws see-through things, like a lantern's glass, with the unjittered matrix, so that's
        // mirrored too. Read before setting the projection, which it follows when the game hasn't set it apart.
        var nonJittered = m_Camera.nonJitteredProjectionMatrix;
        m_Camera.projectionMatrix = s_Mirror * m_Camera.projectionMatrix;
        m_Camera.nonJitteredProjectionMatrix = s_Mirror * nonJittered;
        m_Mirrored = true;
    }

    private void OnPreRender()
    {
        if (m_Mirrored)
            GL.invertCulling = true;
    }

    private void OnPostRender()
    {
        if (!m_Mirrored)
            return;

        GL.invertCulling = false;
        m_Mirrored = false;

        // Back to working the projection out from the field of view, which the game sets again before the next draw.
        if (m_Camera != null)
            m_Camera.ResetProjectionMatrix();
    }
}
