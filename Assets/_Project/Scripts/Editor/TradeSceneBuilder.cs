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
    public static class TradeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TradeScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Design Tokens
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);

        [MenuItem("JuegoTCG/Generar Pantalla de Intercambio (Trade)")]
        public static void BuildTradeScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_back.png");
            Sprite iconSwap = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_swap.png");

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
            GameObject controllerGO = new GameObject("TradeScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            TradeScreenController controller = controllerGO.AddComponent<TradeScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. TOP HEADER (Back Arrow + "INTERCAMBIO")
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

            // Title: "INTERCAMBIO"
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
            titleTMP.text = "INTERCAMBIO";
            titleTMP.fontSize = 46;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // ====================================================
            // 2. TAB FILTER CHIPS ("RECIBIDAS 2", "ENVIADAS")
            // ====================================================
            GameObject tabsHolderGO = new GameObject("TabChipsHolder");
            tabsHolderGO.transform.SetParent(contentGO.transform, false);
            RectTransform tabsHolderRect = tabsHolderGO.AddComponent<RectTransform>();
            tabsHolderRect.anchorMin = new Vector2(0.5f, 1f);
            tabsHolderRect.anchorMax = new Vector2(0.5f, 1f);
            tabsHolderRect.pivot = new Vector2(0.5f, 1f);
            tabsHolderRect.anchoredPosition = new Vector2(0, -155);
            tabsHolderRect.sizeDelta = new Vector2(980, 68);

            HorizontalLayoutGroup thlg = tabsHolderGO.AddComponent<HorizontalLayoutGroup>();
            thlg.childAlignment = TextAnchor.MiddleLeft;
            thlg.childControlWidth = false;
            thlg.childControlHeight = false;
            thlg.childForceExpandWidth = false;
            thlg.childForceExpandHeight = false;
            thlg.spacing = 12f;

            // Tab 1: Recibidas
            GameObject tabRecGO = new GameObject("Tab_Recibidas");
            tabRecGO.transform.SetParent(tabsHolderGO.transform, false);
            RectTransform trRect = tabRecGO.AddComponent<RectTransform>();
            trRect.sizeDelta = new Vector2(255, 64);

            RoundedRectGraphic trG = tabRecGO.AddComponent<RoundedRectGraphic>();
            trG.IsCapsule = true;
            trG.color = Gold;
            trG.BorderWidth = 1.5f;
            trG.BorderColor = Gold;

            Button trBtn = tabRecGO.AddComponent<Button>();

            GameObject trContentGO = new GameObject("Content");
            trContentGO.transform.SetParent(tabRecGO.transform, false);
            RectTransform trcRect = trContentGO.AddComponent<RectTransform>();
            trcRect.anchorMin = Vector2.zero;
            trcRect.anchorMax = Vector2.one;
            trcRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup trchlg = trContentGO.AddComponent<HorizontalLayoutGroup>();
            trchlg.childAlignment = TextAnchor.MiddleCenter;
            trchlg.childControlWidth = false;
            trchlg.childControlHeight = false;
            trchlg.spacing = 8f;

            GameObject trTextGO = new GameObject("Text");
            trTextGO.transform.SetParent(trContentGO.transform, false);
            RectTransform trtRect = trTextGO.AddComponent<RectTransform>();
            trtRect.sizeDelta = new Vector2(145, 34);
            TextMeshProUGUI trTMP = trTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) trTMP.font = barlowTMPFont;
            trTMP.text = "RECIBIDAS";
            trTMP.fontSize = 24;
            trTMP.fontStyle = FontStyles.Bold;
            trTMP.characterSpacing = 3f;
            trTMP.enableWordWrapping = false;
            trTMP.overflowMode = TextOverflowModes.Overflow;
            trTMP.alignment = TextAlignmentOptions.Center;
            trTMP.color = new Color(0.051f, 0.102f, 0.075f);
            trTMP.raycastTarget = false;

            // Badge circle
            GameObject trBadgeGO = new GameObject("Badge");
            trBadgeGO.transform.SetParent(trContentGO.transform, false);
            RectTransform trbRect = trBadgeGO.AddComponent<RectTransform>();
            trbRect.sizeDelta = new Vector2(34, 34);
            RoundedRectGraphic trbG = trBadgeGO.AddComponent<RoundedRectGraphic>();
            trbG.IsCapsule = true;
            trbG.color = new Color(0.051f, 0.102f, 0.075f);
            trbG.raycastTarget = false;

            GameObject trbTextGO = new GameObject("Text");
            trbTextGO.transform.SetParent(trBadgeGO.transform, false);
            RectTransform trbtRect = trbTextGO.AddComponent<RectTransform>();
            trbtRect.anchorMin = Vector2.zero;
            trbtRect.anchorMax = Vector2.one;
            trbtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI trbTMP = trbTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) trbTMP.font = dmSansTMPFont;
            trbTMP.text = "2";
            trbTMP.fontSize = 20;
            trbTMP.fontStyle = FontStyles.Bold;
            trbTMP.alignment = TextAlignmentOptions.Center;
            trbTMP.color = Gold;
            trbTMP.raycastTarget = false;

            // Tab 2: Enviadas
            GameObject tabSentGO = new GameObject("Tab_Enviadas");
            tabSentGO.transform.SetParent(tabsHolderGO.transform, false);
            RectTransform tsRect = tabSentGO.AddComponent<RectTransform>();
            tsRect.sizeDelta = new Vector2(175, 64);

            RoundedRectGraphic tsG = tabSentGO.AddComponent<RoundedRectGraphic>();
            tsG.IsCapsule = true;
            tsG.color = Color.clear;
            tsG.BorderWidth = 1.5f;
            tsG.BorderColor = BorderSubtle;

            Button tsBtn = tabSentGO.AddComponent<Button>();

            GameObject tsTextGO = new GameObject("Text");
            tsTextGO.transform.SetParent(tabSentGO.transform, false);
            RectTransform tstRect = tsTextGO.AddComponent<RectTransform>();
            tstRect.anchorMin = Vector2.zero;
            tstRect.anchorMax = Vector2.one;
            tstRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI tsTMP = tsTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) tsTMP.font = barlowTMPFont;
            tsTMP.text = "ENVIADAS";
            tsTMP.fontSize = 24;
            tsTMP.fontStyle = FontStyles.Bold;
            tsTMP.characterSpacing = 3f;
            tsTMP.enableWordWrapping = false;
            tsTMP.alignment = TextAlignmentOptions.Center;
            tsTMP.color = TextGray;
            tsTMP.raycastTarget = false;

            // ====================================================
            // 3. TRADE OFFERS LIST (Tight Spacing with LayoutElement)
            // ====================================================
            GameObject listHolderGO = new GameObject("OffersList");
            listHolderGO.transform.SetParent(contentGO.transform, false);
            RectTransform listHolderRect = listHolderGO.AddComponent<RectTransform>();
            listHolderRect.anchorMin = new Vector2(0.5f, 1f);
            listHolderRect.anchorMax = new Vector2(0.5f, 1f);
            listHolderRect.pivot = new Vector2(0.5f, 1f);
            listHolderRect.anchoredPosition = new Vector2(0, -240);
            listHolderRect.sizeDelta = new Vector2(980, 920);

            VerticalLayoutGroup vlg = listHolderGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 18f;

            ContentSizeFitter csf = listHolderGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            List<TradeOfferCardView> cardViews = new List<TradeOfferCardView>();
            for (int i = 0; i < 3; i++)
            {
                GameObject cardGO = CreateTradeCardItem(listHolderGO.transform, $"TradeCard_{i}", barlowTMPFont, dmSansTMPFont, iconSwap);
                cardViews.Add(cardGO.GetComponent<TradeOfferCardView>());
            }

            // ====================================================
            // 4. FLOATING CTA BUTTON (+ NUEVO INTERCAMBIO)
            // ====================================================
            GameObject newTradeBtnGO = new GameObject("NewTradeFloatingButton");
            newTradeBtnGO.transform.SetParent(canvasGO.transform, false);
            RectTransform ntbRect = newTradeBtnGO.AddComponent<RectTransform>();
            ntbRect.anchorMin = new Vector2(1f, 0f);
            ntbRect.anchorMax = new Vector2(1f, 0f);
            ntbRect.pivot = new Vector2(1f, 0f);
            ntbRect.anchoredPosition = new Vector2(-40, 205);
            ntbRect.sizeDelta = new Vector2(360, 78);

            RoundedRectGraphic ntbG = newTradeBtnGO.AddComponent<RoundedRectGraphic>();
            ntbG.IsCapsule = true;
            ntbG.color = Gold;
            ntbG.BorderWidth = 1.8f;
            ntbG.BorderColor = Gold;

            Button ntbBtn = newTradeBtnGO.AddComponent<Button>();

            GameObject ntbTextGO = new GameObject("Text");
            ntbTextGO.transform.SetParent(newTradeBtnGO.transform, false);
            RectTransform ntbtRect = ntbTextGO.AddComponent<RectTransform>();
            ntbtRect.anchorMin = Vector2.zero;
            ntbtRect.anchorMax = Vector2.one;
            ntbtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI ntbtTMP = ntbTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) ntbtTMP.font = barlowTMPFont;
            ntbtTMP.text = "+ NUEVO INTERCAMBIO";
            ntbtTMP.fontSize = 24;
            ntbtTMP.fontStyle = FontStyles.Bold;
            ntbtTMP.characterSpacing = 4f;
            ntbtTMP.alignment = TextAlignmentOptions.Center;
            ntbtTMP.color = new Color(0.051f, 0.102f, 0.075f);
            ntbtTMP.raycastTarget = false;

            // ====================================================
            // 5. BOTTOM NAVIGATION BAR (5 Tabs, "Comunidad" Active)
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
                bool isTabActive = (i == 3); // Comunidad is Tab 3 (Active)
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

            // Assign Serialized Properties on TradeScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("tabReceivedButton").objectReferenceValue = trBtn;
            so.FindProperty("tabSentButton").objectReferenceValue = tsBtn;
            so.FindProperty("tabReceivedGraphic").objectReferenceValue = trG;
            so.FindProperty("tabSentGraphic").objectReferenceValue = tsG;
            so.FindProperty("tabReceivedText").objectReferenceValue = trTMP;
            so.FindProperty("tabSentText").objectReferenceValue = tsTMP;
            so.FindProperty("unreadBadgeGO").objectReferenceValue = trBadgeGO;
            so.FindProperty("unreadBadgeText").objectReferenceValue = trbTMP;
            so.FindProperty("newTradeButton").objectReferenceValue = ntbBtn;

            SerializedProperty cardsProp = so.FindProperty("offerCardViews");
            cardsProp.arraySize = cardViews.Count;
            for (int i = 0; i < cardViews.Count; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardViews[i];

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/TradeScreenUI.prefab";
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
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Intercambio guardada como Escena Oficial (TradeScene.unity) y Prefab (TradeScreenUI.prefab)!</color>");
        }

        private static GameObject CreateTradeCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont, Sprite swapSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(980, 275);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 20f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.5f;
            cardG.BorderColor = GoldBorder;

            LayoutElement le = cardGO.AddComponent<LayoutElement>();
            le.minHeight = 280f;
            le.preferredHeight = 280f;
            le.flexibleHeight = 0f;

            TradeOfferCardView cardView = cardGO.AddComponent<TradeOfferCardView>();

            // Unread Dot (Top Right)
            GameObject dotGO = new GameObject("UnreadDot");
            dotGO.transform.SetParent(cardGO.transform, false);
            RectTransform dotRect = dotGO.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(1f, 1f);
            dotRect.anchorMax = new Vector2(1f, 1f);
            dotRect.pivot = new Vector2(1f, 1f);
            dotRect.anchoredPosition = new Vector2(-22, -22);
            dotRect.sizeDelta = new Vector2(14, 14);
            RoundedRectGraphic dotG = dotGO.AddComponent<RoundedRectGraphic>();
            dotG.IsCapsule = true;
            dotG.color = Gold;
            dotG.raycastTarget = false;

            // User Info Header
            GameObject uHeadGO = new GameObject("UserHeader");
            uHeadGO.transform.SetParent(cardGO.transform, false);
            RectTransform uhRect = uHeadGO.AddComponent<RectTransform>();
            uhRect.anchorMin = new Vector2(0f, 1f);
            uhRect.anchorMax = new Vector2(1f, 1f);
            uhRect.pivot = new Vector2(0f, 1f);
            uhRect.anchoredPosition = new Vector2(24, -16);
            uhRect.sizeDelta = new Vector2(-70, 46);

            // Avatar circle
            GameObject avGO = new GameObject("AvatarCircle");
            avGO.transform.SetParent(uHeadGO.transform, false);
            RectTransform avRect = avGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(0, 0);
            avRect.sizeDelta = new Vector2(44, 44);
            RoundedRectGraphic avG = avGO.AddComponent<RoundedRectGraphic>();
            avG.IsCapsule = true;
            avG.color = new Color(1f, 1f, 1f, 0.08f);
            avG.BorderWidth = 1.2f;
            avG.BorderColor = BorderSubtle;
            avG.raycastTarget = false;

            GameObject avTextGO = new GameObject("Text");
            avTextGO.transform.SetParent(avGO.transform, false);
            RectTransform avtRect = avTextGO.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI avTMP = avTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) avTMP.font = dmSansFont;
            avTMP.text = "MA";
            avTMP.fontSize = 18;
            avTMP.fontStyle = FontStyles.Bold;
            avTMP.alignment = TextAlignmentOptions.Center;
            avTMP.color = TextWhite;
            avTMP.raycastTarget = false;

            // Name
            GameObject nameGO = new GameObject("UserNameText");
            nameGO.transform.SetParent(uHeadGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0.6f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(56, 0);
            nameRect.sizeDelta = new Vector2(0, 36);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) nameTMP.font = dmSansFont;
            nameTMP.text = "MiAmigo_01";
            nameTMP.fontSize = 24;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = TextWhite;
            nameTMP.raycastTarget = false;

            // Time
            GameObject timeGO = new GameObject("TimeText");
            timeGO.transform.SetParent(uHeadGO.transform, false);
            RectTransform timeRect = timeGO.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(1f, 0.5f);
            timeRect.anchorMax = new Vector2(1f, 0.5f);
            timeRect.pivot = new Vector2(1f, 0.5f);
            timeRect.anchoredPosition = new Vector2(-20, 0);
            timeRect.sizeDelta = new Vector2(120, 30);
            TextMeshProUGUI timeTMP = timeGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) timeTMP.font = dmSansFont;
            timeTMP.text = "hace 2 h";
            timeTMP.fontSize = 20;
            timeTMP.alignment = TextAlignmentOptions.Right;
            timeTMP.color = TextDim;
            timeTMP.raycastTarget = false;

            // Exchange Row (Middle)
            GameObject exchRowGO = new GameObject("ExchangeRow");
            exchRowGO.transform.SetParent(cardGO.transform, false);
            RectTransform erRect = exchRowGO.AddComponent<RectTransform>();
            erRect.anchorMin = new Vector2(0.5f, 1f);
            erRect.anchorMax = new Vector2(0.5f, 1f);
            erRect.pivot = new Vector2(0.5f, 1f);
            erRect.anchoredPosition = new Vector2(0, -66);
            erRect.sizeDelta = new Vector2(932, 110);

            // You Give Column (Left)
            GameObject ygColGO = new GameObject("YouGiveCol");
            ygColGO.transform.SetParent(exchRowGO.transform, false);
            RectTransform ygcRect = ygColGO.AddComponent<RectTransform>();
            ygcRect.anchorMin = new Vector2(0f, 0f);
            ygcRect.anchorMax = new Vector2(0.42f, 1f);
            ygcRect.sizeDelta = Vector2.zero;

            GameObject ygLabelGO = new GameObject("Label");
            ygLabelGO.transform.SetParent(ygColGO.transform, false);
            RectTransform yglRect = ygLabelGO.AddComponent<RectTransform>();
            yglRect.anchorMin = new Vector2(0f, 1f);
            yglRect.anchorMax = new Vector2(1f, 1f);
            yglRect.pivot = new Vector2(0f, 1f);
            yglRect.anchoredPosition = new Vector2(0, 0);
            yglRect.sizeDelta = new Vector2(0, 24);
            TextMeshProUGUI yglTMP = ygLabelGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) yglTMP.font = dmSansFont;
            yglTMP.text = "TÚ DAS";
            yglTMP.fontSize = 17;
            yglTMP.fontStyle = FontStyles.Bold;
            yglTMP.characterSpacing = 3f;
            yglTMP.color = TextDim;

            GameObject ygCardsHolderGO = new GameObject("CardsHolder");
            ygCardsHolderGO.transform.SetParent(ygColGO.transform, false);
            RectTransform ygchRect = ygCardsHolderGO.AddComponent<RectTransform>();
            ygchRect.anchorMin = new Vector2(0f, 0f);
            ygchRect.anchorMax = new Vector2(1f, 1f);
            ygchRect.anchoredPosition = new Vector2(0, -10);
            ygchRect.sizeDelta = new Vector2(0, -24);            HorizontalLayoutGroup yghlg = ygCardsHolderGO.AddComponent<HorizontalLayoutGroup>();
            yghlg.childAlignment = TextAnchor.MiddleLeft;
            yghlg.childControlWidth = false;
            yghlg.childControlHeight = false;
            yghlg.childForceExpandWidth = false;
            yghlg.childForceExpandHeight = false;
            yghlg.spacing = 8f;

            for (int k = 0; k < 2; k++) CreateMiniCardBox(ygCardsHolderGO.transform, $"GiveCard_{k}", dmSansFont);

            // Swap Icon Center
            GameObject swapIconGO = new GameObject("SwapIcon");
            swapIconGO.transform.SetParent(exchRowGO.transform, false);
            RectTransform siRect = swapIconGO.AddComponent<RectTransform>();
            siRect.anchorMin = new Vector2(0.5f, 0.45f);
            siRect.anchorMax = new Vector2(0.5f, 0.45f);
            siRect.pivot = new Vector2(0.5f, 0.5f);
            siRect.sizeDelta = new Vector2(38, 38);
            Image siImg = swapIconGO.AddComponent<Image>();
            if (swapSprite != null) siImg.sprite = swapSprite;
            siImg.color = new Color(1f, 1f, 1f, 0.35f);
            siImg.raycastTarget = false;

            // You Receive Column (Right)
            GameObject yrColGO = new GameObject("YouReceiveCol");
            yrColGO.transform.SetParent(exchRowGO.transform, false);
            RectTransform yrcRect = yrColGO.AddComponent<RectTransform>();
            yrcRect.anchorMin = new Vector2(0.58f, 0f);
            yrcRect.anchorMax = new Vector2(1f, 1f);
            yrcRect.sizeDelta = Vector2.zero;

            GameObject yrLabelGO = new GameObject("Label");
            yrLabelGO.transform.SetParent(yrColGO.transform, false);
            RectTransform yrlRect = yrLabelGO.AddComponent<RectTransform>();
            yrlRect.anchorMin = new Vector2(0f, 1f);
            yrlRect.anchorMax = new Vector2(1f, 1f);
            yrlRect.pivot = new Vector2(0f, 1f);
            yrlRect.anchoredPosition = new Vector2(0, 0);
            yrlRect.sizeDelta = new Vector2(0, 24);
            TextMeshProUGUI yrlTMP = yrLabelGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) yrlTMP.font = dmSansFont;
            yrlTMP.text = "TÚ RECIBES";
            yrlTMP.fontSize = 17;
            yrlTMP.fontStyle = FontStyles.Bold;
            yrlTMP.characterSpacing = 3f;
            yrlTMP.alignment = TextAlignmentOptions.Right;
            yrlTMP.color = TextDim;

            GameObject yrCardsHolderGO = new GameObject("CardsHolder");
            yrCardsHolderGO.transform.SetParent(yrColGO.transform, false);
            RectTransform yrchRect = yrCardsHolderGO.AddComponent<RectTransform>();
            yrchRect.anchorMin = Vector2.zero;
            yrchRect.anchorMax = Vector2.one;
            yrchRect.anchoredPosition = new Vector2(0, -10);
            yrchRect.sizeDelta = new Vector2(0, -24);

            HorizontalLayoutGroup yrhlg = yrCardsHolderGO.AddComponent<HorizontalLayoutGroup>();
            yrhlg.childAlignment = TextAnchor.MiddleRight;
            yrhlg.childControlWidth = false;
            yrhlg.childControlHeight = false;
            yrhlg.childForceExpandWidth = false;
            yrhlg.childForceExpandHeight = false;
            yrhlg.spacing = 8f;

            for (int k = 0; k < 2; k++) CreateMiniCardBox(yrCardsHolderGO.transform, $"ReceiveCard_{k}", dmSansFont);

            // Action Buttons: Received Mode (ACEPTAR / RECHAZAR)
            GameObject actionsGO = new GameObject("ReceivedActions");
            actionsGO.transform.SetParent(cardGO.transform, false);
            RectTransform actRect = actionsGO.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.5f, 0f);
            actRect.anchorMax = new Vector2(0.5f, 0f);
            actRect.pivot = new Vector2(0.5f, 0f);
            actRect.anchoredPosition = new Vector2(0, 16);
            actRect.sizeDelta = new Vector2(932, 60);

            HorizontalLayoutGroup acthlg = actionsGO.AddComponent<HorizontalLayoutGroup>();
            acthlg.childAlignment = TextAnchor.MiddleCenter;
            acthlg.childControlWidth = true;
            acthlg.childControlHeight = true;
            acthlg.childForceExpandWidth = true;
            acthlg.childForceExpandHeight = true;
            acthlg.spacing = 16f;

            // ACEPTAR Button
            GameObject accBtnGO = new GameObject("AcceptButton");
            accBtnGO.transform.SetParent(actionsGO.transform, false);
            RoundedRectGraphic accG = accBtnGO.AddComponent<RoundedRectGraphic>();
            accG.CornerRadius = 14f;
            accG.color = Gold;
            Button accBtn = accBtnGO.AddComponent<Button>();

            GameObject accTextGO = new GameObject("Text");
            accTextGO.transform.SetParent(accBtnGO.transform, false);
            RectTransform acctRect = accTextGO.AddComponent<RectTransform>();
            acctRect.anchorMin = Vector2.zero;
            acctRect.anchorMax = Vector2.one;
            acctRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI acctTMP = accTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) acctTMP.font = barlowFont;
            acctTMP.text = "ACEPTAR";
            acctTMP.fontSize = 24;
            acctTMP.fontStyle = FontStyles.Bold;
            acctTMP.characterSpacing = 4f;
            acctTMP.alignment = TextAlignmentOptions.Center;
            acctTMP.color = new Color(0.051f, 0.102f, 0.075f);

            // RECHAZAR Button
            GameObject rejBtnGO = new GameObject("RejectButton");
            rejBtnGO.transform.SetParent(actionsGO.transform, false);
            RoundedRectGraphic rejG = rejBtnGO.AddComponent<RoundedRectGraphic>();
            rejG.CornerRadius = 14f;
            rejG.color = new Color(1f, 1f, 1f, 0.05f);
            rejG.BorderWidth = 1.2f;
            rejG.BorderColor = BorderSubtle;
            Button rejBtn = rejBtnGO.AddComponent<Button>();

            GameObject rejTextGO = new GameObject("Text");
            rejTextGO.transform.SetParent(rejBtnGO.transform, false);
            RectTransform rejtRect = rejTextGO.AddComponent<RectTransform>();
            rejtRect.anchorMin = Vector2.zero;
            rejtRect.anchorMax = Vector2.one;
            rejtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rejtTMP = rejTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rejtTMP.font = barlowFont;
            rejtTMP.text = "RECHAZAR";
            rejtTMP.fontSize = 24;
            rejtTMP.fontStyle = FontStyles.Bold;
            rejtTMP.characterSpacing = 4f;
            rejtTMP.alignment = TextAlignmentOptions.Center;
            rejtTMP.color = TextGray;

            // Action Buttons: Sent Mode (CANCELAR OFERTA)
            GameObject sentActionsGO = new GameObject("SentActions");
            sentActionsGO.transform.SetParent(cardGO.transform, false);
            RectTransform sActRect = sentActionsGO.AddComponent<RectTransform>();
            sActRect.anchorMin = new Vector2(0.5f, 0f);
            sActRect.anchorMax = new Vector2(0.5f, 0f);
            sActRect.pivot = new Vector2(0.5f, 0f);
            sActRect.anchoredPosition = new Vector2(0, 16);
            sActRect.sizeDelta = new Vector2(932, 60);

            RoundedRectGraphic sActG = sentActionsGO.AddComponent<RoundedRectGraphic>();
            sActG.CornerRadius = 14f;
            sActG.color = new Color(1f, 1f, 1f, 0.05f);
            sActG.BorderWidth = 1.2f;
            sActG.BorderColor = new Color(1f, 1f, 1f, 0.20f);
            Button cancelBtn = sentActionsGO.AddComponent<Button>();

            GameObject cancelTextGO = new GameObject("Text");
            cancelTextGO.transform.SetParent(sentActionsGO.transform, false);
            RectTransform ctRect = cancelTextGO.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI ctTMP = cancelTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) ctTMP.font = barlowFont;
            ctTMP.text = "CANCELAR OFERTA";
            ctTMP.fontSize = 24;
            ctTMP.fontStyle = FontStyles.Bold;
            ctTMP.characterSpacing = 4f;
            ctTMP.alignment = TextAlignmentOptions.Center;
            ctTMP.color = TextGray;

            sentActionsGO.SetActive(false);

            // Serialize TradeOfferCardView
            SerializedObject toSO = new SerializedObject(cardView);
            toSO.FindProperty("userNameText").objectReferenceValue = nameTMP;
            toSO.FindProperty("avatarText").objectReferenceValue = avTMP;
            toSO.FindProperty("timeText").objectReferenceValue = timeTMP;
            toSO.FindProperty("unreadDot").objectReferenceValue = dotGO;
            toSO.FindProperty("youGiveParent").objectReferenceValue = ygCardsHolderGO.transform;
            toSO.FindProperty("youReceiveParent").objectReferenceValue = yrCardsHolderGO.transform;
            toSO.FindProperty("receivedActionsGroup").objectReferenceValue = actionsGO;
            toSO.FindProperty("sentActionsGroup").objectReferenceValue = sentActionsGO;
            toSO.FindProperty("acceptButton").objectReferenceValue = accBtn;
            toSO.FindProperty("rejectButton").objectReferenceValue = rejBtn;
            toSO.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            toSO.ApplyModifiedProperties();

            return cardGO;
        }

        private static void CreateMiniCardBox(Transform parent, string name, TMP_FontAsset font)
        {
            GameObject miniGO = new GameObject(name);
            miniGO.transform.SetParent(parent, false);
            RectTransform mRect = miniGO.AddComponent<RectTransform>();
            mRect.sizeDelta = new Vector2(58, 75);

            RoundedRectGraphic mg = miniGO.AddComponent<RoundedRectGraphic>();
            mg.CornerRadius = 10f;
            mg.color = new Color(0.035f, 0.07f, 0.05f);
            mg.BorderWidth = 2.0f;
            mg.BorderColor = Gold;
            mg.raycastTarget = false;

            GameObject textGO = new GameObject("Initials");
            textGO.transform.SetParent(miniGO.transform, false);
            RectTransform tRect = textGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI tTMP = textGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tTMP.font = font;
            tTMP.text = "MÍ";
            tTMP.fontSize = 18;
            tTMP.fontStyle = FontStyles.Bold;
            tTMP.alignment = TextAlignmentOptions.Center;
            tTMP.color = Gold;
            tTMP.raycastTarget = false;
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
