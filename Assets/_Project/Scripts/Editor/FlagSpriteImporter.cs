#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace JuegoTCG.Editor
{
    [InitializeOnLoad]
    public static class FlagSpriteImporter
    {
        [InitializeOnLoadMethod]
        [MenuItem("JuegoTCG/Configurar Banderas como Sprites", priority = 51)]
        public static void ConfigureAllFlagsAsSprites()
        {
            string flagsFolder = "Assets/Resources/Flags";
            if (!Directory.Exists(flagsFolder)) return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { flagsFolder });
            int configured = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool modified = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    modified = true;
                }

                if (importer.alphaIsTransparency != true)
                {
                    importer.alphaIsTransparency = true;
                    modified = true;
                }

                if (importer.mipmapEnabled != false)
                {
                    importer.mipmapEnabled = false;
                    modified = true;
                }

                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    modified = true;
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    configured++;
                }
            }

            if (configured > 0)
            {
                Debug.Log($"[FlagSpriteImporter] {configured} banderas configuradas exitosamente como Sprites UI.");
            }

            // Reconstruir automáticamente el CardPrefab oficial para tener las banderas y stats al día
            JuegoTCG.EditorTools.CardPrefabBuilder.BuildCardPrefab();
            JuegoTCG.EditorTools.ResourcesSetupUtility.ExecuteSetup(silent: true);
        }
    }
}
#endif
