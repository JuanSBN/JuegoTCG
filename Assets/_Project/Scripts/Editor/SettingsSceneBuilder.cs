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
    public static class SettingsSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/SettingsScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Design Tokens
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);
        private static readonly Color RedBorder = new Color(0.843f, 0.255f, 0.255f, 0.35f);
        private static readonly Color RedText = new Color(0.882f, 0.294f, 0.294f, 0.95f);

        [MenuItem("JuegoTCG/Generar Pantalla de Ajustes (Settings)")]
        public static void BuildSettingsScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_back.png");
            Sprite iconMusic = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_music.png");
            Sprite iconBell = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_bell.png");
            Sprite iconDoc = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_document.png");
            Sprite iconChevron = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_chevron_right.png");

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

            // Background
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
            GameObject controllerGO = new GameObject("SettingsScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            SettingsScreenController controller = controllerGO.AddComponent<SettingsScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. TOP HEADER (Back Arrow + "AJUSTES")
            // ====================================================
            GameObject topBarGO = new GameObject("TopHeader");
            topBarGO.transform.SetParent(contentGO.transform, false);
            RectTransform topBarRect = topBarGO.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0.5f, 1f);
            topBarRect.anchorMax = new Vector2(0.5f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0, -55);
            topBarRect.sizeDelta = new Vector2(980, 90);

            // Back Button Left
            GameObject backBtnGO = new GameObject("BackButton");
            backBtnGO.transform.SetParent(topBarGO.transform, false);
            RectTransform backBtnRect = backBtnGO.AddComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0f, 0.5f);
            backBtnRect.anchorMax = new Vector2(0f, 0.5f);
            backBtnRect.pivot = new Vector2(0f, 0.5f);
            backBtnRect.anchoredPosition = new Vector2(0, 0);
            backBtnRect.sizeDelta = new Vector2(50, 50);

            Image backImg = backBtnGO.AddComponent<Image>();
            if (iconBack != null) backImg.sprite = iconBack;
            backImg.color = new Color(1f, 1f, 1f, 0.75f);
            Button backBtn = backBtnGO.AddComponent<Button>();

            // Title: "AJUSTES"
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(topBarGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(70, 0);
            titleRect.sizeDelta = new Vector2(0, 80);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "AJUSTES";
            titleTMP.fontSize = 46;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // ====================================================
            // 2. SETTINGS LIST CARD (Música, Notificaciones, Términos)
            // ====================================================
            GameObject settingsCardGO = new GameObject("SettingsCard");
            settingsCardGO.transform.SetParent(contentGO.transform, false);
            RectTransform scRect = settingsCardGO.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 1f);
            scRect.anchorMax = new Vector2(0.5f, 1f);
            scRect.pivot = new Vector2(0.5f, 1f);
            scRect.anchoredPosition = new Vector2(0, -165);
            scRect.sizeDelta = new Vector2(980, 270);

            RoundedRectGraphic scG = settingsCardGO.AddComponent<RoundedRectGraphic>();
            scG.CornerRadius = 20f;
            scG.color = CardBg;
            scG.BorderWidth = 1.5f;
            scG.BorderColor = BorderSubtle;

            // Row 1: Música
            GameObject musicRowGO = CreateSettingRow(settingsCardGO.transform, "Row_Music", 0, iconMusic, "Música", dmSansTMPFont);
            GameObject musicToggleGO = CreateToggleSwitch(musicRowGO.transform, "MusicToggle", out RectTransform musicHandle, out RoundedRectGraphic musicBg);
            Button musicBtn = musicToggleGO.AddComponent<Button>();

            // Divider 1
            CreateDivider(settingsCardGO.transform, "Divider_1", -90);

            // Row 2: Notificaciones
            GameObject notifsRowGO = CreateSettingRow(settingsCardGO.transform, "Row_Notifs", -90, iconBell, "Notificaciones", dmSansTMPFont);
            GameObject notifsToggleGO = CreateToggleSwitch(notifsRowGO.transform, "NotifsToggle", out RectTransform notifsHandle, out RoundedRectGraphic notifsBg);
            Button notifsBtn = notifsToggleGO.AddComponent<Button>();

            // Divider 2
            CreateDivider(settingsCardGO.transform, "Divider_2", -180);

            // Row 3: Términos y privacidad
            GameObject termsRowGO = CreateSettingRow(settingsCardGO.transform, "Row_Terms", -180, iconDoc, "Términos y privacidad", dmSansTMPFont);
            GameObject termsChevronGO = new GameObject("Chevron");
            termsChevronGO.transform.SetParent(termsRowGO.transform, false);
            RectTransform tcRect = termsChevronGO.AddComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(1f, 0.5f);
            tcRect.anchorMax = new Vector2(1f, 0.5f);
            tcRect.pivot = new Vector2(1f, 0.5f);
            tcRect.anchoredPosition = new Vector2(-24, 0);
            tcRect.sizeDelta = new Vector2(24, 24);
            Image tcImg = termsChevronGO.AddComponent<Image>();
            if (iconChevron != null) tcImg.sprite = iconChevron;
            tcImg.color = new Color(1f, 1f, 1f, 0.35f);
            Button termsBtn = termsRowGO.AddComponent<Button>();

            // ====================================================
            // 3. CERRAR SESIÓN BUTTON
            // ====================================================
            GameObject logoutBtnGO = new GameObject("LogoutButton");
            logoutBtnGO.transform.SetParent(contentGO.transform, false);
            RectTransform loRect = logoutBtnGO.AddComponent<RectTransform>();
            loRect.anchorMin = new Vector2(0.5f, 1f);
            loRect.anchorMax = new Vector2(0.5f, 1f);
            loRect.pivot = new Vector2(0.5f, 1f);
            loRect.anchoredPosition = new Vector2(0, -485);
            loRect.sizeDelta = new Vector2(980, 72);

            RoundedRectGraphic loG = logoutBtnGO.AddComponent<RoundedRectGraphic>();
            loG.CornerRadius = 14f;
            loG.color = Color.clear;
            loG.BorderWidth = 1.5f;
            loG.BorderColor = RedBorder;
            Button logoutBtn = logoutBtnGO.AddComponent<Button>();

            GameObject loTextGO = new GameObject("Text");
            loTextGO.transform.SetParent(logoutBtnGO.transform, false);
            RectTransform lotRect = loTextGO.AddComponent<RectTransform>();
            lotRect.anchorMin = Vector2.zero;
            lotRect.anchorMax = Vector2.one;
            lotRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI lotTMP = loTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) lotTMP.font = barlowTMPFont;
            lotTMP.text = "CERRAR SESIÓN";
            lotTMP.fontSize = 26;
            lotTMP.fontStyle = FontStyles.Bold;
            lotTMP.characterSpacing = 4f;
            lotTMP.alignment = TextAlignmentOptions.Center;
            lotTMP.color = RedText;

            // ====================================================
            // 4. VERSION TEXT
            // ====================================================
            GameObject verGO = new GameObject("VersionText");
            verGO.transform.SetParent(contentGO.transform, false);
            RectTransform verRect = verGO.AddComponent<RectTransform>();
            verRect.anchorMin = new Vector2(0.5f, 1f);
            verRect.anchorMax = new Vector2(0.5f, 1f);
            verRect.pivot = new Vector2(0.5f, 1f);
            verRect.anchoredPosition = new Vector2(0, -585);
            verRect.sizeDelta = new Vector2(980, 32);
            TextMeshProUGUI verTMP = verGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) verTMP.font = dmSansTMPFont;
            verTMP.text = "Versión 0.1.0 · Build 47";
            verTMP.fontSize = 20;
            verTMP.alignment = TextAlignmentOptions.Center;
            verTMP.color = TextDim;

            // ====================================================
            // 5. BOTTOM NAVIGATION BAR (5 Tabs, "Perfil" Active)
            // ====================================================
            GameObject bottomBarGO = new GameObject("BottomNavigationBar");
            bottomBarGO.transform.SetParent(canvasGO.transform, false);
            RectTransform bottomBarRect = bottomBarGO.AddComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0.5f, 0f);
            bottomBarRect.anchorMax = new Vector2(0.5f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.anchoredPosition = new Vector2(0, 48);
            bottomBarRect.sizeDelta = new Vector2(1000, 140);

            RoundedRectGraphic navG = bottomBarGO.AddComponent<RoundedRectGraphic>();
            navG.IsCapsule = true;
            navG.color = NavBg;
            navG.BorderWidth = 1.5f;
            navG.BorderColor = BorderSubtle;

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
                bool isTabActive = (i == 4); // Perfil is Tab 4 (Active)
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

            // Assign Serialized Properties on SettingsScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("musicToggleButton").objectReferenceValue = musicBtn;
            so.FindProperty("musicToggleHandle").objectReferenceValue = musicHandle;
            so.FindProperty("musicToggleBackground").objectReferenceValue = musicBg;

            so.FindProperty("notifsToggleButton").objectReferenceValue = notifsBtn;
            so.FindProperty("notifsToggleHandle").objectReferenceValue = notifsHandle;
            so.FindProperty("notifsToggleBackground").objectReferenceValue = notifsBg;

            so.FindProperty("termsButton").objectReferenceValue = termsBtn;
            so.FindProperty("logoutButton").objectReferenceValue = logoutBtn;
            so.FindProperty("versionText").objectReferenceValue = verTMP;

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/SettingsScreenUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

            // Save Scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Update Build Settings
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/HomeScreenScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/MyCardsScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MyCardsScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/StoreScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/StoreScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/CommunityScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/CommunityScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/VitrinesScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/VitrinesScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/TradeScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/TradeScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/MarketScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MarketScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/FriendsScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/FriendsScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/SettingsScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/SettingsScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Ajustes guardada como Escena Oficial (SettingsScene.unity) y Prefab (SettingsScreenUI.prefab)!</color>");
        }

        private static GameObject CreateSettingRow(Transform parent, string name, float yOffset, Sprite icon, string label, TMP_FontAsset font)
        {
            GameObject rowGO = new GameObject(name);
            rowGO.transform.SetParent(parent, false);
            RectTransform rRect = rowGO.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 1f);
            rRect.anchorMax = new Vector2(1f, 1f);
            rRect.pivot = new Vector2(0.5f, 1f);
            rRect.anchoredPosition = new Vector2(0, yOffset);
            rRect.sizeDelta = new Vector2(0, 90);

            // Left Icon
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);
            RectTransform iRect = iconGO.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.5f);
            iRect.anchorMax = new Vector2(0f, 0.5f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.anchoredPosition = new Vector2(24, 0);
            iRect.sizeDelta = new Vector2(28, 28);
            Image img = iconGO.AddComponent<Image>();
            if (icon != null) img.sprite = icon;
            img.color = TextWhite;

            // Label
            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(rowGO.transform, false);
            RectTransform lRect = lblGO.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 0.5f);
            lRect.anchorMax = new Vector2(0.7f, 0.5f);
            lRect.pivot = new Vector2(0f, 0.5f);
            lRect.anchoredPosition = new Vector2(68, 0);
            lRect.sizeDelta = new Vector2(0, 32);
            TextMeshProUGUI lTMP = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) lTMP.font = font;
            lTMP.text = label;
            lTMP.fontSize = 24;
            lTMP.color = TextWhite;

            return rowGO;
        }

        private static GameObject CreateToggleSwitch(Transform parent, string name, out RectTransform handleRect, out RoundedRectGraphic bgGraphic)
        {
            GameObject toggleGO = new GameObject(name);
            toggleGO.transform.SetParent(parent, false);
            RectTransform tRect = toggleGO.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(1f, 0.5f);
            tRect.anchorMax = new Vector2(1f, 0.5f);
            tRect.pivot = new Vector2(1f, 0.5f);
            tRect.anchoredPosition = new Vector2(-24, 0);
            tRect.sizeDelta = new Vector2(72, 40);

            bgGraphic = toggleGO.AddComponent<RoundedRectGraphic>();
            bgGraphic.IsCapsule = true;
            bgGraphic.color = Gold;

            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(toggleGO.transform, false);
            handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = new Vector2(16, 0);
            handleRect.sizeDelta = new Vector2(32, 32);

            RoundedRectGraphic hGraphic = handleGO.AddComponent<RoundedRectGraphic>();
            hGraphic.IsCapsule = true;
            hGraphic.color = new Color(0.051f, 0.102f, 0.075f);

            return toggleGO;
        }

        private static void CreateDivider(Transform parent, string name, float yOffset)
        {
            GameObject divGO = new GameObject(name);
            divGO.transform.SetParent(parent, false);
            RectTransform dRect = divGO.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0f, 1f);
            dRect.anchorMax = new Vector2(1f, 1f);
            dRect.pivot = new Vector2(0.5f, 1f);
            dRect.anchoredPosition = new Vector2(0, yOffset);
            dRect.sizeDelta = new Vector2(-36, 1.5f);

            Image divImg = divGO.AddComponent<Image>();
            divImg.color = BorderSubtle;
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
