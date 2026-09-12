#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;

namespace JuegoTCG.EditorTools
{
    public static class CardPrefabBuilder
    {
        private const string PrefabFolderPath = "Assets/_Project/Prefabs/Cards";
        private const string UIPath = "Assets/_Project/Art/UI";

        private static readonly string[] FrameGuids = new string[]
        {
            "4edcac4ad7f822e4aa7b10b2dd755926", // Comun
            "586794b59d6595341aa4a2f2b59209ce", // Especial
            "ab60ad89df16072448c901abb76cbe3a", // Epica
            "2a89c6d7166430641b49f80a84ac2cd8", // Legendaria
            "8ab77af7592605c48b2e119ccdb7dcb3", // Mitica
            "ae059fc1520988141a79cb933243639f"  // Full Art
        };

        [MenuItem("JuegoTCG/Reconstruir CardPrefab con Banderas y Stats", priority = 52)]
        public static void BuildCardPrefab()
        {
            if (!Directory.Exists(PrefabFolderPath))
            {
                Directory.CreateDirectory(PrefabFolderPath);
                AssetDatabase.Refresh();
            }

            ProceduralAssetGenerator.GenerateUISprites();
            SetupPlayerPhotos();

            ConfigureFontImporters();
            AssetDatabase.Refresh();

            TMP_FontAsset momoTMPFont = GetOrCreateTMPFont("MomoTrustDisplay-Regular");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-Bold");

            Sprite roundedCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_rounded_card.png");
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_circle.png");
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_star.png");

            // Load CardFrames using GUIDs
            Sprite[] frames = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(FrameGuids[i]);
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            // Create Root GameObject
            GameObject rootGO = new GameObject("CardPrefab");
            RectTransform rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(360, 500);

            CardDisplay display = rootGO.AddComponent<CardDisplay>();
            rootGO.AddComponent<HolographicTilt>();

            // ----------------------------------------------------
            // FRONT CONTAINER
            // ----------------------------------------------------
            GameObject frontGO = new GameObject("FrontContainer");
            frontGO.transform.SetParent(rootGO.transform, false);
            RectTransform frontRect = frontGO.AddComponent<RectTransform>();
            frontRect.anchorMin = Vector2.zero;
            frontRect.anchorMax = Vector2.one;
            frontRect.sizeDelta = Vector2.zero;

            // 1. Dark Card Background base with Mask (Clips full-bleed player photo to card corners)
            GameObject bgBaseGO = new GameObject("CardBaseBackground");
            bgBaseGO.transform.SetParent(frontGO.transform, false);
            RectTransform bgBaseRect = bgBaseGO.AddComponent<RectTransform>();
            bgBaseRect.anchorMin = Vector2.zero;
            bgBaseRect.anchorMax = Vector2.one;
            bgBaseRect.sizeDelta = Vector2.zero;
            Image bgBaseImg = bgBaseGO.AddComponent<Image>();
            bgBaseImg.sprite = roundedCardSprite;
            bgBaseImg.type = Image.Type.Sliced;
            bgBaseImg.color = new Color(0.05f, 0.08f, 0.13f); // #0D1421

            Mask cardMask = bgBaseGO.AddComponent<Mask>();
            cardMask.showMaskGraphic = true;

            // 2. Player Photo Image (Spans the ENTIRE card, clipped by card mask)
            GameObject photoGO = new GameObject("PlayerArtImage");
            photoGO.transform.SetParent(bgBaseGO.transform, false);
            RectTransform photoRect = photoGO.AddComponent<RectTransform>();
            photoRect.anchorMin = Vector2.zero;
            photoRect.anchorMax = Vector2.one;
            photoRect.sizeDelta = Vector2.zero;
            Image photoImg = photoGO.AddComponent<Image>();
            photoImg.preserveAspect = false; // Fills entire card space
            photoGO.SetActive(false);

            // 3. Placeholder Avatar (When no photo is set)
            GameObject placeholderGO = new GameObject("PlaceholderAvatar");
            placeholderGO.transform.SetParent(bgBaseGO.transform, false);
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
            placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
            placeholderRect.pivot = new Vector2(0.5f, 0.5f);
            placeholderRect.anchoredPosition = new Vector2(0, 45);
            placeholderRect.sizeDelta = new Vector2(130, 130);
            Image avatarCircleImg = placeholderGO.AddComponent<Image>();
            avatarCircleImg.sprite = circleSprite;
            avatarCircleImg.color = new Color(0.12f, 0.18f, 0.28f);

            GameObject initialsGO = new GameObject("PlayerInitialsText");
            initialsGO.transform.SetParent(placeholderGO.transform, false);
            RectTransform initialsRect = initialsGO.AddComponent<RectTransform>();
            initialsRect.anchorMin = Vector2.zero;
            initialsRect.anchorMax = Vector2.one;
            initialsRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI initialsTMP = initialsGO.AddComponent<TextMeshProUGUI>();
            if (momoTMPFont != null) initialsTMP.font = momoTMPFont;
            initialsTMP.text = "LY";
            initialsTMP.fontSize = 44;
            initialsTMP.fontStyle = FontStyles.Bold;
            initialsTMP.alignment = TextAlignmentOptions.Center;
            initialsTMP.color = Color.white;

            // 4. Frame Image (Rendered on top of the art so borders & box frame the picture)
            GameObject frameGO = new GameObject("FrameImage");
            frameGO.transform.SetParent(frontGO.transform, false);
            RectTransform frameRect = frameGO.AddComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.sizeDelta = Vector2.zero;
            Image frameImg = frameGO.AddComponent<Image>();
            if (frames[0] != null) frameImg.sprite = frames[0];

            // 4b. Top-Right OVR Tab Container (Fits inside Component 1 tab - Circular Badge)
            GameObject ovrGO = new GameObject("OVRContainer");
            ovrGO.transform.SetParent(frontGO.transform, false);
            RectTransform ovrRect = ovrGO.AddComponent<RectTransform>();
            ovrRect.anchorMin = new Vector2(0.80f, 0.81f);
            ovrRect.anchorMax = new Vector2(0.96f, 0.94f);
            ovrRect.sizeDelta = Vector2.zero;

            Image ovrBg = ovrGO.AddComponent<Image>();
            ovrBg.sprite = circleSprite;
            ovrBg.color = new Color(0.04f, 0.06f, 0.10f, 0.75f);

            VerticalLayoutGroup ovrVlg = ovrGO.AddComponent<VerticalLayoutGroup>();
            ovrVlg.childControlWidth = true;
            ovrVlg.childControlHeight = true;
            ovrVlg.childForceExpandWidth = true;
            ovrVlg.childForceExpandHeight = true;
            ovrVlg.padding = new RectOffset(2, 2, 6, 4);
            ovrVlg.spacing = -6f;

            GameObject ovrValGO = new GameObject("OVRValueText");
            ovrValGO.transform.SetParent(ovrGO.transform, false);
            TextMeshProUGUI ovrValTMP = ovrValGO.AddComponent<TextMeshProUGUI>();
            if (momoTMPFont != null) ovrValTMP.font = momoTMPFont;
            ovrValTMP.text = "89";
            ovrValTMP.fontSize = 28;
            ovrValTMP.fontStyle = FontStyles.Bold;
            ovrValTMP.alignment = TextAlignmentOptions.Center;
            ovrValTMP.color = Color.white;

            GameObject ovrTitleGO = new GameObject("OVRTitleText");
            ovrTitleGO.transform.SetParent(ovrGO.transform, false);
            TextMeshProUGUI ovrTitleTMP = ovrTitleGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) ovrTitleTMP.font = dmSansTMPFont;
            ovrTitleTMP.text = "GRL";
            ovrTitleTMP.fontSize = 11;
            ovrTitleTMP.fontStyle = FontStyles.Bold;
            ovrTitleTMP.alignment = TextAlignmentOptions.Center;
            ovrTitleTMP.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);

