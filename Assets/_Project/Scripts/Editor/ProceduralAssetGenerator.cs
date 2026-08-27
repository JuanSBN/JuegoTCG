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

            CreateCircleSprite($"{UIPath}/ui_circle.png", 128);
            CreatePillSprite($"{UIPath}/ui_pill.png", 128, 128, 62f, false);
            CreatePillSprite($"{UIPath}/ui_pill_bordered.png", 128, 128, 62f, true);
            CreateRoundedRectSprite($"{UIPath}/ui_rounded_card.png", 360, 500, 36);
            CreateRoundedRectSprite($"{UIPath}/ui_rounded_pack.png", 440, 620, 44);
            CreateStarSprite($"{UIPath}/ui_star.png", 256);
            CreateRaysSprite($"{UIPath}/ui_rays.png", 512, 16);
            CreateStadiumBackgroundSprite($"{UIPath}/bg_stadium.png", 512, 1024);
            CreateStadiumLinesSprite($"{UIPath}/bg_stadium_lines.png", 512, 1024);

            AssetDatabase.Refresh();

            // Set Texture Importer Settings with 9-Slice Sprite Borders
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

                    // Set 9-slice borders so shapes never stretch or distort
                    if (assetPath.Contains("ui_pill"))
                    {
                        importer.spriteBorder = new Vector4(62, 62, 62, 62);
                    }
                    else if (assetPath.Contains("ui_rounded_card"))
                    {
                        importer.spriteBorder = new Vector4(40, 40, 40, 40);
                    }
                    else if (assetPath.Contains("ui_rounded_pack"))
                    {
                        importer.spriteBorder = new Vector4(48, 48, 48, 48);
                    }

                    importer.SaveAndReimport();
                }
            }

            Debug.Log("<color=green>[JuegoTCG] Sprites procedurales de UI generados con 9-slice borders perfectos!</color>");
        }

        private static void CreateCircleSprite(string path, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            float radius = center - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreatePillSprite(string path, int width, int height, float radius, bool withBorder)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float borderWidth = 3.5f;

            Color goldFill = new Color(1f, 0.85f, 0.45f, 0.22f);
            Color goldBorder = new Color(1f, 0.88f, 0.55f, 0.90f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float dist = Vector2.Distance(new Vector2(px, py), new Vector2(centerX, centerY));
                    float alphaOuter = Mathf.Clamp01(radius - dist + 1f);

                    if (withBorder)
                    {
                        float innerRadius = radius - borderWidth;
                        float alphaInner = Mathf.Clamp01(innerRadius - dist + 1f);
                        float borderAlpha = Mathf.Clamp01(alphaOuter - alphaInner);

                        Color pixelColor = Color.Lerp(goldFill * alphaInner, goldBorder, borderAlpha);
                        pixelColor.a = Mathf.Max(goldFill.a * alphaInner, goldBorder.a * borderAlpha);
                        tex.SetPixel(x, y, pixelColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaOuter));
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRoundedRectSprite(string path, int width, int height, float radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float dx = Mathf.Max(radius - px, 0f, px - (width - radius));
                    float dy = Mathf.Max(radius - py, 0f, py - (height - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStarSprite(string path, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rOuter = size * 0.44f;
            float rInner = size * 0.18f;
            int points = 5;

            Vector2[] starPoly = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float angle = (i * Mathf.PI / points) - (Mathf.PI / 2f);
                float r = (i % 2 == 0) ? rOuter : rInner;
                starPoly[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    if (IsPointInPolygon(pt, starPoly))
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static bool IsPointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static void CreateRaysSprite(string path, int size, int rayCount)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f - 2f;
            float anglePerRay = 360f / rayCount;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(pt, center);
                    if (dist > maxR)
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                        continue;
                    }

                    float angle = (Mathf.Atan2(pt.y - center.y, pt.x - center.x) * Mathf.Rad2Deg + 360f) % 360f;
                    float rayPos = (angle % anglePerRay) / anglePerRay;

                    // Conic ray segment
                    float alpha = (rayPos < 0.45f) ? Mathf.SmoothStep(0f, 1f, dist / maxR) * 0.18f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 0.85f, 0.4f, alpha));
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStadiumBackgroundSprite(string path, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color bgDeep = new Color(0.043f, 0.070f, 0.125f); // #0B1220
            Color bgPitch = new Color(0.058f, 0.137f, 0.094f); // #0F2318
            Color goldOrb = new Color(0.96f, 0.65f, 0.14f, 0.20f);
            Color blueOrb = new Color(0.24f, 0.55f, 0.87f, 0.20f);

            Vector2 goldCenter = new Vector2(width * 0.20f, height * 1.05f);
            Vector2 blueCenter = new Vector2(width * 0.80f, height * 1.05f);
            float orbRadius = width * 1.3f;

            for (int y = 0; y < height; y++)
            {
                float tY = (float)y / height;
                Color baseCol = Color.Lerp(bgPitch, bgDeep, tY);

                for (int x = 0; x < width; x++)
                {
                    Vector2 pt = new Vector2(x, y);

                    float distGold = Vector2.Distance(pt, goldCenter);
                    float alphaGold = Mathf.Clamp01(1f - (distGold / orbRadius)) * goldOrb.a;

                    float distBlue = Vector2.Distance(pt, blueCenter);
                    float alphaBlue = Mathf.Clamp01(1f - (distBlue / orbRadius)) * blueOrb.a;

                    Color col = baseCol;
                    col = Color.Lerp(col, new Color(goldOrb.r, goldOrb.g, goldOrb.b), alphaGold);
                    col = Color.Lerp(col, new Color(blueOrb.r, blueOrb.g, blueOrb.b), alphaBlue);

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStadiumLinesSprite(string path, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, clear);

            int leftX = (int)(width * 0.088f);
            int rightX = (int)(width * 0.912f);
            int topY = (int)(height * 0.95f);
            int botY = (int)(height * 0.30f);

            for (int y = botY; y <= topY; y++)
            {
                float t = (float)(y - botY) / (topY - botY);
                float alpha = Mathf.SmoothStep(0f, 0.15f, t);
                Color lineCol = new Color(1f, 1f, 1f, alpha);

                for (int dx = 0; dx < 3; dx++)
                {
                    tex.SetPixel(leftX + dx, y, lineCol);
                    tex.SetPixel(rightX + dx, y, lineCol);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
#endif
