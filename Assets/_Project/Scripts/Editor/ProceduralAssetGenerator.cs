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
            CreatePillSprite($"{UIPath}/ui_pill.png", 256, 128, 64);
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
                        importer.spriteBorder = new Vector4(64, 60, 64, 60);
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

            Debug.Log("<color=green>[JuegoTCG] Sprites procedurales de UI generados con 9-slice borders con exito!</color>");
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

        private static void CreatePillSprite(string path, int width, int height, float radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float dist;
                    if (px < radius)
                        dist = Vector2.Distance(new Vector2(px, py), new Vector2(radius, height * 0.5f));
                    else if (px > width - radius)
                        dist = Vector2.Distance(new Vector2(px, py), new Vector2(width - radius, height * 0.5f));
                    else
                        dist = Mathf.Abs(py - height * 0.5f);

                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
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
            float rOuter = size * 0.46f;
            float rInner = size * 0.19f;
            int points = 5;

            Vector2[] starPoly = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float angle = (i * Mathf.PI / points) - (Mathf.PI * 0.5f);
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
                        // Radial Gold gradient (from bright yellow-gold in center to rich amber on edge)
                        float dist = Vector2.Distance(pt, center) / rOuter;
                        Color starColor = Color.Lerp(new Color(1f, 0.92f, 0.55f, 1f), new Color(0.96f, 0.65f, 0.14f, 1f), dist);
                        tex.SetPixel(x, y, starColor);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRaysSprite(string path, int size, int rayCount)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 dir = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float dist = dir.magnitude;
                    if (dist > maxR)
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                        continue;
                    }

                    float angle = (Mathf.Atan2(dir.y, dir.x) + Mathf.PI) / (Mathf.PI * 2f); // 0..1
                    float rayCycle = (angle * rayCount) % 1f;
                    float rayAlpha = Mathf.SmoothStep(0f, 1f, Mathf.Sin(rayCycle * Mathf.PI));
                    float fade = 1f - (dist / maxR);

                    Color rayCol = new Color(0.96f, 0.65f, 0.14f, rayAlpha * fade * 0.28f);
                    tex.SetPixel(x, y, rayCol);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStadiumBackgroundSprite(string path, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color topColor = new Color(0.043f, 0.071f, 0.125f); // #0B1220 (Dark Night Blue)
            Color bottomColor = new Color(0.059f, 0.137f, 0.094f); // #0F2318 (Stadium Pitch Dark Green)
            Color goldOrb = new Color(0.96f, 0.65f, 0.14f); // #F5A623 (Warm Top Light)
            Color blueOrb = new Color(0.24f, 0.55f, 0.87f); // #3E8EDE (Cyan Top Light)
            Color centerGlow = new Color(0.12f, 0.22f, 0.38f); // Stage depth glow

            for (int y = 0; y < height; y++)
            {
                float ny = (float)y / (height - 1); // 0 (bottom) to 1 (top)

                // 1. Vertical base gradient: transitions from #0B1220 down to #0F2318 in the bottom 45%
                float tBase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ny / 0.50f));
                Color baseCol = Color.Lerp(bottomColor, topColor, tBase);

                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / (width - 1); // 0 (left) to 1 (right)

                    // 2. Gold Light Orb (Top-Left, 20% X, 100% Y)
                    float distGold = Mathf.Sqrt(Mathf.Pow((nx - 0.20f) / 0.50f, 2) + Mathf.Pow((ny - 1.02f) / 0.32f, 2));
                    float alphaGold = Mathf.Clamp01(1f - distGold) * 0.22f;

                    // 3. Blue Light Orb (Top-Right, 80% X, 100% Y)
                    float distBlue = Mathf.Sqrt(Mathf.Pow((nx - 0.80f) / 0.50f, 2) + Mathf.Pow((ny - 1.02f) / 0.32f, 2));
                    float alphaBlue = Mathf.Clamp01(1f - distBlue) * 0.22f;

                    // 4. Center Stage Spotlight Glow (Centered behind cards)
                    float distCenter = Mathf.Sqrt(Mathf.Pow((nx - 0.50f) / 0.55f, 2) + Mathf.Pow((ny - 0.52f) / 0.38f, 2));
                    float alphaCenter = Mathf.Clamp01(1f - distCenter) * 0.30f;

                    // Blend lighting onto base gradient
                    Color finalPixel = baseCol + (goldOrb * alphaGold) + (blueOrb * alphaBlue) + (centerGlow * alphaCenter);
                    finalPixel.a = 1f;

                    tex.SetPixel(x, y, finalPixel);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStadiumLinesSprite(string path, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Clear transparent
            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Left & Right goalpost stadium lines (approx 9% from screen edges)
            int leftLineX = Mathf.RoundToInt(width * 0.088f);
            int rightLineX = Mathf.RoundToInt(width * 0.912f);
            int lineWidth = 2;

            for (int y = 0; y < height; y++)
            {
                float ny = (float)y / (height - 1);
                // Visible from top (0.95) fading out towards bottom (0.28)
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ny - 0.28f) / 0.65f)) * 0.16f;
                Color lineCol = new Color(1f, 1f, 1f, alpha);

                for (int w = -lineWidth; w <= lineWidth; w++)
                {
                    if (leftLineX + w >= 0 && leftLineX + w < width)
                    {
                        tex.SetPixel(leftLineX + w, y, lineCol);
                    }
                    if (rightLineX + w >= 0 && rightLineX + w < width)
                    {
                        tex.SetPixel(rightLineX + w, y, lineCol);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static bool IsPointInPolygon(Vector2 p, Vector2[] poly)
        {
            int n = poly.Length;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
#endif
