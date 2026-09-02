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
    public static class LoginSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/LoginScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Design Tokens
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f, 0.85f); // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.65f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.40f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.18f);

        public static void BuildLoginScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite logoCardsSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_logo_cards.png");
            Sprite iconGoogle = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_google.png");
            Sprite iconEmail = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_email.png");

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
            GameObject controllerGO = new GameObject("LoginScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            LoginScreenController controller = controllerGO.AddComponent<LoginScreenController>();

            // Central Container
            GameObject centerGO = new GameObject("CenterContainer");
            centerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform centerRect = centerGO.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = new Vector2(0, 40);
            centerRect.sizeDelta = new Vector2(940, 1200);

            // ====================================================
            // 1. LOGO SLOT (Logo del juego / Card mark)
            // ====================================================
            GameObject logoSlotGO = new GameObject("GameLogoSlot");
            logoSlotGO.transform.SetParent(centerGO.transform, false);
            RectTransform logoRect = logoSlotGO.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 1f);
            logoRect.anchorMax = new Vector2(0.5f, 1f);
            logoRect.pivot = new Vector2(0.5f, 1f);
            logoRect.anchoredPosition = new Vector2(0, -60);
            logoRect.sizeDelta = new Vector2(280, 260);

            GameObject logoImgGO = new GameObject("CardLogoIcon");
            logoImgGO.transform.SetParent(logoSlotGO.transform, false);
            RectTransform liRect = logoImgGO.AddComponent<RectTransform>();
            liRect.anchorMin = new Vector2(0.5f, 0.5f);
            liRect.anchorMax = new Vector2(0.5f, 0.5f);
            liRect.anchoredPosition = new Vector2(0, 0);
            liRect.sizeDelta = new Vector2(220, 220);
            Image logoImg = logoImgGO.AddComponent<Image>();
            if (logoCardsSprite != null) logoImg.sprite = logoCardsSprite;
            logoImg.preserveAspect = true;

            // ====================================================
            // 2. TITLE & SUBTITLE
            // ====================================================
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(centerGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -360);
            titleRect.sizeDelta = new Vector2(900, 70);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "BIENVENIDO";
            titleTMP.fontSize = 50;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = TextWhite;

            GameObject subGO = new GameObject("SubtitleText");
            subGO.transform.SetParent(centerGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -445);
            subRect.sizeDelta = new Vector2(800, 100);
            TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) subTMP.font = dmSansTMPFont;
            subTMP.text = "Inicia sesión para acceder a tu colección y conectarte con otros jugadores.";
            subTMP.fontSize = 24;
            subTMP.lineSpacing = 16f;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = TextGray;

            // ====================================================
            // 3. PROVIDER BUTTONS (Google & Email)
            // ====================================================
            // Google Button
            GameObject googleBtnGO = CreateProviderButton(centerGO.transform, "GoogleButton", -590, iconGoogle, "Continuar con Google", dmSansTMPFont);
            Button googleBtn = googleBtnGO.GetComponent<Button>();

            // Email Button
            GameObject emailBtnGO = CreateProviderButton(centerGO.transform, "EmailButton", -720, iconEmail, "Continuar con email", dmSansTMPFont);
            Button emailBtn = emailBtnGO.GetComponent<Button>();

            // ====================================================
            // 4. GUEST / SKIP BUTTON ("Continuar como invitado")
            // ====================================================
            GameObject guestBtnGO = new GameObject("GuestButton");
            guestBtnGO.transform.SetParent(centerGO.transform, false);
            RectTransform guestRect = guestBtnGO.AddComponent<RectTransform>();
            guestRect.anchorMin = new Vector2(0.5f, 1f);
            guestRect.anchorMax = new Vector2(0.5f, 1f);
            guestRect.pivot = new Vector2(0.5f, 1f);
            guestRect.anchoredPosition = new Vector2(0, -860);
            guestRect.sizeDelta = new Vector2(600, 60);

            Button guestBtn = guestBtnGO.AddComponent<Button>();
            GameObject guestTextGO = new GameObject("Text");
            guestTextGO.transform.SetParent(guestBtnGO.transform, false);
            RectTransform gtRect = guestTextGO.AddComponent<RectTransform>();
            gtRect.anchorMin = Vector2.zero;
            gtRect.anchorMax = Vector2.one;
            gtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI gtTMP = guestTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) gtTMP.font = dmSansTMPFont;
            gtTMP.text = "Continuar como invitado";
            gtTMP.fontSize = 22;
            gtTMP.alignment = TextAlignmentOptions.Center;
            gtTMP.color = TextDim;

            // Assign Serialized Properties on LoginScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("logoSlotContainer").objectReferenceValue = logoRect;
            so.FindProperty("logoCardIcon").objectReferenceValue = logoImg;
            so.FindProperty("titleText").objectReferenceValue = titleTMP;
            so.FindProperty("subtitleText").objectReferenceValue = subTMP;
            so.FindProperty("googleButton").objectReferenceValue = googleBtn;
            so.FindProperty("emailButton").objectReferenceValue = emailBtn;
            so.FindProperty("guestButton").objectReferenceValue = guestBtn;
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            PrefabUtility.SaveAsPrefabAsset(canvasGO, $"{prefabDir}/LoginScreenUI.prefab");

            // Save Scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Login guardada con éxito (LoginScene.unity)!</color>");
        }

        private static GameObject CreateProviderButton(Transform parent, string name, float yOffset, Sprite icon, string label, TMP_FontAsset font)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform bRect = btnGO.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 1f);
            bRect.anchorMax = new Vector2(0.5f, 1f);
            bRect.pivot = new Vector2(0.5f, 1f);
            bRect.anchoredPosition = new Vector2(0, yOffset);
            bRect.sizeDelta = new Vector2(900, 100);

            RoundedRectGraphic bgG = btnGO.AddComponent<RoundedRectGraphic>();
            bgG.CornerRadius = 18f;
            bgG.color = CardBg;
            bgG.BorderWidth = 1.5f;
            bgG.BorderColor = BorderSubtle;

            Button btn = btnGO.AddComponent<Button>();

            // Icon Left
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(btnGO.transform, false);
            RectTransform iRect = iconGO.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.5f);
            iRect.anchorMax = new Vector2(0f, 0.5f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.anchoredPosition = new Vector2(36, 0);
            iRect.sizeDelta = new Vector2(40, 40);
            Image img = iconGO.AddComponent<Image>();
            if (icon != null) img.sprite = icon;
            img.preserveAspect = true;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);
            RectTransform lRect = labelGO.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 0.5f);
            lRect.anchorMax = new Vector2(1f, 0.5f);
            lRect.pivot = new Vector2(0f, 0.5f);
            lRect.anchoredPosition = new Vector2(100, 0);
            lRect.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI lTMP = labelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) lTMP.font = font;
            lTMP.text = label;
            lTMP.fontSize = 26;
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.color = TextWhite;

            return btnGO;
        }

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            if (File.Exists("Assets/_Project/Scenes/SplashScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/SplashScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/LoginScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/LoginScene.unity", true));
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
                catch { }
            }
            return TMP_Settings.defaultFontAsset;
        }
    }
}
#endif
