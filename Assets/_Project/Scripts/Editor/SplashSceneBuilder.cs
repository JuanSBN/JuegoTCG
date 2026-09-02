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
    public static class SplashSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/SplashScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Design Tokens
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.70f);
        private static readonly Color TrackBg = new Color(1f, 1f, 1f, 0.12f);

        [MenuItem("JuegoTCG/Generar Pantalla de Splash (SplashScene)")]
        public static void BuildSplashScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite logoCardsSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_logo_cards.png");

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
            GameObject controllerGO = new GameObject("SplashScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            SplashScreenController controller = controllerGO.AddComponent<SplashScreenController>();

            // Central Container
            GameObject centerGO = new GameObject("CenterContainer");
            centerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform centerRect = centerGO.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = new Vector2(0, 30);
            centerRect.sizeDelta = new Vector2(900, 800);

            // ====================================================
            // 1. LOGO SLOT (Logo del juego / Card mark)
            // ====================================================
            GameObject logoSlotGO = new GameObject("GameLogoSlot");
            logoSlotGO.transform.SetParent(centerGO.transform, false);
            RectTransform logoRect = logoSlotGO.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 1f);
            logoRect.anchorMax = new Vector2(0.5f, 1f);
            logoRect.pivot = new Vector2(0.5f, 1f);
            logoRect.anchoredPosition = new Vector2(0, -40);
            logoRect.sizeDelta = new Vector2(300, 300);

            GameObject logoImgGO = new GameObject("CardLogoIcon");
            logoImgGO.transform.SetParent(logoSlotGO.transform, false);
            RectTransform liRect = logoImgGO.AddComponent<RectTransform>();
            liRect.anchorMin = new Vector2(0.5f, 0.5f);
            liRect.anchorMax = new Vector2(0.5f, 0.5f);
            liRect.anchoredPosition = new Vector2(0, 0);
            liRect.sizeDelta = new Vector2(250, 250);
            Image logoImg = logoImgGO.AddComponent<Image>();
            if (logoCardsSprite != null) logoImg.sprite = logoCardsSprite;
            logoImg.preserveAspect = true;

            // ====================================================
            // 2. STATUS TEXT ("Cargando sesión...")
            // ====================================================
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(centerGO.transform, false);
            RectTransform statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0, 110);
            statusRect.sizeDelta = new Vector2(800, 40);
            TextMeshProUGUI statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) statusTMP.font = dmSansTMPFont;
            statusTMP.text = "Cargando sesión...";
            statusTMP.fontSize = 24;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = TextGray;

            // ====================================================
            // 3. PROGRESS BAR
            // ====================================================
            GameObject sliderGO = new GameObject("ProgressBar");
            sliderGO.transform.SetParent(centerGO.transform, false);
            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0f);
            sliderRect.anchorMax = new Vector2(0.5f, 0f);
            sliderRect.pivot = new Vector2(0.5f, 0f);
            sliderRect.anchoredPosition = new Vector2(0, 60);
            sliderRect.sizeDelta = new Vector2(600, 14);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Background Track
            GameObject sbgGO = new GameObject("Background");
            sbgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform sbgRect = sbgGO.AddComponent<RectTransform>();
            sbgRect.anchorMin = Vector2.zero;
            sbgRect.anchorMax = Vector2.one;
            sbgRect.sizeDelta = Vector2.zero;
            RoundedRectGraphic sbgG = sbgGO.AddComponent<RoundedRectGraphic>();
            sbgG.IsCapsule = true;
            sbgG.color = TrackBg;

            // Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform faRect = fillAreaGO.AddComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fRect = fillGO.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            RoundedRectGraphic fillG = fillGO.AddComponent<RoundedRectGraphic>();
            fillG.IsCapsule = true;
            fillG.color = Gold;

            slider.fillRect = fRect;

            // Assign Serialized Properties
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("logoSlotContainer").objectReferenceValue = logoRect;
            so.FindProperty("logoCardIcon").objectReferenceValue = logoImg;
            so.FindProperty("statusText").objectReferenceValue = statusTMP;
            so.FindProperty("progressBar").objectReferenceValue = slider;
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            PrefabUtility.SaveAsPrefabAsset(canvasGO, $"{prefabDir}/SplashScreenUI.prefab");

            // Save Scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Update Build Settings (SplashScene as index 0!)
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Splash guardada con éxito (SplashScene.unity) como Escena 0!</color>");
        }

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            // Escena 0: SplashScene
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
