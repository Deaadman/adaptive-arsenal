namespace AdaptiveArsenal.Utilities;

public class WeaponSkinLoader
{
    public static Texture2D LoadEmbeddedTexture(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                Logging.LogError($"Resource not found: {resourceName}");
                return null;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            Texture2D texture = new Texture2D(2, 2); // Initialize with default dimensions
            if (texture.LoadImage(buffer))
            {
                return texture;
            }
            else
            {
                Logging.LogError($"Failed to load texture: {resourceName}");
                return null;
            }
        }
    }
}