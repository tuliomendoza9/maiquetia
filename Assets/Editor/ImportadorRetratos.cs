using UnityEditor;

/// <summary>Todo PNG bajo Resources/Retratos se importa como Sprite 2D.</summary>
public class ImportadorRetratos : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Retratos")) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 512;
    }
}
