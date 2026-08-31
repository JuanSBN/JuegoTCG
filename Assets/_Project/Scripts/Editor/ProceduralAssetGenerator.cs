#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JuegoTCG.EditorTools
{
    public static class ProceduralAssetGenerator
    {
        private const string UIPath = "Assets/_Project/Art/UI";

        [MenuItem("JuegoTCG/Generar Sprites Procedurales de UI")]
        public static void GenerateUISprites()
        {
            if (!Directory.Exists(UIPath))
            {
                Directory.CreateDirectory(UIPath);
            }

            AssetDatabase.Refresh();

            // Set Texture Importer Settings with exact 9-Slice Sprite Borders
            string[] files = Directory.GetFiles(UIPath, "*.png");
            foreach (var file in files)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear;

                    if (assetPath.Contains("ui_capsule_bordered") || assetPath.Contains("ui_capsule"))
                    {
                        // 256x128 capsule with 64px radius caps
                        importer.spriteBorder = new Vector4(64, 63, 64, 63);
                    }
                    else if (assetPath.Contains("ui_day_box"))
                    {
                        // 256x256 rounded square with 28px radius
                        importer.spriteBorder = new Vector4(28, 28, 28, 28);
                    }
                    else if (assetPath.Contains("ui_rounded_card"))
                    {
                        // 256x256 rounded card with 20px radius
                        importer.spriteBorder = new Vector4(20, 20, 20, 20);
                    }
                    else if (assetPath.Contains("ui_modal_bg"))
                    {
                        importer.spriteBorder = new Vector4(32, 32, 32, 32);
                    }
                    else if (assetPath.Contains("ui_mission_card"))
                    {
                        importer.spriteBorder = new Vector4(20, 20, 20, 20);
                    }

                    importer.SaveAndReimport();
                }
            }

            Debug.Log("<color=green>[JuegoTCG] Sprites vectoriales y 9-slices calibrados con éxito!</color>");
        }
    }
}
#endif
