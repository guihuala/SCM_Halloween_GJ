using UnityEditor;

// Keep exported SVG cursors readable and uncompressed on every platform.
public sealed class CourierCursorImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/NightVoyage/Cursors/") || !assetPath.EndsWith(".png")) return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Cursor;
        texture.isReadable = true;
        texture.mipmapEnabled = false;
        texture.alphaIsTransparency = true;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    }
}