            // 5. Player Name Text (Positioned cleanly right above the bottom tab)
            GameObject nameGO = new GameObject("PlayerNameText");
            nameGO.transform.SetParent(frontGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.04f, 0.16f);
            nameRect.anchorMax = new Vector2(0.96f, 0.26f);
            nameRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (momoTMPFont != null) nameTMP.font = momoTMPFont;
            nameTMP.text = "Lamine Yamal";
            nameTMP.fontSize = 32;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.outlineColor = new Color32(0, 0, 0, 180);
            nameTMP.outlineWidth = 0.08f;

            if (nameTMP.fontMaterial != null)
            {
                nameTMP.fontMaterial.EnableKeyword("OUTLINE_ON");
                nameTMP.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.70f));
                nameTMP.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);

                nameTMP.fontMaterial.EnableKeyword("UNDERLAY_ON");
                nameTMP.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                nameTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 2.0f);
                nameTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -3.0f);
                nameTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
                nameTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.0f);
            }

            // Soft natural drop shadow
            Shadow nameShadow = nameGO.AddComponent<Shadow>();
            nameShadow.effectColor = new Color(0f, 0f, 0f, 0.80f);
            nameShadow.effectDistance = new Vector2(2f, -3f);

            // 6. Flag Image (Positioned outside the tab, centered in its left space)
            GameObject flagGO = new GameObject("FlagImage");
            flagGO.transform.SetParent(frontGO.transform, false);
            RectTransform flagRect = flagGO.AddComponent<RectTransform>();
            flagRect.anchorMin = new Vector2(0.054f, 0.025f);
            flagRect.anchorMax = new Vector2(0.174f, 0.135f);
            flagRect.sizeDelta = Vector2.zero;
            Image flagImg = flagGO.AddComponent<Image>();
            flagImg.preserveAspect = true;

            // 7. Footer Box / Tab (Fits inside the frame's bottom tab - now houses the 4 stats!)
            GameObject footerGO = new GameObject("FooterContainer");
            footerGO.transform.SetParent(frontGO.transform, false);
            RectTransform footerRect = footerGO.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.20f, 0.015f);
            footerRect.anchorMax = new Vector2(0.80f, 0.145f);
            footerRect.sizeDelta = Vector2.zero;

            // Horizontal Stats Container inside the frame tab
            GameObject statsGroupGO = new GameObject("StatsGroup");
            statsGroupGO.transform.SetParent(footerGO.transform, false);
            RectTransform statsGroupRect = statsGroupGO.AddComponent<RectTransform>();
            statsGroupRect.anchorMin = new Vector2(0.02f, 0.05f);
            statsGroupRect.anchorMax = new Vector2(0.98f, 0.95f);
            statsGroupRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup hlg = statsGroupGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 2f;

            // 4 Stats Columns inside the tab (Title top, Value bottom)
            TextMeshProUGUI s1Title, s1Val, s2Title, s2Val, s3Title, s3Val, s4Title, s4Val;
            CreateStatColumn(statsGroupGO.transform, dmSansTMPFont, "TIR", "83", out s1Title, out s1Val);
            CreateStatColumn(statsGroupGO.transform, dmSansTMPFont, "PAS", "84", out s2Title, out s2Val);
            CreateStatColumn(statsGroupGO.transform, dmSansTMPFont, "DEF", "38", out s3Title, out s3Val);
            CreateStatColumn(statsGroupGO.transform, dmSansTMPFont, "REG", "89", out s4Title, out s4Val);

            // ----------------------------------------------------
            // BACK CONTAINER (Reverso con estrella dorada para 3D flip)
            // ----------------------------------------------------
            GameObject backGO = new GameObject("BackContainer");
            backGO.transform.SetParent(rootGO.transform, false);
            RectTransform backRect = backGO.AddComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.sizeDelta = Vector2.zero;

            // Back Border
            GameObject backBorderGO = new GameObject("BackBorder");
            backBorderGO.transform.SetParent(backGO.transform, false);
            RectTransform backBorderRect = backBorderGO.AddComponent<RectTransform>();
            backBorderRect.anchorMin = Vector2.zero;
            backBorderRect.anchorMax = Vector2.one;
            backBorderRect.sizeDelta = Vector2.zero;
            Image backBorderImg = backBorderGO.AddComponent<Image>();
            backBorderImg.sprite = roundedCardSprite;
            backBorderImg.type = Image.Type.Sliced;
            backBorderImg.color = new Color(0.96f, 0.65f, 0.14f, 0.45f);

            // Back Inner Body
            GameObject backInnerGO = new GameObject("BackInner");
            backInnerGO.transform.SetParent(backBorderGO.transform, false);
            RectTransform backInnerRect = backInnerGO.AddComponent<RectTransform>();
            backInnerRect.anchorMin = Vector2.zero;
            backInnerRect.anchorMax = Vector2.one;
            backInnerRect.sizeDelta = new Vector2(-8, -8);
            Image backInnerImg = backInnerGO.AddComponent<Image>();
            backInnerImg.sprite = roundedCardSprite;
            backInnerImg.type = Image.Type.Sliced;
            backInnerImg.color = new Color(0.08f, 0.13f, 0.22f);

            // Back Star Sprite
            GameObject starGO = new GameObject("BackStar");
            starGO.transform.SetParent(backInnerGO.transform, false);
            RectTransform starRect = starGO.AddComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.pivot = new Vector2(0.5f, 0.5f);
            starRect.sizeDelta = new Vector2(130, 130);
            Image backStarImg = starGO.AddComponent<Image>();
            backStarImg.sprite = starSprite;
            backStarImg.color = Color.white;

            backGO.SetActive(false);

            // Load Holographic Material
            Material holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/HolographicFoilMaterial.mat");

            // Assign Fields to SerializedObject of CardDisplay
            SerializedObject so = new SerializedObject(display);
            so.FindProperty("frontContainer").objectReferenceValue = frontGO;
            so.FindProperty("backContainer").objectReferenceValue = backGO;
            so.FindProperty("frameImage").objectReferenceValue = frameImg;
            so.FindProperty("playerArtImage").objectReferenceValue = photoImg;
            so.FindProperty("placeholderAvatar").objectReferenceValue = placeholderGO;
            so.FindProperty("playerInitialsText").objectReferenceValue = initialsTMP;
            so.FindProperty("nameText").objectReferenceValue = nameTMP;
            so.FindProperty("teamText").objectReferenceValue = null;
            so.FindProperty("positionText").objectReferenceValue = null;
            so.FindProperty("rarityText").objectReferenceValue = null;
            so.FindProperty("holographicMaterial").objectReferenceValue = holoMat;

            // Flag & Stats serialization
            so.FindProperty("flagImage").objectReferenceValue = flagImg;
            so.FindProperty("stat1TitleText").objectReferenceValue = s1Title;
            so.FindProperty("stat1ValueText").objectReferenceValue = s1Val;
            so.FindProperty("stat2TitleText").objectReferenceValue = s2Title;
            so.FindProperty("stat2ValueText").objectReferenceValue = s2Val;
            so.FindProperty("stat3TitleText").objectReferenceValue = s3Title;
            so.FindProperty("stat3ValueText").objectReferenceValue = s3Val;
            so.FindProperty("stat4TitleText").objectReferenceValue = s4Title;
            so.FindProperty("stat4ValueText").objectReferenceValue = s4Val;

            // OVR serialization
            so.FindProperty("ovrValueText").objectReferenceValue = ovrValTMP;
            so.FindProperty("ovrTitleText").objectReferenceValue = ovrTitleTMP;

            SerializedProperty framesProp = so.FindProperty("rarityFrames");
            framesProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabPath = $"{PrefabFolderPath}/CardPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            Object.DestroyImmediate(rootGO);

            // Also copy to Resources/CardPrefab.prefab
            if (File.Exists(prefabPath))
            {
                if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.CopyAsset(prefabPath, "Assets/Resources/CardPrefab.prefab");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡CardPrefab.prefab recreado con los marcos oficiales de CardFrames por encima del arte!</color>");
        }

        public static void SetupPlayerPhotos()
        {
            string laminePath = "Assets/_Project/Art/PlayerPhotos/Lamine Yamal.png";
            if (File.Exists(laminePath))
            {
                TextureImporter importer = AssetImporter.GetAtPath(laminePath) as TextureImporter;
                if (importer != null)
                {
                    if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.mipmapEnabled = false;
                        importer.alphaIsTransparency = true;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.SaveAndReimport();
                    }
                }

                Sprite lamineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(laminePath);
                if (lamineSprite != null)
                {
                    string cardAssetPath = "Assets/_Project/ScriptableObjects/PilotAlbum/card_10_Lamine_Yamal.asset";
                    CardData lamineCard = AssetDatabase.LoadAssetAtPath<CardData>(cardAssetPath);
                    if (lamineCard != null)
                    {
                        SerializedObject cardSO = new SerializedObject(lamineCard);
                        cardSO.FindProperty("defaultArt").objectReferenceValue = lamineSprite;
                        cardSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(lamineCard);
                        AssetDatabase.SaveAssets();
                        Debug.Log("<color=cyan>[JuegoTCG] Foto oficial de Lamine Yamal asignada exitosamente a card_10_Lamine_Yamal.asset</color>");
                    }
                }
            }
        }

        private const string FontPath = "Assets/_Project/Art/Fonts";

        private static void ConfigureFontImporters()
        {
            if (!Directory.Exists(FontPath)) return;
            string[] fontFiles = Directory.GetFiles(FontPath, "*.ttf");
            foreach (var file in fontFiles)
            {
                string assetPath = file.Replace('\\', '/');
                TrueTypeFontImporter importer = AssetImporter.GetAtPath(assetPath) as TrueTypeFontImporter;
                if (importer != null && !importer.includeFontData)
                {
                    importer.includeFontData = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static TMP_FontAsset GetOrCreateTMPFont(string fontName)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>($"{FontPath}/{fontName}.ttf");
            if (font != null)
            {
                try
                {
                    TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
                    if (fontAsset != null)
                    {
                        fontAsset.name = $"{fontName} SDF";
                        return fontAsset;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[JuegoTCG] Creación dinámica de fuente {fontName}: {ex.Message}");
                }
            }
            return TMP_Settings.defaultFontAsset;
        }

        private static void CreateStatColumn(Transform parent, TMP_FontAsset font, string defaultTitle, string defaultVal, out TextMeshProUGUI titleTMP, out TextMeshProUGUI valTMP)
        {
            GameObject colGO = new GameObject($"Stat_{defaultTitle}");
            colGO.transform.SetParent(parent, false);
            RectTransform colRect = colGO.AddComponent<RectTransform>();

            VerticalLayoutGroup vlg = colGO.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.spacing = -2f;

            // Stat Title (e.g. TIR, PAS, DEF, REG)
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(colGO.transform, false);
            titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (font != null) titleTMP.font = font;
            titleTMP.text = defaultTitle;
            titleTMP.fontSize = 11;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(0.45f, 0.45f, 0.50f);

            // Stat Value (e.g. 82, 86, 45, 91)
            GameObject valGO = new GameObject("ValueText");
            valGO.transform.SetParent(colGO.transform, false);
            valTMP = valGO.AddComponent<TextMeshProUGUI>();
            if (font != null) valTMP.font = font;
            valTMP.text = defaultVal;
            valTMP.fontSize = 16;
            valTMP.fontStyle = FontStyles.Bold;
            valTMP.alignment = TextAlignmentOptions.Center;
            valTMP.color = new Color(0.08f, 0.12f, 0.18f);
        }
    }
}
#endif


