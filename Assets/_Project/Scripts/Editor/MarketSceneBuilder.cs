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
    public static class MarketSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/MarketScene.unity";
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

        public static void BuildMarketScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_back.png");
            Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_coin.png");

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
            GameObject controllerGO = new GameObject("MarketScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            MarketScreenController controller = controllerGO.AddComponent<MarketScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. TOP HEADER (Back Arrow + "MERCADO" + Coins Balance)
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

            // Title: "MERCADO"
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(topBarGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0.6f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(70, 0);
            titleRect.sizeDelta = new Vector2(0, 80);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "MERCADO";
            titleTMP.fontSize = 46;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // Coin Balance Top Right
            GameObject coinPillGO = new GameObject("CoinBalance");
            coinPillGO.transform.SetParent(topBarGO.transform, false);
            RectTransform cpRect = coinPillGO.AddComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(1f, 0.5f);
            cpRect.anchorMax = new Vector2(1f, 0.5f);
            cpRect.pivot = new Vector2(1f, 0.5f);
            cpRect.anchoredPosition = new Vector2(0, 0);
            cpRect.sizeDelta = new Vector2(160, 60);

            HorizontalLayoutGroup cphlg = coinPillGO.AddComponent<HorizontalLayoutGroup>();
            cphlg.childAlignment = TextAnchor.MiddleRight;
            cphlg.childControlWidth = false;
            cphlg.childControlHeight = false;
            cphlg.spacing = 8f;

            GameObject cpIconGO = new GameObject("CoinIcon");
            cpIconGO.transform.SetParent(coinPillGO.transform, false);
            RectTransform cpiRect = cpIconGO.AddComponent<RectTransform>();
            cpiRect.sizeDelta = new Vector2(34, 34);
            Image cpiImg = cpIconGO.AddComponent<Image>();
            if (coinSprite != null) cpiImg.sprite = coinSprite;

            GameObject cpTextGO = new GameObject("CoinsText");
            cpTextGO.transform.SetParent(coinPillGO.transform, false);
            RectTransform cptRect = cpTextGO.AddComponent<RectTransform>();
            cptRect.sizeDelta = new Vector2(90, 36);
            TextMeshProUGUI coinsTMP = cpTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) coinsTMP.font = barlowTMPFont;
            coinsTMP.text = "1240";
            coinsTMP.fontSize = 32;
            coinsTMP.fontStyle = FontStyles.Bold;
            coinsTMP.color = Gold;
            coinsTMP.alignment = TextAlignmentOptions.Left;

            // ====================================================
            // 2. MAIN MODE TAB CHIPS ("COMPRAR", "MIS VENTAS")
            // ====================================================
            GameObject mainTabsGO = new GameObject("MainModeTabs");
            mainTabsGO.transform.SetParent(contentGO.transform, false);
            RectTransform mtRect = mainTabsGO.AddComponent<RectTransform>();
            mtRect.anchorMin = new Vector2(0.5f, 1f);
            mtRect.anchorMax = new Vector2(0.5f, 1f);
            mtRect.pivot = new Vector2(0.5f, 1f);
            mtRect.anchoredPosition = new Vector2(0, -150);
            mtRect.sizeDelta = new Vector2(980, 64);

            HorizontalLayoutGroup mthlg = mainTabsGO.AddComponent<HorizontalLayoutGroup>();
            mthlg.childAlignment = TextAnchor.MiddleLeft;
            mthlg.childControlWidth = false;
            mthlg.childControlHeight = false;
            mthlg.childForceExpandWidth = false;
            mthlg.childForceExpandHeight = false;
            mthlg.spacing = 12f;

            // Tab 1: Comprar
            GameObject tabBuyGO = new GameObject("Tab_Comprar");
            tabBuyGO.transform.SetParent(mainTabsGO.transform, false);
            RectTransform tbRect = tabBuyGO.AddComponent<RectTransform>();
            tbRect.sizeDelta = new Vector2(170, 60);

            RoundedRectGraphic tbG = tabBuyGO.AddComponent<RoundedRectGraphic>();
            tbG.IsCapsule = true;
            tbG.color = Gold;
            tbG.BorderWidth = 1.5f;
            tbG.BorderColor = Gold;

            Button tbBtn = tabBuyGO.AddComponent<Button>();

            GameObject tbTextGO = new GameObject("Text");
            tbTextGO.transform.SetParent(tabBuyGO.transform, false);
            RectTransform tbtRect = tbTextGO.AddComponent<RectTransform>();
            tbtRect.anchorMin = Vector2.zero;
            tbtRect.anchorMax = Vector2.one;
            tbtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI tbTMP = tbTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) tbTMP.font = barlowTMPFont;
            tbTMP.text = "COMPRAR";
            tbTMP.fontSize = 24;
            tbTMP.fontStyle = FontStyles.Bold;
            tbTMP.characterSpacing = 3f;
            tbTMP.alignment = TextAlignmentOptions.Center;
            tbTMP.color = new Color(0.051f, 0.102f, 0.075f);
            tbTMP.raycastTarget = false;

            // Tab 2: Mis Ventas
            GameObject tabSellGO = new GameObject("Tab_MisVentas");
            tabSellGO.transform.SetParent(mainTabsGO.transform, false);
            RectTransform tsRect = tabSellGO.AddComponent<RectTransform>();
            tsRect.sizeDelta = new Vector2(185, 60);

            RoundedRectGraphic tsG = tabSellGO.AddComponent<RoundedRectGraphic>();
            tsG.IsCapsule = true;
            tsG.color = Color.clear;
            tsG.BorderWidth = 1.5f;
            tsG.BorderColor = BorderSubtle;

            Button tsBtn = tabSellGO.AddComponent<Button>();

            GameObject tsTextGO = new GameObject("Text");
            tsTextGO.transform.SetParent(tabSellGO.transform, false);
            RectTransform tstRect = tsTextGO.AddComponent<RectTransform>();
            tstRect.anchorMin = Vector2.zero;
            tstRect.anchorMax = Vector2.one;
            tstRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI tsTMP = tsTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) tsTMP.font = barlowTMPFont;
            tsTMP.text = "MIS VENTAS";
            tsTMP.fontSize = 24;
            tsTMP.fontStyle = FontStyles.Bold;
            tsTMP.characterSpacing = 3f;
            tsTMP.alignment = TextAlignmentOptions.Center;
            tsTMP.color = TextGray;
            tsTMP.raycastTarget = false;

            // ====================================================
            // 3. RARITY FILTER CHIPS ("TODAS", "COMÚN", "POCO COMÚN", "RARA", "MÍTICA")
            // ====================================================
            GameObject rarityHolderGO = new GameObject("RarityFiltersHolder");
            rarityHolderGO.transform.SetParent(contentGO.transform, false);
            RectTransform rhRect = rarityHolderGO.AddComponent<RectTransform>();
            rhRect.anchorMin = new Vector2(0.5f, 1f);
            rhRect.anchorMax = new Vector2(0.5f, 1f);
            rhRect.pivot = new Vector2(0.5f, 1f);
            rhRect.anchoredPosition = new Vector2(0, -225);
            rhRect.sizeDelta = new Vector2(980, 54);

            HorizontalLayoutGroup rhlg = rarityHolderGO.AddComponent<HorizontalLayoutGroup>();
            rhlg.childAlignment = TextAnchor.MiddleLeft;
            rhlg.childControlWidth = false;
            rhlg.childControlHeight = false;
            rhlg.childForceExpandWidth = false;
            rhlg.childForceExpandHeight = false;
            rhlg.spacing = 10f;

            string[] filterLabels = { "TODAS", "COMÚN", "POCO COMÚN", "RARA", "MÍTICA" };
            float[] filterWidths = { 120f, 125f, 175f, 115f, 125f };

            List<Button> filterBtns = new List<Button>();
            List<RoundedRectGraphic> filterGraphics = new List<RoundedRectGraphic>();
            List<TMP_Text> filterTexts = new List<TMP_Text>();

            for (int f = 0; f < filterLabels.Length; f++)
            {
                bool isFilterActive = (f == 0);
                GameObject fGO = new GameObject($"Filter_{filterLabels[f]}");
                fGO.transform.SetParent(rarityHolderGO.transform, false);
                RectTransform fRect = fGO.AddComponent<RectTransform>();
                fRect.sizeDelta = new Vector2(filterWidths[f], 52);

                RoundedRectGraphic fG = fGO.AddComponent<RoundedRectGraphic>();
                fG.IsCapsule = true;
                fG.color = isFilterActive ? Gold : Color.clear;
                fG.BorderWidth = 1.5f;
                fG.BorderColor = isFilterActive ? Gold : BorderSubtle;

                Button fBtn = fGO.AddComponent<Button>();

                GameObject fTextGO = new GameObject("Text");
                fTextGO.transform.SetParent(fGO.transform, false);
                RectTransform ftRect = fTextGO.AddComponent<RectTransform>();
                ftRect.anchorMin = Vector2.zero;
                ftRect.anchorMax = Vector2.one;
                ftRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI ftTMP = fTextGO.AddComponent<TextMeshProUGUI>();
                if (barlowTMPFont != null) ftTMP.font = barlowTMPFont;
                ftTMP.text = filterLabels[f];
                ftTMP.fontSize = 20;
                ftTMP.fontStyle = FontStyles.Bold;
                ftTMP.characterSpacing = 2f;
                ftTMP.alignment = TextAlignmentOptions.Center;
                ftTMP.color = isFilterActive ? new Color(0.051f, 0.102f, 0.075f) : TextGray;
                ftTMP.raycastTarget = false;

                filterBtns.Add(fBtn);
                filterGraphics.Add(fG);
                filterTexts.Add(ftTMP);
            }

            // ====================================================
            // 4. BUY MODE LISTINGS GRID (Scrollable Container)
            // ====================================================
            GameObject buyContGO = new GameObject("BuyTabContainer");
            buyContGO.transform.SetParent(contentGO.transform, false);
            RectTransform bcRect = buyContGO.AddComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.5f, 1f);
            bcRect.anchorMax = new Vector2(0.5f, 1f);
            bcRect.pivot = new Vector2(0.5f, 1f);
            bcRect.anchoredPosition = new Vector2(0, -300);
            bcRect.sizeDelta = new Vector2(980, 1400);

            // 2-Column Grid
            GameObject gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(buyContGO.transform, false);
            RectTransform gridRect = gridGO.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(980, 1400);

            GridLayoutGroup glg = gridGO.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(475, 410);
            glg.spacing = new Vector2(30, 24);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            List<MarketListingCardView> cardViews = new List<MarketListingCardView>();
            for (int i = 0; i < 6; i++)
            {
                GameObject cardGO = CreateMarketCardItem(gridGO.transform, $"ListingCard_{i}", barlowTMPFont, dmSansTMPFont, coinSprite);
                cardViews.Add(cardGO.GetComponent<MarketListingCardView>());
            }

            // ====================================================
            // 5. SELL MODE CONTAINER ("Tus Duplicados" + "Listados Activos")
            // ====================================================
            GameObject sellContGO = new GameObject("SellTabContainer");
            sellContGO.transform.SetParent(contentGO.transform, false);
            RectTransform scRect = sellContGO.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 1f);
            scRect.anchorMax = new Vector2(0.5f, 1f);
            scRect.pivot = new Vector2(0.5f, 1f);
            scRect.anchoredPosition = new Vector2(0, -230);
            scRect.sizeDelta = new Vector2(980, 1450);

            // Section 1 Header: TUS DUPLICADOS
            GameObject dupHeaderGO = new GameObject("DuplicatesHeader");
            dupHeaderGO.transform.SetParent(sellContGO.transform, false);
            RectTransform dhRect = dupHeaderGO.AddComponent<RectTransform>();
            dhRect.anchorMin = new Vector2(0f, 1f);
            dhRect.anchorMax = new Vector2(1f, 1f);
            dhRect.pivot = new Vector2(0f, 1f);
            dhRect.anchoredPosition = new Vector2(0, 0);
            dhRect.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI dhTMP = dupHeaderGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) dhTMP.font = barlowTMPFont;
            dhTMP.text = "TUS DUPLICADOS";
            dhTMP.fontSize = 28;
            dhTMP.fontStyle = FontStyles.Bold;
            dhTMP.characterSpacing = 4f;
            dhTMP.color = TextWhite;

            // Section 1 Grid (2x2)
            GameObject dupGridGO = new GameObject("DuplicatesGrid");
            dupGridGO.transform.SetParent(sellContGO.transform, false);
            RectTransform dgRect = dupGridGO.AddComponent<RectTransform>();
            dgRect.anchorMin = new Vector2(0.5f, 1f);
            dgRect.anchorMax = new Vector2(0.5f, 1f);
            dgRect.pivot = new Vector2(0.5f, 1f);
            dgRect.anchoredPosition = new Vector2(0, -48);
            dgRect.sizeDelta = new Vector2(980, 720);

            GridLayoutGroup dglg = dupGridGO.AddComponent<GridLayoutGroup>();
            dglg.cellSize = new Vector2(475, 340);
            dglg.spacing = new Vector2(30, 20);
            dglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            dglg.constraintCount = 2;

            List<DuplicateCardView> dupViews = new List<DuplicateCardView>();
            for (int i = 0; i < 4; i++)
            {
                GameObject dupCardGO = CreateDuplicateCardItem(dupGridGO.transform, $"DupCard_{i}", barlowTMPFont, dmSansTMPFont);
                dupViews.Add(dupCardGO.GetComponent<DuplicateCardView>());
            }

            // Section 2 Header: LISTADOS ACTIVOS
            GameObject actHeaderGO = new GameObject("ActiveListingsHeader");
            actHeaderGO.transform.SetParent(sellContGO.transform, false);
            RectTransform ahRect = actHeaderGO.AddComponent<RectTransform>();
            ahRect.anchorMin = new Vector2(0f, 1f);
            ahRect.anchorMax = new Vector2(1f, 1f);
            ahRect.pivot = new Vector2(0f, 1f);
            ahRect.anchoredPosition = new Vector2(0, -780);
            ahRect.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI ahTMP = actHeaderGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) ahTMP.font = barlowTMPFont;
            ahTMP.text = "LISTADOS ACTIVOS";
            ahTMP.fontSize = 28;
            ahTMP.fontStyle = FontStyles.Bold;
            ahTMP.characterSpacing = 4f;
            ahTMP.color = TextWhite;

            // Section 2 Vertical List
            GameObject actListGO = new GameObject("ActiveListingsHolder");
            actListGO.transform.SetParent(sellContGO.transform, false);
            RectTransform alRect = actListGO.AddComponent<RectTransform>();
            alRect.anchorMin = new Vector2(0.5f, 1f);
            alRect.anchorMax = new Vector2(0.5f, 1f);
            alRect.pivot = new Vector2(0.5f, 1f);
            alRect.anchoredPosition = new Vector2(0, -830);
            alRect.sizeDelta = new Vector2(980, 500);

            VerticalLayoutGroup alvlg = actListGO.AddComponent<VerticalLayoutGroup>();
            alvlg.childAlignment = TextAnchor.UpperCenter;
            alvlg.childControlWidth = true;
            alvlg.childControlHeight = true;
            alvlg.childForceExpandWidth = true;
            alvlg.childForceExpandHeight = false;
            alvlg.spacing = 18f;

            List<ActiveListingCardView> activeViews = new List<ActiveListingCardView>();
            for (int i = 0; i < 2; i++)
            {
                GameObject actCardGO = CreateActiveListingCardItem(actListGO.transform, $"ActiveCard_{i}", barlowTMPFont, dmSansTMPFont, coinSprite);
                activeViews.Add(actCardGO.GetComponent<ActiveListingCardView>());
            }

            sellContGO.SetActive(false); // Inactive initially

            // ====================================================
            // 6. BOTTOM NAVIGATION BAR (5 Tabs, "Comunidad" Active)
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

            // Assign Serialized Properties on MarketScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("coinsText").objectReferenceValue = coinsTMP;

            so.FindProperty("tabBuyButton").objectReferenceValue = tbBtn;
            so.FindProperty("tabSellButton").objectReferenceValue = tsBtn;
            so.FindProperty("tabBuyGraphic").objectReferenceValue = tbG;
            so.FindProperty("tabSellGraphic").objectReferenceValue = tsG;
            so.FindProperty("tabBuyText").objectReferenceValue = tbTMP;
            so.FindProperty("tabSellText").objectReferenceValue = tsTMP;

            so.FindProperty("rarityFiltersHolderGO").objectReferenceValue = rarityHolderGO;
            so.FindProperty("buyTabContainer").objectReferenceValue = buyContGO;
            so.FindProperty("sellTabContainer").objectReferenceValue = sellContGO;

            SerializedProperty fbProp = so.FindProperty("rarityFilterButtons");
            fbProp.arraySize = filterBtns.Count;
            for (int i = 0; i < filterBtns.Count; i++) fbProp.GetArrayElementAtIndex(i).objectReferenceValue = filterBtns[i];

            SerializedProperty fgProp = so.FindProperty("rarityFilterGraphics");
            fgProp.arraySize = filterGraphics.Count;
            for (int i = 0; i < filterGraphics.Count; i++) fgProp.GetArrayElementAtIndex(i).objectReferenceValue = filterGraphics[i];

            SerializedProperty ftProp = so.FindProperty("rarityFilterTexts");
            ftProp.arraySize = filterTexts.Count;
            for (int i = 0; i < filterTexts.Count; i++) ftProp.GetArrayElementAtIndex(i).objectReferenceValue = filterTexts[i];

            SerializedProperty cardsProp = so.FindProperty("listingCardViews");
            cardsProp.arraySize = cardViews.Count;
            for (int i = 0; i < cardViews.Count; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardViews[i];

            SerializedProperty dupProp = so.FindProperty("duplicateCardViews");
            dupProp.arraySize = dupViews.Count;
            for (int i = 0; i < dupViews.Count; i++) dupProp.GetArrayElementAtIndex(i).objectReferenceValue = dupViews[i];

            SerializedProperty actProp = so.FindProperty("activeListingCardViews");
            actProp.arraySize = activeViews.Count;
            for (int i = 0; i < activeViews.Count; i++) actProp.GetArrayElementAtIndex(i).objectReferenceValue = activeViews[i];

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/MarketScreenUI.prefab";
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
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Mercado guardada con pestañas COMPRAR y MIS VENTAS completas (MarketScene.unity & MarketScreenUI.prefab)!</color>");
        }

        private static GameObject CreateMarketCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont, Sprite coinSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(475, 410);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 20f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.5f;
            cardG.BorderColor = new Color(0.188f, 0.820f, 0.345f);

            MarketListingCardView cardView = cardGO.AddComponent<MarketListingCardView>();

            // Seller Row (Top)
            GameObject sellerRowGO = new GameObject("SellerRow");
            sellerRowGO.transform.SetParent(cardGO.transform, false);
            RectTransform srRect = sellerRowGO.AddComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0f, 1f);
            srRect.anchorMax = new Vector2(1f, 1f);
            srRect.pivot = new Vector2(0.5f, 1f);
            srRect.anchoredPosition = new Vector2(0, -12);
            srRect.sizeDelta = new Vector2(-28, 44);

            // Seller Avatar
            GameObject savGO = new GameObject("AvatarCircle");
            savGO.transform.SetParent(sellerRowGO.transform, false);
            RectTransform savRect = savGO.AddComponent<RectTransform>();
            savRect.anchorMin = new Vector2(0f, 0.5f);
            savRect.anchorMax = new Vector2(0f, 0.5f);
            savRect.pivot = new Vector2(0f, 0.5f);
            savRect.anchoredPosition = new Vector2(0, 0);
            savRect.sizeDelta = new Vector2(36, 36);
            RoundedRectGraphic savG = savGO.AddComponent<RoundedRectGraphic>();
            savG.IsCapsule = true;
            savG.color = new Color(1f, 1f, 1f, 0.08f);
            savG.BorderWidth = 1.0f;
            savG.BorderColor = BorderSubtle;
            savG.raycastTarget = false;

            GameObject savTextGO = new GameObject("Text");
            savTextGO.transform.SetParent(savGO.transform, false);
            RectTransform savtRect = savTextGO.AddComponent<RectTransform>();
            savtRect.anchorMin = Vector2.zero;
            savtRect.anchorMax = Vector2.one;
            savtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI savTMP = savTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) savTMP.font = dmSansFont;
            savTMP.text = "PP";
            savTMP.fontSize = 16;
            savTMP.fontStyle = FontStyles.Bold;
            savTMP.alignment = TextAlignmentOptions.Center;
            savTMP.color = TextWhite;
            savTMP.raycastTarget = false;

            // Seller Name
            GameObject sNameGO = new GameObject("SellerName");
            sNameGO.transform.SetParent(sellerRowGO.transform, false);
            RectTransform snRect = sNameGO.AddComponent<RectTransform>();
            snRect.anchorMin = new Vector2(0f, 0.5f);
            snRect.anchorMax = new Vector2(0.6f, 0.5f);
            snRect.pivot = new Vector2(0f, 0.5f);
            snRect.anchoredPosition = new Vector2(44, 0);
            snRect.sizeDelta = new Vector2(0, 30);
            TextMeshProUGUI snTMP = sNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) snTMP.font = dmSansFont;
            snTMP.text = "ProPlayer_99";
            snTMP.fontSize = 20;
            snTMP.fontStyle = FontStyles.Bold;
            snTMP.color = TextWhite;
            snTMP.raycastTarget = false;

            // Posted Time
            GameObject sTimeGO = new GameObject("PostedTime");
            sTimeGO.transform.SetParent(sellerRowGO.transform, false);
            RectTransform stRect = sTimeGO.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(1f, 0.5f);
            stRect.anchorMax = new Vector2(1f, 0.5f);
            stRect.pivot = new Vector2(1f, 0.5f);
            stRect.anchoredPosition = new Vector2(0, 0);
            stRect.sizeDelta = new Vector2(120, 26);
            TextMeshProUGUI stTMP = sTimeGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) stTMP.font = dmSansFont;
            stTMP.text = "hace 5 min";
            stTMP.fontSize = 17;
            stTMP.alignment = TextAlignmentOptions.Right;
            stTMP.color = TextDim;
            stTMP.raycastTarget = false;

            // Card Initials Big Center (e.g. "JM")
            GameObject iniGO = new GameObject("InitialsText");
            iniGO.transform.SetParent(cardGO.transform, false);
            RectTransform iniRect = iniGO.AddComponent<RectTransform>();
            iniRect.anchorMin = new Vector2(0f, 0.52f);
            iniRect.anchorMax = new Vector2(1f, 0.78f);
            iniRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI iniTMP = iniGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) iniTMP.font = barlowFont;
            iniTMP.text = "JM";
            iniTMP.fontSize = 58;
            iniTMP.fontStyle = FontStyles.Bold;
            iniTMP.alignment = TextAlignmentOptions.Center;
            iniTMP.color = new Color(1f, 1f, 1f, 0.20f);

            // Card Name (e.g. "Musiala")
            GameObject pNameGO = new GameObject("PlayerNameText");
            pNameGO.transform.SetParent(cardGO.transform, false);
            RectTransform pnRect = pNameGO.AddComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0f, 0.40f);
            pnRect.anchorMax = new Vector2(1f, 0.52f);
            pnRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI pnTMP = pNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) pnTMP.font = dmSansFont;
            pnTMP.text = "Musiala";
            pnTMP.fontSize = 20;
            pnTMP.alignment = TextAlignmentOptions.Center;
            pnTMP.color = TextWhite;

            // Rarity Label (e.g. "COMÚN")
            GameObject rarGO = new GameObject("RarityText");
            rarGO.transform.SetParent(cardGO.transform, false);
            RectTransform rarRect = rarGO.AddComponent<RectTransform>();
            rarRect.anchorMin = new Vector2(0f, 0.30f);
            rarRect.anchorMax = new Vector2(1f, 0.40f);
            rarRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rarTMP = rarGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rarTMP.font = barlowFont;
            rarTMP.text = "COMÚN";
            rarTMP.fontSize = 18;
            rarTMP.fontStyle = FontStyles.Bold;
            rarTMP.characterSpacing = 4f;
            rarTMP.alignment = TextAlignmentOptions.Center;
            rarTMP.color = new Color(0.188f, 0.820f, 0.345f);

            // Price Box Row (Bottom 1)
            GameObject pBoxGO = new GameObject("PriceBox");
            pBoxGO.transform.SetParent(cardGO.transform, false);
            RectTransform pbRect = pBoxGO.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0f);
            pbRect.anchorMax = new Vector2(0.5f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.anchoredPosition = new Vector2(0, 76);
            pbRect.sizeDelta = new Vector2(435, 48);

            RoundedRectGraphic pbG = pBoxGO.AddComponent<RoundedRectGraphic>();
            pbG.CornerRadius = 10f;
            pbG.color = new Color(0.910f, 0.659f, 0.125f, 0.10f);
            pbG.BorderWidth = 1.2f;
            pbG.BorderColor = GoldBorder;

            GameObject pbContentGO = new GameObject("Content");
            pbContentGO.transform.SetParent(pBoxGO.transform, false);
            RectTransform pbcRect = pbContentGO.AddComponent<RectTransform>();
            pbcRect.anchorMin = Vector2.zero;
            pbcRect.anchorMax = Vector2.one;
            pbcRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup pbhlg = pbContentGO.AddComponent<HorizontalLayoutGroup>();
            pbhlg.childAlignment = TextAnchor.MiddleLeft;
            pbhlg.childControlWidth = false;
            pbhlg.childControlHeight = false;
            pbhlg.padding = new RectOffset(16, 0, 0, 0);
            pbhlg.spacing = 8f;

            GameObject pbIconGO = new GameObject("CoinIcon");
            pbIconGO.transform.SetParent(pbContentGO.transform, false);
            RectTransform pbiRect = pbIconGO.AddComponent<RectTransform>();
            pbiRect.sizeDelta = new Vector2(24, 24);
            Image pbiImg = pbIconGO.AddComponent<Image>();
            if (coinSprite != null) pbiImg.sprite = coinSprite;

            GameObject pbTextGO = new GameObject("PriceText");
            pbTextGO.transform.SetParent(pbContentGO.transform, false);
            RectTransform pbtRect = pbTextGO.AddComponent<RectTransform>();
            pbtRect.sizeDelta = new Vector2(100, 30);
            TextMeshProUGUI pbtTMP = pbTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) pbtTMP.font = barlowFont;
            pbtTMP.text = "25";
            pbtTMP.fontSize = 24;
            pbtTMP.fontStyle = FontStyles.Bold;
            pbtTMP.alignment = TextAlignmentOptions.Left;
            pbtTMP.color = Gold;

            // COMPRAR Button (Bottom 2)
            GameObject buyBtnGO = new GameObject("BuyButton");
            buyBtnGO.transform.SetParent(cardGO.transform, false);
            RectTransform bbRect = buyBtnGO.AddComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0.5f, 0f);
            bbRect.anchorMax = new Vector2(0.5f, 0f);
            bbRect.pivot = new Vector2(0.5f, 0f);
            bbRect.anchoredPosition = new Vector2(0, 16);
            bbRect.sizeDelta = new Vector2(435, 52);

            RoundedRectGraphic bbG = buyBtnGO.AddComponent<RoundedRectGraphic>();
            bbG.CornerRadius = 10f;
            bbG.color = Gold;
            Button buyBtn = buyBtnGO.AddComponent<Button>();

            GameObject bbTextGO = new GameObject("Text");
            bbTextGO.transform.SetParent(buyBtnGO.transform, false);
            RectTransform bbtRect = bbTextGO.AddComponent<RectTransform>();
            bbtRect.anchorMin = Vector2.zero;
            bbtRect.anchorMax = Vector2.one;
            bbtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI bbtTMP = bbTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) bbtTMP.font = barlowFont;
            bbtTMP.text = "COMPRAR";
            bbtTMP.fontSize = 22;
            bbtTMP.fontStyle = FontStyles.Bold;
            bbtTMP.characterSpacing = 3f;
            bbtTMP.alignment = TextAlignmentOptions.Center;
            bbtTMP.color = new Color(0.051f, 0.102f, 0.075f);

            // Serialize MarketListingCardView
            SerializedObject mlSO = new SerializedObject(cardView);
            mlSO.FindProperty("sellerAvatarText").objectReferenceValue = savTMP;
            mlSO.FindProperty("sellerNameText").objectReferenceValue = snTMP;
            mlSO.FindProperty("postedAtText").objectReferenceValue = stTMP;
            mlSO.FindProperty("initialsText").objectReferenceValue = iniTMP;
            mlSO.FindProperty("cardNameText").objectReferenceValue = pnTMP;
            mlSO.FindProperty("rarityText").objectReferenceValue = rarTMP;
            mlSO.FindProperty("cardBorderGraphic").objectReferenceValue = cardG;
            mlSO.FindProperty("priceText").objectReferenceValue = pbtTMP;
            mlSO.FindProperty("buyButton").objectReferenceValue = buyBtn;
            mlSO.ApplyModifiedProperties();

            return cardGO;
        }

        private static GameObject CreateDuplicateCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(475, 340);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 18f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.5f;
            cardG.BorderColor = new Color(0.678f, 0.369f, 0.941f); // Purple / Rara by default

            DuplicateCardView cardView = cardGO.AddComponent<DuplicateCardView>();

            // Count Badge Top Left (e.g. "×3")
            GameObject badgeGO = new GameObject("CountBadge");
            badgeGO.transform.SetParent(cardGO.transform, false);
            RectTransform bRect = badgeGO.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 1f);
            bRect.anchorMax = new Vector2(0f, 1f);
            bRect.pivot = new Vector2(0f, 1f);
            bRect.anchoredPosition = new Vector2(16, -14);
            bRect.sizeDelta = new Vector2(42, 28);
            TextMeshProUGUI bTMP = badgeGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) bTMP.font = dmSansFont;
            bTMP.text = "×2";
            bTMP.fontSize = 20;
            bTMP.fontStyle = FontStyles.Bold;
            bTMP.color = Gold;

            // Initials Big Center
            GameObject iniGO = new GameObject("Initials");
            iniGO.transform.SetParent(cardGO.transform, false);
            RectTransform iniRect = iniGO.AddComponent<RectTransform>();
            iniRect.anchorMin = new Vector2(0f, 0.48f);
            iniRect.anchorMax = new Vector2(1f, 0.82f);
            iniRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI iniTMP = iniGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) iniTMP.font = barlowFont;
            iniTMP.text = "JB";
            iniTMP.fontSize = 54;
            iniTMP.fontStyle = FontStyles.Bold;
            iniTMP.alignment = TextAlignmentOptions.Center;
            iniTMP.color = new Color(1f, 1f, 1f, 0.20f);

            // Rarity Label
            GameObject rarGO = new GameObject("Rarity");
            rarGO.transform.SetParent(cardGO.transform, false);
            RectTransform rarRect = rarGO.AddComponent<RectTransform>();
            rarRect.anchorMin = new Vector2(0f, 0.36f);
            rarRect.anchorMax = new Vector2(1f, 0.48f);
            rarRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rarTMP = rarGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rarTMP.font = barlowFont;
            rarTMP.text = "RARA";
            rarTMP.fontSize = 17;
            rarTMP.fontStyle = FontStyles.Bold;
            rarTMP.characterSpacing = 3f;
            rarTMP.alignment = TextAlignmentOptions.Center;
            rarTMP.color = new Color(0.678f, 0.369f, 0.941f);

            // Player Name
            GameObject nameGO = new GameObject("PlayerName");
            nameGO.transform.SetParent(cardGO.transform, false);
            RectTransform nRect = nameGO.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 0.24f);
            nRect.anchorMax = new Vector2(1f, 0.36f);
            nRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI nTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) nTMP.font = dmSansFont;
            nTMP.text = "Bellingham";
            nTMP.fontSize = 19;
            nTMP.alignment = TextAlignmentOptions.Center;
            nTMP.color = TextWhite;

            // PUBLICAR Button (Bottom)
            GameObject pubBtnGO = new GameObject("PublishButton");
            pubBtnGO.transform.SetParent(cardGO.transform, false);
            RectTransform pbRect = pubBtnGO.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0f);
            pbRect.anchorMax = new Vector2(0.5f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.anchoredPosition = new Vector2(0, 14);
            pbRect.sizeDelta = new Vector2(435, 48);

            RoundedRectGraphic pbG = pubBtnGO.AddComponent<RoundedRectGraphic>();
            pbG.CornerRadius = 10f;
            pbG.color = new Color(0.910f, 0.659f, 0.125f, 0.12f);
            pbG.BorderWidth = 1.2f;
            pbG.BorderColor = new Color(0.910f, 0.659f, 0.125f, 0.40f);
            Button pubBtn = pubBtnGO.AddComponent<Button>();

            GameObject pbTextGO = new GameObject("Text");
            pbTextGO.transform.SetParent(pubBtnGO.transform, false);
            RectTransform pbtRect = pbTextGO.AddComponent<RectTransform>();
            pbtRect.anchorMin = Vector2.zero;
            pbtRect.anchorMax = Vector2.one;
            pbtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI pbtTMP = pbTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) pbtTMP.font = barlowFont;
            pbtTMP.text = "PUBLICAR";
            pbtTMP.fontSize = 22;
            pbtTMP.fontStyle = FontStyles.Bold;
            pbtTMP.characterSpacing = 3f;
            pbtTMP.alignment = TextAlignmentOptions.Center;
            pbtTMP.color = Gold;

            // Serialize DuplicateCardView
            SerializedObject dupSO = new SerializedObject(cardView);
            dupSO.FindProperty("countBadgeText").objectReferenceValue = bTMP;
            dupSO.FindProperty("initialsText").objectReferenceValue = iniTMP;
            dupSO.FindProperty("cardNameText").objectReferenceValue = nTMP;
            dupSO.FindProperty("rarityText").objectReferenceValue = rarTMP;
            dupSO.FindProperty("cardBorderGraphic").objectReferenceValue = cardG;
            dupSO.FindProperty("publishButton").objectReferenceValue = pubBtn;
            dupSO.ApplyModifiedProperties();

            return cardGO;
        }

        private static GameObject CreateActiveListingCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont, Sprite coinSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(980, 220);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 18f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.2f;
            cardG.BorderColor = BorderSubtle;

            LayoutElement le = cardGO.AddComponent<LayoutElement>();
            le.minHeight = 220f;
            le.preferredHeight = 220f;
            le.flexibleHeight = 0f;

            ActiveListingCardView cardView = cardGO.AddComponent<ActiveListingCardView>();

            // Mini Card Preview Left
            GameObject miniGO = new GameObject("MiniCardPreview");
            miniGO.transform.SetParent(cardGO.transform, false);
            RectTransform miniRect = miniGO.AddComponent<RectTransform>();
            miniRect.anchorMin = new Vector2(0f, 1f);
            miniRect.anchorMax = new Vector2(0f, 1f);
            miniRect.pivot = new Vector2(0f, 1f);
            miniRect.anchoredPosition = new Vector2(20, -18);
            miniRect.sizeDelta = new Vector2(68, 90);

            RoundedRectGraphic miniG = miniGO.AddComponent<RoundedRectGraphic>();
            miniG.CornerRadius = 12f;
            miniG.color = new Color(0.035f, 0.07f, 0.05f);
            miniG.BorderWidth = 1.8f;
            miniG.BorderColor = new Color(0.678f, 0.369f, 0.941f); // Purple / Rara

            GameObject miniInitGO = new GameObject("Initials");
            miniInitGO.transform.SetParent(miniGO.transform, false);
            RectTransform miRect = miniInitGO.AddComponent<RectTransform>();
            miRect.anchorMin = Vector2.zero;
            miRect.anchorMax = Vector2.one;
            miRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI miTMP = miniInitGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) miTMP.font = barlowFont;
            miTMP.text = "KDB";
            miTMP.fontSize = 24;
            miTMP.fontStyle = FontStyles.Bold;
            miTMP.alignment = TextAlignmentOptions.Center;
            miTMP.color = new Color(1f, 1f, 1f, 0.25f);

            // Card Name & Rarity
            GameObject nameGO = new GameObject("CardName");
            nameGO.transform.SetParent(cardGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(104, -18);
            nameRect.sizeDelta = new Vector2(0, 28);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) nameTMP.font = dmSansFont;
            nameTMP.text = "De Bruyne";
            nameTMP.fontSize = 22;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = TextWhite;

            GameObject rarGO = new GameObject("Rarity");
            rarGO.transform.SetParent(cardGO.transform, false);
            RectTransform rarRect = rarGO.AddComponent<RectTransform>();
            rarRect.anchorMin = new Vector2(0f, 1f);
            rarRect.anchorMax = new Vector2(0.5f, 1f);
            rarRect.pivot = new Vector2(0f, 1f);
            rarRect.anchoredPosition = new Vector2(104, -46);
            rarRect.sizeDelta = new Vector2(0, 24);
            TextMeshProUGUI rarTMP = rarGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rarTMP.font = barlowFont;
            rarTMP.text = "RARA";
            rarTMP.fontSize = 17;
            rarTMP.fontStyle = FontStyles.Bold;
            rarTMP.characterSpacing = 3f;
            rarTMP.color = new Color(0.678f, 0.369f, 0.941f);

            // Price Tag Row
            GameObject pTagGO = new GameObject("PriceTagBox");
            pTagGO.transform.SetParent(cardGO.transform, false);
            RectTransform ptRect = pTagGO.AddComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0f, 1f);
            ptRect.anchorMax = new Vector2(1f, 1f);
            ptRect.pivot = new Vector2(0f, 1f);
            ptRect.anchoredPosition = new Vector2(104, -74);
            ptRect.sizeDelta = new Vector2(-124, 40);

            RoundedRectGraphic ptG = pTagGO.AddComponent<RoundedRectGraphic>();
            ptG.CornerRadius = 8f;
            ptG.color = new Color(0.910f, 0.659f, 0.125f, 0.10f);
            ptG.BorderWidth = 1.0f;
            ptG.BorderColor = GoldBorder;

            GameObject ptContentGO = new GameObject("Content");
            ptContentGO.transform.SetParent(pTagGO.transform, false);
            RectTransform ptcRect = ptContentGO.AddComponent<RectTransform>();
            ptcRect.anchorMin = Vector2.zero;
            ptcRect.anchorMax = Vector2.one;
            ptcRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup pthlg = ptContentGO.AddComponent<HorizontalLayoutGroup>();
            pthlg.childAlignment = TextAnchor.MiddleLeft;
            pthlg.childControlWidth = false;
            pthlg.childControlHeight = false;
            pthlg.padding = new RectOffset(12, 0, 0, 0);
            pthlg.spacing = 6f;

            GameObject ptiGO = new GameObject("CoinIcon");
            ptiGO.transform.SetParent(ptContentGO.transform, false);
            RectTransform ptiRect = ptiGO.AddComponent<RectTransform>();
            ptiRect.sizeDelta = new Vector2(20, 20);
            Image ptiImg = ptiGO.AddComponent<Image>();
            if (coinSprite != null) ptiImg.sprite = coinSprite;

            GameObject pttGO = new GameObject("PriceText");
            pttGO.transform.SetParent(ptContentGO.transform, false);
            RectTransform pttRect = pttGO.AddComponent<RectTransform>();
            pttRect.sizeDelta = new Vector2(80, 26);
            TextMeshProUGUI pttTMP = pttGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) pttTMP.font = barlowFont;
            pttTMP.text = "250";
            pttTMP.fontSize = 22;
            pttTMP.fontStyle = FontStyles.Bold;
            pttTMP.color = Gold;

            // Posted At Time
            GameObject timeGO = new GameObject("PostedTime");
            timeGO.transform.SetParent(cardGO.transform, false);
            RectTransform timeRect = timeGO.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0f, 1f);
            timeRect.anchorMax = new Vector2(1f, 1f);
            timeRect.pivot = new Vector2(0f, 1f);
            timeRect.anchoredPosition = new Vector2(104, -118);
            timeRect.sizeDelta = new Vector2(-124, 22);
            TextMeshProUGUI timeTMP = timeGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) timeTMP.font = dmSansFont;
            timeTMP.text = "Publicado hace 2 h";
            timeTMP.fontSize = 16;
            timeTMP.color = TextDim;

            // Actions Row (EDITAR PRECIO / RETIRAR)
            GameObject actionsGO = new GameObject("ActionsRow");
            actionsGO.transform.SetParent(cardGO.transform, false);
            RectTransform actRect = actionsGO.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.5f, 0f);
            actRect.anchorMax = new Vector2(0.5f, 0f);
            actRect.pivot = new Vector2(0.5f, 0f);
            actRect.anchoredPosition = new Vector2(0, 14);
            actRect.sizeDelta = new Vector2(932, 52);

            HorizontalLayoutGroup acthlg = actionsGO.AddComponent<HorizontalLayoutGroup>();
            acthlg.childAlignment = TextAnchor.MiddleCenter;
            acthlg.childControlWidth = true;
            acthlg.childControlHeight = true;
            acthlg.childForceExpandWidth = true;
            acthlg.childForceExpandHeight = true;
            acthlg.spacing = 14f;

            // EDITAR PRECIO Button
            GameObject editBtnGO = new GameObject("EditPriceButton");
            editBtnGO.transform.SetParent(actionsGO.transform, false);
            RoundedRectGraphic editG = editBtnGO.AddComponent<RoundedRectGraphic>();
            editG.CornerRadius = 10f;
            editG.color = new Color(1f, 1f, 1f, 0.05f);
            editG.BorderWidth = 1.0f;
            editG.BorderColor = BorderSubtle;
            Button editBtn = editBtnGO.AddComponent<Button>();

            GameObject editTxtGO = new GameObject("Text");
            editTxtGO.transform.SetParent(editBtnGO.transform, false);
            RectTransform etRect = editTxtGO.AddComponent<RectTransform>();
            etRect.anchorMin = Vector2.zero;
            etRect.anchorMax = Vector2.one;
            etRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI etTMP = editTxtGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) etTMP.font = barlowFont;
            etTMP.text = "EDITAR PRECIO";
            etTMP.fontSize = 20;
            etTMP.fontStyle = FontStyles.Bold;
            etTMP.characterSpacing = 3f;
            etTMP.alignment = TextAlignmentOptions.Center;
            etTMP.color = TextGray;

            // RETIRAR Button
            GameObject retBtnGO = new GameObject("WithdrawButton");
            retBtnGO.transform.SetParent(actionsGO.transform, false);
            RoundedRectGraphic retG = retBtnGO.AddComponent<RoundedRectGraphic>();
            retG.CornerRadius = 10f;
            retG.color = new Color(0.86f, 0.2f, 0.2f, 0.08f);
            retG.BorderWidth = 1.0f;
            retG.BorderColor = new Color(0.86f, 0.2f, 0.2f, 0.35f);
            Button retBtn = retBtnGO.AddComponent<Button>();

            GameObject retTxtGO = new GameObject("Text");
            retTxtGO.transform.SetParent(retBtnGO.transform, false);
            RectTransform rtRect = retTxtGO.AddComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rtTMP = retTxtGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rtTMP.font = barlowFont;
            rtTMP.text = "RETIRAR";
            rtTMP.fontSize = 20;
            rtTMP.fontStyle = FontStyles.Bold;
            rtTMP.characterSpacing = 3f;
            rtTMP.alignment = TextAlignmentOptions.Center;
            rtTMP.color = new Color(0.86f, 0.35f, 0.35f);

            // Serialize ActiveListingCardView
            SerializedObject actSO = new SerializedObject(cardView);
            actSO.FindProperty("initialsText").objectReferenceValue = miTMP;
            actSO.FindProperty("miniBorderGraphic").objectReferenceValue = miniG;
            actSO.FindProperty("cardNameText").objectReferenceValue = nameTMP;
            actSO.FindProperty("rarityText").objectReferenceValue = rarTMP;
            actSO.FindProperty("priceText").objectReferenceValue = pttTMP;
            actSO.FindProperty("postedAtText").objectReferenceValue = timeTMP;
            actSO.FindProperty("editPriceButton").objectReferenceValue = editBtn;
            actSO.FindProperty("withdrawButton").objectReferenceValue = retBtn;
            actSO.ApplyModifiedProperties();

            return cardGO;
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
