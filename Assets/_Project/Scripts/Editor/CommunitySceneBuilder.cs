#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class CommunitySceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/CommunityScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Exact Design Tokens from docs/Pantallas/src/App.tsx
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);

        private struct CommunityItemDef
        {
            public string key;
            public string label;
            public Sprite icon;
            public int? badge;

            public CommunityItemDef(string k, string l, Sprite ic, int? b)
            {
                key = k;
                label = l;
                icon = ic;
                badge = b;
            }
        }

        [MenuItem("JuegoTCG/Generar Pantalla de Comunidad")]
        public static void BuildCommunityScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("JuegoTCG", "Por favor sal del modo Play (detén la ejecución) antes de generar la escena.", "Entendido");
                return;
            }

            ProceduralAssetGenerator.GenerateUISprites();
            ConfigureFontImporters();
            AssetDatabase.Refresh();

            // Load and create persistent SDF Font Assets
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconVitrinas = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_vitrinas.png");
            Sprite iconIntercambio = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_intercambio.png");
            Sprite iconVender = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_vender.png");
            Sprite iconAmigos = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_amigos.png");

            Sprite iconHome = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_home.png");
            Sprite iconCards = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_cards.png");
            Sprite iconShop = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_shop.png");
            Sprite iconUsers = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_users.png");
            Sprite iconUser = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_user.png");

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.051f, 0.082f, 0.125f);
            cam.orthographic = true;
            camGO.AddComponent<AudioListener>();

            // Canvas
            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Event System
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Background (Stadium Atmosphere)
            GameObject bgGO = new GameObject("TacticalPitchBackground");
            bgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGO.AddComponent<Image>();
            if (tacticalPitchSprite != null) bgImg.sprite = tacticalPitchSprite;
            bgImg.color = Color.white;

            // Controller GO
            GameObject controllerGO = new GameObject("CommunityScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            CommunityScreenController controller = controllerGO.AddComponent<CommunityScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. HEADER (Title "COMUNIDAD" in Barlow Condensed Bold)
            // ====================================================
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(contentGO.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0, -60);
            headerRect.sizeDelta = new Vector2(980, 80);

            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "COMUNIDAD";
            titleTMP.fontSize = 38;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.alignment = TextAlignmentOptions.Left;
            titleTMP.color = TextWhite;

            // ====================================================
            // 2. 2x2 GRID OF ACTION CARDS (Aspect Ratio 1:1)
            // ====================================================
            GameObject gridGO = new GameObject("CommunityGrid");
            gridGO.transform.SetParent(contentGO.transform, false);
            RectTransform gridRect = gridGO.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0, -180);
            gridRect.sizeDelta = new Vector2(980, 1050);

            GridLayoutGroup glg = gridGO.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(465, 465); // 1:1 square
            glg.spacing = new Vector2(35, 35);
            glg.constraintCount = 2;

            CommunityItemDef[] items = new CommunityItemDef[]
            {
                new CommunityItemDef("vitrinas", "Vitrinas públicas", iconVitrinas, null),
                new CommunityItemDef("intercambio", "Intercambio", iconIntercambio, 3),
                new CommunityItemDef("vender", "Mercado", iconVender, null),
                new CommunityItemDef("amigos", "Amigos", iconAmigos, 2)
            };

            Button[] actionBtns = new Button[4];

            for (int i = 0; i < items.Length; i++)
            {
                CommunityItemDef item = items[i];
                GameObject cardGO = new GameObject($"Card_{item.key}");
                cardGO.transform.SetParent(gridGO.transform, false);
                RectTransform cardRect = cardGO.AddComponent<RectTransform>();

                // Rounded Box Container
                RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
                cardG.CornerRadius = 24f;
                cardG.color = CardBg;
                cardG.BorderWidth = 1.5f;
                cardG.BorderColor = BorderSubtle;

                // Badge (if applicable)
                if (item.badge.HasValue)
                {
                    GameObject badgeGO = new GameObject("Badge");
                    badgeGO.transform.SetParent(cardGO.transform, false);
                    RectTransform badgeRect = badgeGO.AddComponent<RectTransform>();
                    badgeRect.anchorMin = new Vector2(1f, 1f);
                    badgeRect.anchorMax = new Vector2(1f, 1f);
                    badgeRect.pivot = new Vector2(1f, 1f);
                    badgeRect.anchoredPosition = new Vector2(-20, -20);
                    badgeRect.sizeDelta = new Vector2(54, 40);

                    RoundedRectGraphic badgeG = badgeGO.AddComponent<RoundedRectGraphic>();
                    badgeG.IsCapsule = true;
                    badgeG.color = new Color(0f, 0f, 0f, 0.65f);
                    badgeG.BorderWidth = 1.5f;
                    badgeG.BorderColor = GoldBorder;

                    GameObject badgeTextGO = new GameObject("Text");
                    badgeTextGO.transform.SetParent(badgeGO.transform, false);
                    RectTransform btRect = badgeTextGO.AddComponent<RectTransform>();
                    btRect.anchorMin = Vector2.zero;
                    btRect.anchorMax = Vector2.one;
                    btRect.sizeDelta = Vector2.zero;
                    TextMeshProUGUI btTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
                    if (dmSansTMPFont != null) btTMP.font = dmSansTMPFont;
                    btTMP.text = item.badge.Value.ToString();
                    btTMP.fontSize = 22;
                    btTMP.fontStyle = FontStyles.Bold;
                    btTMP.alignment = TextAlignmentOptions.Center;
                    btTMP.color = Gold;
                }

                // Icon
                GameObject iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(cardGO.transform, false);
                RectTransform iconRect = iconGO.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0, 25);
                iconRect.sizeDelta = new Vector2(84, 84);
                Image iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = item.icon;
                iconImg.color = new Color(1f, 1f, 1f, 0.85f);

                // Label
                GameObject labelGO = new GameObject("Label");
                labelGO.transform.SetParent(cardGO.transform, false);
                RectTransform labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.05f, 0f);
                labelRect.anchorMax = new Vector2(0.95f, 0.35f);
                labelRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) labelTMP.font = dmSansTMPFont;
                labelTMP.text = item.label;
                labelTMP.fontSize = 28;
                labelTMP.fontStyle = FontStyles.Bold;
                labelTMP.alignment = TextAlignmentOptions.Center;
                labelTMP.color = TextWhite;

                actionBtns[i] = cardGO.AddComponent<Button>();
            }

            // ====================================================
            // 3. LIQUID-GLASS BOTTOM NAVIGATION BAR (Tab Comunidad Active)
            // ====================================================
            GameObject bottomBarGO = new GameObject("BottomNavBar");
            bottomBarGO.transform.SetParent(contentGO.transform, false);
            RectTransform bottomBarRect = bottomBarGO.AddComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0.5f, 0f);
            bottomBarRect.anchorMax = new Vector2(0.5f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.anchoredPosition = new Vector2(0, 45);
            bottomBarRect.sizeDelta = new Vector2(980, 135);

            RoundedRectGraphic bottomBarG = bottomBarGO.AddComponent<RoundedRectGraphic>();
            bottomBarG.IsCapsule = true;
            bottomBarG.color = NavBg;
            bottomBarG.BorderWidth = 1.5f;
            bottomBarG.BorderColor = new Color(1f, 1f, 1f, 0.15f);

            // Top glass highlight line
            GameObject glassLineGO = new GameObject("GlassHighlight");
            glassLineGO.transform.SetParent(bottomBarGO.transform, false);
            RectTransform glassRect = glassLineGO.AddComponent<RectTransform>();
            glassRect.anchorMin = new Vector2(0.08f, 1f);
            glassRect.anchorMax = new Vector2(0.92f, 1f);
            glassRect.pivot = new Vector2(0.5f, 1f);
            glassRect.anchoredPosition = new Vector2(0, -2);
            glassRect.sizeDelta = new Vector2(0, 2);
            Image glassImg = glassLineGO.AddComponent<Image>();
            glassImg.color = new Color(1f, 1f, 1f, 0.35f);

            string[] tabLabels = { "Inicio", "Mis cartas", "Tienda", "Comunidad", "Perfil" };
            Sprite[] tabIcons = { iconHome, iconCards, iconShop, iconUsers, iconUser };
            Button[] tabBtns = new Button[5];
            float tabSpacing = 188f;
            float startTabX = -tabSpacing * 2f;

            for (int i = 0; i < 5; i++)
            {
                bool isTabActive = (i == 3); // "Comunidad" is Active
                GameObject tabGO = new GameObject($"Tab_{tabLabels[i]}");
                tabGO.transform.SetParent(bottomBarGO.transform, false);
                RectTransform tabRect = tabGO.AddComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(0.5f, 0.5f);
                tabRect.anchorMax = new Vector2(0.5f, 0.5f);
                tabRect.pivot = new Vector2(0.5f, 0.5f);
                tabRect.anchoredPosition = new Vector2(startTabX + i * tabSpacing, 0);
                tabRect.sizeDelta = isTabActive ? new Vector2(170, 96) : new Vector2(140, 96);

                if (isTabActive)
                {
                    RoundedRectGraphic activePillG = tabGO.AddComponent<RoundedRectGraphic>();
                    activePillG.IsCapsule = true;
                    activePillG.color = new Color(1f, 1f, 1f, 0.10f);
                }

                // Tab Icon
                GameObject tabIconGO = new GameObject("Icon");
                tabIconGO.transform.SetParent(tabGO.transform, false);
                RectTransform tabIconRect = tabIconGO.AddComponent<RectTransform>();
                tabIconRect.anchorMin = new Vector2(0.5f, 0.5f);
                tabIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                tabIconRect.anchoredPosition = new Vector2(0, 14);
                tabIconRect.sizeDelta = new Vector2(46, 46);
                Image tabIconImg = tabIconGO.AddComponent<Image>();
                tabIconImg.sprite = tabIcons[i];
                tabIconImg.color = isTabActive ? Gold : new Color(1f, 1f, 1f, 0.45f);

                // Tab Label
                GameObject tabLabelGO = new GameObject("Label");
                tabLabelGO.transform.SetParent(tabGO.transform, false);
                RectTransform tabLabelRect = tabLabelGO.AddComponent<RectTransform>();
                tabLabelRect.anchorMin = new Vector2(0f, 0f);
                tabLabelRect.anchorMax = new Vector2(1f, 0.35f);
                tabLabelRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI tabLabelTMP = tabLabelGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) tabLabelTMP.font = dmSansTMPFont;
                tabLabelTMP.text = tabLabels[i];
                tabLabelTMP.fontSize = 18;
                tabLabelTMP.fontStyle = isTabActive ? FontStyles.Bold : FontStyles.Normal;
                tabLabelTMP.alignment = TextAlignmentOptions.Center;
                tabLabelTMP.color = isTabActive ? Gold : new Color(1f, 1f, 1f, 0.38f);

                tabBtns[i] = tabGO.AddComponent<Button>();
            }

            // Assign Serialized Properties
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("titleText").objectReferenceValue = titleTMP;
            so.FindProperty("showcasesButton").objectReferenceValue = actionBtns[0];
            so.FindProperty("exchangeButton").objectReferenceValue = actionBtns[1];
            so.FindProperty("sellButton").objectReferenceValue = actionBtns[2];
            so.FindProperty("friendsButton").objectReferenceValue = actionBtns[3];

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];

            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/CommunityUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

            // Save Scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register in Build Settings
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/HomeScreenScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/MyCardsScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MyCardsScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/StoreScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/StoreScene.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/CommunityScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/VitrinesScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/VitrinesScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/TradeScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/TradeScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/MarketScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MarketScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Comunidad generada con éxito (CommunityScene & CommunityUI.prefab)!</color>");
        }

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
    }
}
#endif
