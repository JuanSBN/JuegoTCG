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
    public static class StoreSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/StoreScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Exact Design Tokens from docs/Pantallas 2.0/src/App.tsx
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.50f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);   // rgba(14,32,22,0.85)

        [MenuItem("JuegoTCG/Generar Pantalla de Tienda (Store)")]
        public static void BuildStoreScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load and create persistent SDF Font Assets
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_circle.png");
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_coin.png");
            Sprite playSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_play.png");

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
            cam.backgroundColor = new Color(0.051f, 0.082f, 0.125f); // #0d1520
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
            GameObject controllerGO = new GameObject("StoreScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            StoreScreenController controller = controllerGO.AddComponent<StoreScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. TOP HEADER ("TIENDA" + CoinChip)
            // ====================================================
            GameObject topBarGO = new GameObject("TopHeader");
            topBarGO.transform.SetParent(contentGO.transform, false);
            RectTransform topBarRect = topBarGO.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0.5f, 1f);
            topBarRect.anchorMax = new Vector2(0.5f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0, -60);
            topBarRect.sizeDelta = new Vector2(980, 100);

            // Title Left: "TIENDA"
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(topBarGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 0);
            titleRect.sizeDelta = new Vector2(400, 80);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "TIENDA";
            titleTMP.fontSize = 50;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // CoinChip Right: Pill
            GameObject coinChipGO = new GameObject("CoinChip");
            coinChipGO.transform.SetParent(topBarGO.transform, false);
            RectTransform chipRect = coinChipGO.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(1f, 0.5f);
            chipRect.anchorMax = new Vector2(1f, 0.5f);
            chipRect.pivot = new Vector2(1f, 0.5f);
            chipRect.anchoredPosition = new Vector2(0, 0);
            chipRect.sizeDelta = new Vector2(180, 68);

            RoundedRectGraphic chipG = coinChipGO.AddComponent<RoundedRectGraphic>();
            chipG.IsCapsule = true;
            chipG.color = new Color(0f, 0f, 0f, 0.50f);
            chipG.BorderWidth = 1.8f;
            chipG.BorderColor = GoldBorder;

            GameObject chipCoinIconGO = new GameObject("CoinIcon");
            chipCoinIconGO.transform.SetParent(coinChipGO.transform, false);
            RectTransform chipIconRect = chipCoinIconGO.AddComponent<RectTransform>();
            chipIconRect.anchorMin = new Vector2(0f, 0.5f);
            chipIconRect.anchorMax = new Vector2(0f, 0.5f);
            chipIconRect.pivot = new Vector2(0f, 0.5f);
            chipIconRect.anchoredPosition = new Vector2(16, 0);
            chipIconRect.sizeDelta = new Vector2(36, 36);
            Image chipCoinImg = chipCoinIconGO.AddComponent<Image>();
            if (coinSprite != null) chipCoinImg.sprite = coinSprite;

            GameObject chipTextGO = new GameObject("CoinsText");
            chipTextGO.transform.SetParent(coinChipGO.transform, false);
            RectTransform chipTextRect = chipTextGO.AddComponent<RectTransform>();
            chipTextRect.anchorMin = new Vector2(0f, 0f);
            chipTextRect.anchorMax = new Vector2(1f, 1f);
            chipTextRect.anchoredPosition = new Vector2(28, 0);
            chipTextRect.sizeDelta = new Vector2(0, 0);
            TextMeshProUGUI coinsTMP = chipTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) coinsTMP.font = dmSansTMPFont;
            coinsTMP.text = "240";
            coinsTMP.fontSize = 28;
            coinsTMP.fontStyle = FontStyles.Bold;
            coinsTMP.alignment = TextAlignmentOptions.Center;
            coinsTMP.color = TextWhite;

            // ====================================================
            // 2. SECTION 1: "COMPRAR SOBRES"
            // ====================================================
            GameObject sec1GO = new GameObject("Section_Packs");
            sec1GO.transform.SetParent(contentGO.transform, false);
            RectTransform sec1Rect = sec1GO.AddComponent<RectTransform>();
            sec1Rect.anchorMin = new Vector2(0.5f, 1f);
            sec1Rect.anchorMax = new Vector2(0.5f, 1f);
            sec1Rect.pivot = new Vector2(0.5f, 1f);
            sec1Rect.anchoredPosition = new Vector2(0, -180);
            sec1Rect.sizeDelta = new Vector2(980, 560);

            GameObject sec1TitleGO = new GameObject("SectionTitle");
            sec1TitleGO.transform.SetParent(sec1GO.transform, false);
            RectTransform sec1TitleRect = sec1TitleGO.AddComponent<RectTransform>();
            sec1TitleRect.anchorMin = new Vector2(0f, 1f);
            sec1TitleRect.anchorMax = new Vector2(1f, 1f);
            sec1TitleRect.pivot = new Vector2(0f, 1f);
            sec1TitleRect.anchoredPosition = new Vector2(0, 0);
            sec1TitleRect.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI sec1TitleTMP = sec1TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec1TitleTMP.font = barlowTMPFont;
            sec1TitleTMP.text = "COMPRAR SOBRES";
            sec1TitleTMP.fontSize = 30;
            sec1TitleTMP.fontStyle = FontStyles.Bold;
            sec1TitleTMP.characterSpacing = 8f;
            sec1TitleTMP.color = TextWhite;

            string[] packLabels = { "SOBRE A", "SOBRE B", "SOBRE C" };
            int[] packPrices = { 100, 300, 600 };
            Button[] packBtns = new Button[3];
            float packSpacing = 335f;
            float startPackX = -packSpacing; // -335, 0, 335

            for (int i = 0; i < 3; i++)
            {
                bool isFeatured = (i == 1);
                GameObject packHolderGO = new GameObject($"PackHolder_{i}");
                packHolderGO.transform.SetParent(sec1GO.transform, false);
                RectTransform phRect = packHolderGO.AddComponent<RectTransform>();
                phRect.anchorMin = new Vector2(0.5f, 1f);
                phRect.anchorMax = new Vector2(0.5f, 1f);
                phRect.pivot = new Vector2(0.5f, 1f);
                phRect.anchoredPosition = new Vector2(startPackX + i * packSpacing, -60);
                phRect.sizeDelta = new Vector2(305, 480);

                // Envelope Card (3:4 ratio)
                GameObject envCardGO = new GameObject("EnvelopeCard");
                envCardGO.transform.SetParent(packHolderGO.transform, false);
                RectTransform envRect = envCardGO.AddComponent<RectTransform>();
                envRect.anchorMin = new Vector2(0.5f, 1f);
                envRect.anchorMax = new Vector2(0.5f, 1f);
                envRect.pivot = new Vector2(0.5f, 1f);
                envRect.anchoredPosition = new Vector2(0, 0);
                envRect.sizeDelta = new Vector2(305, 390);

                RoundedRectGraphic envG = envCardGO.AddComponent<RoundedRectGraphic>();
                envG.CornerRadius = 20f;
                envG.color = CardBg;
                envG.BorderWidth = isFeatured ? 2.5f : 1.5f;
                envG.BorderColor = isFeatured ? GoldBorder : BorderSubtle;

                // Envelope Label at bottom of card
                GameObject envLabelGO = new GameObject("Label");
                envLabelGO.transform.SetParent(envCardGO.transform, false);
                RectTransform envLabelRect = envLabelGO.AddComponent<RectTransform>();
                envLabelRect.anchorMin = new Vector2(0f, 0f);
                envLabelRect.anchorMax = new Vector2(1f, 0.22f);
                envLabelRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI envLabelTMP = envLabelGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) envLabelTMP.font = dmSansTMPFont;
                envLabelTMP.text = packLabels[i];
                envLabelTMP.fontSize = 22;
                envLabelTMP.fontStyle = FontStyles.Bold;
                envLabelTMP.alignment = TextAlignmentOptions.Center;
                envLabelTMP.color = isFeatured ? Gold : TextGray;

                // Price Button Below Card
                GameObject priceBtnGO = new GameObject("PriceButton");
                priceBtnGO.transform.SetParent(packHolderGO.transform, false);
                RectTransform priceRect = priceBtnGO.AddComponent<RectTransform>();
                priceRect.anchorMin = new Vector2(0.5f, 0f);
                priceRect.anchorMax = new Vector2(0.5f, 0f);
                priceRect.pivot = new Vector2(0.5f, 0f);
                priceRect.anchoredPosition = new Vector2(0, 0);
                priceRect.sizeDelta = new Vector2(305, 76);

                RoundedRectGraphic priceG = priceBtnGO.AddComponent<RoundedRectGraphic>();
                priceG.CornerRadius = 14f;
                priceG.color = isFeatured ? new Color(0.910f, 0.659f, 0.125f, 0.12f) : new Color(1f, 1f, 1f, 0.05f);
                priceG.BorderWidth = 1.5f;
                priceG.BorderColor = isFeatured ? GoldBorder : BorderSubtle;

                packBtns[i] = priceBtnGO.AddComponent<Button>();

                // Price icon + text centered
                GameObject pContentGO = new GameObject("PriceContent");
                pContentGO.transform.SetParent(priceBtnGO.transform, false);
                RectTransform pcRect = pContentGO.AddComponent<RectTransform>();
                pcRect.anchorMin = Vector2.zero;
                pcRect.anchorMax = Vector2.one;
                pcRect.sizeDelta = Vector2.zero;

                HorizontalLayoutGroup phlg = pContentGO.AddComponent<HorizontalLayoutGroup>();
                phlg.childAlignment = TextAnchor.MiddleCenter;
                phlg.childControlWidth = false;
                phlg.childControlHeight = false;
                phlg.childForceExpandWidth = false;
                phlg.childForceExpandHeight = false;
                phlg.spacing = 10f;

                GameObject pIconGO = new GameObject("CoinIcon");
                pIconGO.transform.SetParent(pContentGO.transform, false);
                RectTransform pIconRect = pIconGO.AddComponent<RectTransform>();
                pIconRect.sizeDelta = new Vector2(32, 32);
                Image pIconImg = pIconGO.AddComponent<Image>();
                if (coinSprite != null) pIconImg.sprite = coinSprite;
                pIconImg.raycastTarget = false;

                GameObject pTextGO = new GameObject("PriceText");
                pTextGO.transform.SetParent(pContentGO.transform, false);
                RectTransform pTextRect = pTextGO.AddComponent<RectTransform>();
                pTextRect.sizeDelta = new Vector2(100, 36);
                TextMeshProUGUI pTMP = pTextGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) pTMP.font = dmSansTMPFont;
                pTMP.text = packPrices[i].ToString();
                pTMP.fontSize = 26;
                pTMP.fontStyle = FontStyles.Bold;
                pTMP.alignment = TextAlignmentOptions.Left;
                pTMP.color = isFeatured ? Gold : TextWhite;
                pTMP.raycastTarget = false;
            }

            // ====================================================
            // 3. SECTION 2: "VER ANUNCIO"
            // ====================================================
            GameObject sec2GO = new GameObject("Section_WatchAd");
            sec2GO.transform.SetParent(contentGO.transform, false);
            RectTransform sec2Rect = sec2GO.AddComponent<RectTransform>();
            sec2Rect.anchorMin = new Vector2(0.5f, 1f);
            sec2Rect.anchorMax = new Vector2(0.5f, 1f);
            sec2Rect.pivot = new Vector2(0.5f, 1f);
            sec2Rect.anchoredPosition = new Vector2(0, -780);
            sec2Rect.sizeDelta = new Vector2(980, 240);

            GameObject sec2TitleGO = new GameObject("SectionTitle");
            sec2TitleGO.transform.SetParent(sec2GO.transform, false);
            RectTransform sec2TitleRect = sec2TitleGO.AddComponent<RectTransform>();
            sec2TitleRect.anchorMin = new Vector2(0f, 1f);
            sec2TitleRect.anchorMax = new Vector2(1f, 1f);
            sec2TitleRect.pivot = new Vector2(0f, 1f);
            sec2TitleRect.anchoredPosition = new Vector2(0, 0);
            sec2TitleRect.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI sec2TitleTMP = sec2TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec2TitleTMP.font = barlowTMPFont;
            sec2TitleTMP.text = "VER ANUNCIO";
            sec2TitleTMP.fontSize = 30;
            sec2TitleTMP.fontStyle = FontStyles.Bold;
            sec2TitleTMP.characterSpacing = 8f;
            sec2TitleTMP.color = TextWhite;

            // Interactive Ad Card
            GameObject adCardGO = new GameObject("WatchAdCard");
            adCardGO.transform.SetParent(sec2GO.transform, false);
            RectTransform adCardRect = adCardGO.AddComponent<RectTransform>();
            adCardRect.anchorMin = new Vector2(0.5f, 1f);
            adCardRect.anchorMax = new Vector2(0.5f, 1f);
            adCardRect.pivot = new Vector2(0.5f, 1f);
            adCardRect.anchoredPosition = new Vector2(0, -50);
            adCardRect.sizeDelta = new Vector2(980, 180);

            RoundedRectGraphic adG = adCardGO.AddComponent<RoundedRectGraphic>();
            adG.CornerRadius = 24f;
            adG.color = new Color(0.055f, 0.086f, 0.039f, 0.90f); // rgba(14,22,10,0.9)
            adG.BorderWidth = 2.0f;
            adG.BorderColor = Gold;

            Button adBtn = adCardGO.AddComponent<Button>();

            // Play Circle Button (Left)
            GameObject playCircleGO = new GameObject("PlayCircle");
            playCircleGO.transform.SetParent(adCardGO.transform, false);
            RectTransform playCircleRect = playCircleGO.AddComponent<RectTransform>();
            playCircleRect.anchorMin = new Vector2(0f, 0.5f);
            playCircleRect.anchorMax = new Vector2(0f, 0.5f);
            playCircleRect.pivot = new Vector2(0f, 0.5f);
            playCircleRect.anchoredPosition = new Vector2(30, 0);
            playCircleRect.sizeDelta = new Vector2(100, 100);

            RoundedRectGraphic playCircleG = playCircleGO.AddComponent<RoundedRectGraphic>();
            playCircleG.IsCapsule = true;
            playCircleG.color = Gold;
            playCircleG.raycastTarget = false;

            GameObject playIconGO = new GameObject("PlayIcon");
            playIconGO.transform.SetParent(playCircleGO.transform, false);
            RectTransform playIconRect = playIconGO.AddComponent<RectTransform>();
            playIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            playIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            playIconRect.anchoredPosition = new Vector2(4, 0);
            playIconRect.sizeDelta = new Vector2(44, 44);
            Image playIconImg = playIconGO.AddComponent<Image>();
            if (playSprite != null) playIconImg.sprite = playSprite;
            playIconImg.color = Color.black;
            playIconImg.raycastTarget = false;

            // Ad Text Middle
            GameObject adTextContGO = new GameObject("TextContainer");
            adTextContGO.transform.SetParent(adCardGO.transform, false);
            RectTransform adTextContRect = adTextContGO.AddComponent<RectTransform>();
            adTextContRect.anchorMin = new Vector2(0f, 0.5f);
            adTextContRect.anchorMax = new Vector2(0.75f, 0.5f);
            adTextContRect.pivot = new Vector2(0f, 0.5f);
            adTextContRect.anchoredPosition = new Vector2(150, 0);
            adTextContRect.sizeDelta = new Vector2(0, 100);

            GameObject adTitleGO = new GameObject("Title");
            adTitleGO.transform.SetParent(adTextContGO.transform, false);
            RectTransform adTitleRect = adTitleGO.AddComponent<RectTransform>();
            adTitleRect.anchorMin = new Vector2(0f, 1f);
            adTitleRect.anchorMax = new Vector2(1f, 1f);
            adTitleRect.pivot = new Vector2(0f, 1f);
            adTitleRect.anchoredPosition = new Vector2(0, -10);
            adTitleRect.sizeDelta = new Vector2(0, 36);
            TextMeshProUGUI adTitleTMP = adTitleGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) adTitleTMP.font = dmSansTMPFont;
            adTitleTMP.text = "Ve un anuncio y gana 1 sobre";
            adTitleTMP.fontSize = 28;
            adTitleTMP.fontStyle = FontStyles.Bold;
            adTitleTMP.color = TextWhite;
            adTitleTMP.raycastTarget = false;

            GameObject adSubGO = new GameObject("Subtitle");
            adSubGO.transform.SetParent(adTextContGO.transform, false);
            RectTransform adSubRect = adSubGO.AddComponent<RectTransform>();
            adSubRect.anchorMin = new Vector2(0f, 0f);
            adSubRect.anchorMax = new Vector2(1f, 0f);
            adSubRect.pivot = new Vector2(0f, 0f);
            adSubRect.anchoredPosition = new Vector2(0, 16);
            adSubRect.sizeDelta = new Vector2(0, 30);
            TextMeshProUGUI adSubTMP = adSubGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) adSubTMP.font = dmSansTMPFont;
            adSubTMP.text = "Gratis · Sin costo";
            adSubTMP.fontSize = 22;
            adSubTMP.color = TextGray;
            adSubTMP.raycastTarget = false;

            // Counter Right: "2/3" + "HOY"
            GameObject adCounterContGO = new GameObject("CounterContainer");
            adCounterContGO.transform.SetParent(adCardGO.transform, false);
            RectTransform adCounterContRect = adCounterContGO.AddComponent<RectTransform>();
            adCounterContRect.anchorMin = new Vector2(1f, 0.5f);
            adCounterContRect.anchorMax = new Vector2(1f, 0.5f);
            adCounterContRect.pivot = new Vector2(1f, 0.5f);
            adCounterContRect.anchoredPosition = new Vector2(-36, 0);
            adCounterContRect.sizeDelta = new Vector2(120, 100);

            GameObject adCountNumGO = new GameObject("CountText");
            adCountNumGO.transform.SetParent(adCounterContGO.transform, false);
            RectTransform adCountNumRect = adCountNumGO.AddComponent<RectTransform>();
            adCountNumRect.anchorMin = new Vector2(0f, 1f);
            adCountNumRect.anchorMax = new Vector2(1f, 1f);
            adCountNumRect.pivot = new Vector2(0.5f, 1f);
            adCountNumRect.anchoredPosition = new Vector2(0, -10);
            adCountNumRect.sizeDelta = new Vector2(0, 42);
            TextMeshProUGUI adCountNumTMP = adCountNumGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) adCountNumTMP.font = dmSansTMPFont;
            adCountNumTMP.text = "2/3";
            adCountNumTMP.fontSize = 38;
            adCountNumTMP.fontStyle = FontStyles.Bold;
            adCountNumTMP.alignment = TextAlignmentOptions.Center;
            adCountNumTMP.color = Gold;
            adCountNumTMP.raycastTarget = false;

            GameObject adHoyGO = new GameObject("HoyLabel");
            adHoyGO.transform.SetParent(adCounterContGO.transform, false);
            RectTransform adHoyRect = adHoyGO.AddComponent<RectTransform>();
            adHoyRect.anchorMin = new Vector2(0f, 0f);
            adHoyRect.anchorMax = new Vector2(1f, 0f);
            adHoyRect.pivot = new Vector2(0.5f, 0f);
            adHoyRect.anchoredPosition = new Vector2(0, 14);
            adHoyRect.sizeDelta = new Vector2(0, 24);
            TextMeshProUGUI adHoyTMP = adHoyGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) adHoyTMP.font = dmSansTMPFont;
            adHoyTMP.text = "HOY";
            adHoyTMP.fontSize = 18;
            adHoyTMP.characterSpacing = 4f;
            adHoyTMP.alignment = TextAlignmentOptions.Center;
            adHoyTMP.color = TextDim;
            adHoyTMP.raycastTarget = false;

            // ====================================================
            // 4. SECTION 3: "COMPRAR MONEDAS" (2x2 Grid)
            // ====================================================
            GameObject sec3GO = new GameObject("Section_BuyCoins");
            sec3GO.transform.SetParent(contentGO.transform, false);
            RectTransform sec3Rect = sec3GO.AddComponent<RectTransform>();
            sec3Rect.anchorMin = new Vector2(0.5f, 1f);
            sec3Rect.anchorMax = new Vector2(0.5f, 1f);
            sec3Rect.pivot = new Vector2(0.5f, 1f);
            sec3Rect.anchoredPosition = new Vector2(0, -1060);
            sec3Rect.sizeDelta = new Vector2(980, 520);

            GameObject sec3TitleGO = new GameObject("SectionTitle");
            sec3TitleGO.transform.SetParent(sec3GO.transform, false);
            RectTransform sec3TitleRect = sec3TitleGO.AddComponent<RectTransform>();
            sec3TitleRect.anchorMin = new Vector2(0f, 1f);
            sec3TitleRect.anchorMax = new Vector2(1f, 1f);
            sec3TitleRect.pivot = new Vector2(0f, 1f);
            sec3TitleRect.anchoredPosition = new Vector2(0, 0);
            sec3TitleRect.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI sec3TitleTMP = sec3TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec3TitleTMP.font = barlowTMPFont;
            sec3TitleTMP.text = "COMPRAR MONEDAS";
            sec3TitleTMP.fontSize = 30;
            sec3TitleTMP.fontStyle = FontStyles.Bold;
            sec3TitleTMP.characterSpacing = 8f;
            sec3TitleTMP.color = TextWhite;

            int[] coinAmounts = { 150, 400, 900, 2000 };
            int[] bonusAmounts = { 0, 0, 100, 300 };
            string[] priceTags = { "$0.99", "$1.99", "$3.99", "$7.99" };
            Button[] coinPackBtns = new Button[4];

            float cardW = 475f;
            float cardH = 220f;
            float gapX = 30f;
            float gapY = 24f;

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;

                GameObject cpGO = new GameObject($"CoinPack_{i}");
                cpGO.transform.SetParent(sec3GO.transform, false);
                RectTransform cpRect = cpGO.AddComponent<RectTransform>();
                cpRect.anchorMin = new Vector2(0f, 1f);
                cpRect.anchorMax = new Vector2(0f, 1f);
                cpRect.pivot = new Vector2(0f, 1f);
                cpRect.anchoredPosition = new Vector2(col * (cardW + gapX), -55 - row * (cardH + gapY));
                cpRect.sizeDelta = new Vector2(cardW, cardH);

                RoundedRectGraphic cpG = cpGO.AddComponent<RoundedRectGraphic>();
                cpG.CornerRadius = 20f;
                cpG.color = CardBg;
                cpG.BorderWidth = 1.5f;
                cpG.BorderColor = BorderSubtle;

                coinPackBtns[i] = cpGO.AddComponent<Button>();

                // Coin Icons at top
                GameObject cIconsHolderGO = new GameObject("Icons");
                cIconsHolderGO.transform.SetParent(cpGO.transform, false);
                RectTransform ciRect = cIconsHolderGO.AddComponent<RectTransform>();
                ciRect.anchorMin = new Vector2(0.5f, 1f);
                ciRect.anchorMax = new Vector2(0.5f, 1f);
                ciRect.pivot = new Vector2(0.5f, 1f);
                ciRect.anchoredPosition = new Vector2(0, -16);
                ciRect.sizeDelta = new Vector2(100, 44);

                HorizontalLayoutGroup cihlg = cIconsHolderGO.AddComponent<HorizontalLayoutGroup>();
                cihlg.childAlignment = TextAnchor.MiddleCenter;
                cihlg.childControlWidth = false;
                cihlg.childControlHeight = false;
                cihlg.spacing = 6f;

                GameObject c1GO = new GameObject("Coin1");
                c1GO.transform.SetParent(cIconsHolderGO.transform, false);
                RectTransform c1Rect = c1GO.AddComponent<RectTransform>();
                c1Rect.sizeDelta = new Vector2(40, 40);
                Image c1Img = c1GO.AddComponent<Image>();
                if (coinSprite != null) c1Img.sprite = coinSprite;
                c1Img.raycastTarget = false;

                if (bonusAmounts[i] > 0)
                {
                    GameObject c2GO = new GameObject("Coin2");
                    c2GO.transform.SetParent(cIconsHolderGO.transform, false);
                    RectTransform c2Rect = c2GO.AddComponent<RectTransform>();
                    c2Rect.sizeDelta = new Vector2(30, 30);
                    Image c2Img = c2GO.AddComponent<Image>();
                    if (coinSprite != null) c2Img.sprite = coinSprite;
                    c2Img.raycastTarget = false;
                }

                // Amount Text
                GameObject amtGO = new GameObject("AmountText");
                amtGO.transform.SetParent(cpGO.transform, false);
                RectTransform amtRect = amtGO.AddComponent<RectTransform>();
                amtRect.anchorMin = new Vector2(0f, 0.5f);
                amtRect.anchorMax = new Vector2(1f, 0.5f);
                amtRect.pivot = new Vector2(0.5f, 0.5f);
                amtRect.anchoredPosition = new Vector2(0, bonusAmounts[i] > 0 ? 4 : -6);
                amtRect.sizeDelta = new Vector2(0, 34);
                TextMeshProUGUI amtTMP = amtGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) amtTMP.font = dmSansTMPFont;
                amtTMP.text = coinAmounts[i].ToString();
                amtTMP.fontSize = 32;
                amtTMP.fontStyle = FontStyles.Bold;
                amtTMP.alignment = TextAlignmentOptions.Center;
                amtTMP.color = TextWhite;
                amtTMP.raycastTarget = false;

                if (bonusAmounts[i] > 0)
                {
                    GameObject bonusGO = new GameObject("BonusText");
                    bonusGO.transform.SetParent(cpGO.transform, false);
                    RectTransform bonusRect = bonusGO.AddComponent<RectTransform>();
                    bonusRect.anchorMin = new Vector2(0f, 0.5f);
                    bonusRect.anchorMax = new Vector2(1f, 0.5f);
                    bonusRect.pivot = new Vector2(0.5f, 0.5f);
                    bonusRect.anchoredPosition = new Vector2(0, -22);
                    bonusRect.sizeDelta = new Vector2(0, 24);
                    TextMeshProUGUI bonusTMP = bonusGO.AddComponent<TextMeshProUGUI>();
                    if (dmSansTMPFont != null) bonusTMP.font = dmSansTMPFont;
                    bonusTMP.text = $"+{bonusAmounts[i]} bonus";
                    bonusTMP.fontSize = 20;
                    bonusTMP.fontStyle = FontStyles.Bold;
                    bonusTMP.alignment = TextAlignmentOptions.Center;
                    bonusTMP.color = Gold;
                    bonusTMP.raycastTarget = false;
                }

                // Price Tag Pill (Bottom)
                GameObject pTagGO = new GameObject("PriceTag");
                pTagGO.transform.SetParent(cpGO.transform, false);
                RectTransform pTagRect = pTagGO.AddComponent<RectTransform>();
                pTagRect.anchorMin = new Vector2(0.5f, 0f);
                pTagRect.anchorMax = new Vector2(0.5f, 0f);
                pTagRect.pivot = new Vector2(0.5f, 0f);
                pTagRect.anchoredPosition = new Vector2(0, 14);
                pTagRect.sizeDelta = new Vector2(150, 44);

                RoundedRectGraphic pTagG = pTagGO.AddComponent<RoundedRectGraphic>();
                pTagG.CornerRadius = 10f;
                pTagG.color = new Color(1f, 1f, 1f, 0.07f);
                pTagG.BorderWidth = 1.2f;
                pTagG.BorderColor = BorderSubtle;
                pTagG.raycastTarget = false;

                GameObject pTagTextGO = new GameObject("Text");
                pTagTextGO.transform.SetParent(pTagGO.transform, false);
                RectTransform pttRect = pTagTextGO.AddComponent<RectTransform>();
                pttRect.anchorMin = Vector2.zero;
                pttRect.anchorMax = Vector2.one;
                pttRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI pttTMP = pTagTextGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) pttTMP.font = dmSansTMPFont;
                pttTMP.text = priceTags[i];
                pttTMP.fontSize = 22;
                pttTMP.fontStyle = FontStyles.Bold;
                pttTMP.alignment = TextAlignmentOptions.Center;
                pttTMP.color = TextGray;
                pttTMP.raycastTarget = false;
            }

            // ====================================================
            // 5. BOTTOM NAVIGATION BAR (5 Tabs, "Tienda" Active)
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
                bool isTabActive = (i == 2); // Tienda is Tab 2 (Active)
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

            // Assign Serialized Properties on StoreScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("coinsText").objectReferenceValue = coinsTMP;

            so.FindProperty("packAButton").objectReferenceValue = packBtns[0];
            so.FindProperty("packBButton").objectReferenceValue = packBtns[1];
            so.FindProperty("packCButton").objectReferenceValue = packBtns[2];

            so.FindProperty("watchAdButton").objectReferenceValue = adBtn;
            so.FindProperty("adCountText").objectReferenceValue = adCountNumTMP;

            so.FindProperty("coinPack1Button").objectReferenceValue = coinPackBtns[0];
            so.FindProperty("coinPack2Button").objectReferenceValue = coinPackBtns[1];
            so.FindProperty("coinPack3Button").objectReferenceValue = coinPackBtns[2];
            so.FindProperty("coinPack4Button").objectReferenceValue = coinPackBtns[3];

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];
            so.ApplyModifiedProperties();

            // Save Prefab in Assets/_Project/Prefabs/UI/
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/StoreScreenUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

            // Save Scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register Scenes in Build Settings
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/HomeScreenScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/MyCardsScene.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MyCardsScene.unity", true));
            }
            if (File.Exists("Assets/_Project/Scenes/StoreScene.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/StoreScene.unity", true));
            }
            if (File.Exists("Assets/_Project/Scenes/CommunityScene.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/CommunityScene.unity", true));
            }
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            }
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Tienda guardada como Escena Oficial (StoreScene.unity) y Prefab (StoreScreenUI.prefab)!</color>");
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
