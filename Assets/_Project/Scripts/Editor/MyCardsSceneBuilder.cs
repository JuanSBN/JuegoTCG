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
    public static class MyCardsSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/MyCardsScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Exact Design Tokens from docs/Pantallas/src/App.tsx
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);

        // Rarity Palette
        private static readonly Color ColorComun = new Color(0.153f, 0.788f, 0.416f);       // #27c96a
        private static readonly Color ColorPocoComun = new Color(0.706f, 0.784f, 0.765f);   // #b4c8c3
        private static readonly Color ColorRara = new Color(0.608f, 0.361f, 0.965f);        // #9b5cf6
        private static readonly Color ColorMitica = new Color(0.910f, 0.659f, 0.125f);      // #e8a820

        private struct MockCard
        {
            public string name;
            public string initials;
            public string rarity;
            public int count;
            public Color rarityColor;

            public MockCard(string n, string ini, string r, int c, Color col)
            {
                name = n;
                initials = ini;
                rarity = r;
                count = c;
                rarityColor = col;
            }
        }

        public static void BuildMyCardsScene()
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
            Sprite iconSearch = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_search.png");
            Sprite iconChevLeft = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_chevron_left.png");
            Sprite iconChevRight = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_chevron_right.png");
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
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.0f;
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
            GameObject controllerGO = new GameObject("MyCardsScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            MyCardsScreenController controller = controllerGO.AddComponent<MyCardsScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. HEADER (Fixed at top: Title + Filter Bar + Counter & Search)
            // ====================================================
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(contentGO.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0, -90); // Safe Area debajo de la barra de estado y cámara
            headerRect.sizeDelta = new Vector2(980, 260);

            // Title "MIS CARTAS" (Barlow Condensed Bold)
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(0, 0);
            titleRect.sizeDelta = new Vector2(0, 56);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "MIS CARTAS";
            titleTMP.fontSize = 50;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // Filter Bar Row (Left arrow, scrollable filter pills, right arrow)
            GameObject filterRowGO = new GameObject("FilterBarRow");
            filterRowGO.transform.SetParent(headerGO.transform, false);
            RectTransform filterRowRect = filterRowGO.AddComponent<RectTransform>();
            filterRowRect.anchorMin = new Vector2(0f, 1f);
            filterRowRect.anchorMax = new Vector2(1f, 1f);
            filterRowRect.pivot = new Vector2(0.5f, 1f);
            filterRowRect.anchoredPosition = new Vector2(0, -80);
            filterRowRect.sizeDelta = new Vector2(0, 60);

            // Left arrow (<)
            GameObject leftArrowGO = new GameObject("LeftArrowBtn");
            leftArrowGO.transform.SetParent(filterRowGO.transform, false);
            RectTransform laRect = leftArrowGO.AddComponent<RectTransform>();
            laRect.anchorMin = new Vector2(0f, 0.5f);
            laRect.anchorMax = new Vector2(0f, 0.5f);
            laRect.pivot = new Vector2(0f, 0.5f);
            laRect.anchoredPosition = new Vector2(0, 0);
            laRect.sizeDelta = new Vector2(32, 48);
            if (iconChevLeft != null)
            {
                Image laImg = leftArrowGO.AddComponent<Image>();
                laImg.sprite = iconChevLeft;
                laImg.preserveAspect = true;
                laImg.color = new Color(1f, 1f, 1f, 0.40f);
            }
            Button leftArrowBtn = leftArrowGO.AddComponent<Button>();

            // Right arrow (>)
            GameObject rightArrowGO = new GameObject("RightArrowBtn");
            rightArrowGO.transform.SetParent(filterRowGO.transform, false);
            RectTransform raRect = rightArrowGO.AddComponent<RectTransform>();
            raRect.anchorMin = new Vector2(1f, 0.5f);
            raRect.anchorMax = new Vector2(1f, 0.5f);
            raRect.pivot = new Vector2(1f, 0.5f);
            raRect.anchoredPosition = new Vector2(0, 0);
            raRect.sizeDelta = new Vector2(32, 48);
            if (iconChevRight != null)
            {
                Image raImg = rightArrowGO.AddComponent<Image>();
                raImg.sprite = iconChevRight;
                raImg.preserveAspect = true;
                raImg.color = new Color(1f, 1f, 1f, 0.40f);
            }
            Button rightArrowBtn = rightArrowGO.AddComponent<Button>();

            // Filter Scroll Container
            GameObject filterScrollGO = new GameObject("FilterScrollView");
            filterScrollGO.transform.SetParent(filterRowGO.transform, false);
            RectTransform fsRect = filterScrollGO.AddComponent<RectTransform>();
            fsRect.anchorMin = Vector2.zero;
            fsRect.anchorMax = Vector2.one;
            fsRect.offsetMin = new Vector2(40, 0);
            fsRect.offsetMax = new Vector2(-40, 0);
            ScrollRect filterScrollRect = filterScrollGO.AddComponent<ScrollRect>();
            filterScrollRect.vertical = false;
            filterScrollRect.horizontal = true;

            GameObject filterContentGO = new GameObject("FilterContent");
            filterContentGO.transform.SetParent(filterScrollGO.transform, false);
            RectTransform fcRect = filterContentGO.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0f, 0.5f);
            fcRect.anchorMax = new Vector2(0f, 0.5f);
            fcRect.pivot = new Vector2(0f, 0.5f);
            fcRect.anchoredPosition = Vector2.zero;
            fcRect.sizeDelta = new Vector2(920, 56);

            HorizontalLayoutGroup fcHlg = filterContentGO.AddComponent<HorizontalLayoutGroup>();
            fcHlg.childAlignment = TextAnchor.MiddleLeft;
            fcHlg.childControlWidth = false;
            fcHlg.childControlHeight = false;
            fcHlg.childForceExpandWidth = false;
            fcHlg.childForceExpandHeight = false;
            fcHlg.spacing = 16f;

            filterScrollRect.content = fcRect;

            string[] filterNames = { "Álbum", "Recientes", "Rareza", "Cantidad", "Nación" };
            float[] filterWidths = { 145f, 175f, 155f, 165f, 150f };
            Button[] filterBtns = new Button[filterNames.Length];

            for (int i = 0; i < filterNames.Length; i++)
            {
                bool isActive = (filterNames[i] == "Rareza");
                GameObject fBtnGO = new GameObject($"FilterBtn_{filterNames[i]}");
                fBtnGO.transform.SetParent(filterContentGO.transform, false);
                RectTransform fbRect = fBtnGO.AddComponent<RectTransform>();
                fbRect.sizeDelta = new Vector2(filterWidths[i], 52);

                RoundedRectGraphic fbG = fBtnGO.AddComponent<RoundedRectGraphic>();
                fbG.IsCapsule = true;
                fbG.color = isActive ? new Color(0.910f, 0.659f, 0.125f, 0.12f) : new Color(1f, 1f, 1f, 0.05f);
                fbG.BorderWidth = isActive ? 2f : 1.2f;
                fbG.BorderColor = isActive ? Gold : BorderSubtle;

                GameObject fbTextGO = new GameObject("Text");
                fbTextGO.transform.SetParent(fBtnGO.transform, false);
                RectTransform fbtRect = fbTextGO.AddComponent<RectTransform>();
                fbtRect.anchorMin = Vector2.zero;
                fbtRect.anchorMax = Vector2.one;
                fbtRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI fbtTMP = fbTextGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) fbtTMP.font = dmSansTMPFont;
                fbtTMP.text = filterNames[i];
                fbtTMP.fontSize = 22;
                fbtTMP.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
                fbtTMP.alignment = TextAlignmentOptions.Center;
                fbtTMP.color = isActive ? Gold : TextGray;

                filterBtns[i] = fBtnGO.AddComponent<Button>();
            }

            // Counter & Search Row
            GameObject countSearchRowGO = new GameObject("CountAndSearchRow");
            countSearchRowGO.transform.SetParent(headerGO.transform, false);
            RectTransform countSearchRowRect = countSearchRowGO.AddComponent<RectTransform>();
            countSearchRowRect.anchorMin = new Vector2(0f, 1f);
            countSearchRowRect.anchorMax = new Vector2(1f, 1f);
            countSearchRowRect.pivot = new Vector2(0.5f, 1f);
            countSearchRowRect.anchoredPosition = new Vector2(0, -160);
            countSearchRowRect.sizeDelta = new Vector2(0, 48);

            GameObject countGO = new GameObject("TotalCountText");
            countGO.transform.SetParent(countSearchRowGO.transform, false);
            RectTransform cRect = countGO.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0.5f);
            cRect.anchorMax = new Vector2(0.6f, 0.5f);
            cRect.pivot = new Vector2(0f, 0.5f);
            cRect.anchoredPosition = new Vector2(0, 0);
            cRect.sizeDelta = new Vector2(0, 44);
            TextMeshProUGUI countTMP = countGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) countTMP.font = dmSansTMPFont;
            countTMP.text = "1,232 CARTAS";
            countTMP.fontSize = 28;
            countTMP.fontStyle = FontStyles.Bold;
            countTMP.alignment = TextAlignmentOptions.Left;
            countTMP.color = TextGray;

            GameObject searchBtnGO = new GameObject("SearchButton");
            searchBtnGO.transform.SetParent(countSearchRowGO.transform, false);
            RectTransform sRect = searchBtnGO.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(1f, 0.5f);
            sRect.anchorMax = new Vector2(1f, 0.5f);
            sRect.pivot = new Vector2(1f, 0.5f);
            sRect.anchoredPosition = new Vector2(0, 0);
            sRect.sizeDelta = new Vector2(46, 46);
            sRect.pivot = new Vector2(1f, 0.5f);
            sRect.anchoredPosition = new Vector2(0, 0);
            sRect.sizeDelta = new Vector2(40, 40);
            if (iconSearch != null)
            {
                Image searchImg = searchBtnGO.AddComponent<Image>();
                searchImg.sprite = iconSearch;
                searchImg.preserveAspect = true;
                searchImg.color = new Color(1f, 1f, 1f, 0.50f);
            }
            Button searchBtn = searchBtnGO.AddComponent<Button>();

            // ====================================================
            // 2. SCROLLABLE CARD GRID (Placed cleanly below header and above bottom bar)
            // ====================================================
            GameObject gridScrollGO = new GameObject("CardGridScrollView");
            gridScrollGO.transform.SetParent(contentGO.transform, false);
            RectTransform gsRect = gridScrollGO.AddComponent<RectTransform>();
            gsRect.anchorMin = new Vector2(0.5f, 0f);
            gsRect.anchorMax = new Vector2(0.5f, 1f);
            gsRect.pivot = new Vector2(0.5f, 0.5f);
            gsRect.offsetMin = new Vector2(-510, 0);
            gsRect.offsetMax = new Vector2(510, -280);

            ScrollRect cardScrollRect = gridScrollGO.AddComponent<ScrollRect>();
            cardScrollRect.horizontal = false;
            cardScrollRect.vertical = true;
            cardScrollRect.scrollSensitivity = 35f;

            // Viewport
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(gridScrollGO.transform, false);
            RectTransform vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            cardScrollRect.viewport = vpRect;

            // Content Container
            GameObject gridContentGO = new GameObject("Content");
            gridContentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform gcRect = gridContentGO.AddComponent<RectTransform>();
            gcRect.anchorMin = new Vector2(0f, 1f);
            gcRect.anchorMax = new Vector2(1f, 1f);
            gcRect.pivot = new Vector2(0.5f, 1f);
            gcRect.anchoredPosition = Vector2.zero;
            gcRect.sizeDelta = new Vector2(0, 4400);

            GridLayoutGroup glg = gridContentGO.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(460, 640); // 3:4 aspect ratio
            glg.spacing = new Vector2(30, 30);
            glg.padding = new RectOffset(35, 35, 20, 260); // 260px de espacio inferior para que las últimas cartas queden 100% visibles sobre la barra
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            ContentSizeFitter csf = gridContentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            cardScrollRect.content = gcRect;

            // Mock Card Data (from docs/Pantallas/src/App.tsx)
            MockCard[] cards = new MockCard[]
            {
                new MockCard("Luis Díaz", "LD", "Mítica", 1, ColorMitica),
                new MockCard("Vinicius Jr.", "VJ", "Rara", 2, ColorRara),
                new MockCard("Haaland", "EH", "Común", 5, ColorComun),
                new MockCard("Mbappé", "KM", "Poco común", 3, ColorPocoComun),
                new MockCard("Pedri", "PE", "Rara", 1, ColorRara),
                new MockCard("Rodri", "RO", "Común", 4, ColorComun),
                new MockCard("Lamine Yamal", "LY", "Mítica", 1, ColorMitica),
                new MockCard("Bellingham", "JB", "Rara", 2, ColorRara),
                new MockCard("Salah", "MS", "Poco común", 6, ColorPocoComun),
                new MockCard("De Bruyne", "KDB", "Rara", 1, ColorRara),
                new MockCard("Musiala", "JM", "Común", 3, ColorComun),
                new MockCard("Osimhen", "VO", "Poco común", 2, ColorPocoComun),
            };

            for (int i = 0; i < cards.Length; i++)
            {
                MockCard card = cards[i];
                GameObject cardGO = new GameObject($"Card_{i + 1}_{card.name}");
                cardGO.transform.SetParent(gridContentGO.transform, false);
                RectTransform cCardRect = cardGO.AddComponent<RectTransform>();

                // Card Base Container (Rounded 24px with Rarity Border and Dark Card Background)
                RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
                cardG.CornerRadius = 24f;
                cardG.color = CardBg;
                cardG.BorderWidth = 2.5f;
                cardG.BorderColor = card.rarityColor;

                // Card Number Badge (Top-Left)
                GameObject numGO = new GameObject("CardNumber");
                numGO.transform.SetParent(cardGO.transform, false);
                RectTransform numRect = numGO.AddComponent<RectTransform>();
                numRect.anchorMin = new Vector2(0f, 1f);
                numRect.anchorMax = new Vector2(0f, 1f);
                numRect.pivot = new Vector2(0f, 1f);
                numRect.anchoredPosition = new Vector2(18, -16);
                numRect.sizeDelta = new Vector2(80, 28);
                TextMeshProUGUI numTMP = numGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) numTMP.font = dmSansTMPFont;
                numTMP.text = $"#{i + 1:D2}";
                numTMP.fontSize = 18;
                numTMP.fontStyle = FontStyles.Bold;
                numTMP.alignment = TextAlignmentOptions.Left;
                numTMP.color = TextDim;

                // Duplicate Badge (Top-Right)
                if (card.count > 1)
                {
                    GameObject dupGO = new GameObject("DuplicateBadge");
                    dupGO.transform.SetParent(cardGO.transform, false);
                    RectTransform dupRect = dupGO.AddComponent<RectTransform>();
                    dupRect.anchorMin = new Vector2(1f, 1f);
                    dupRect.anchorMax = new Vector2(1f, 1f);
                    dupRect.pivot = new Vector2(1f, 1f);
                    dupRect.anchoredPosition = new Vector2(-16, -14);
                    dupRect.sizeDelta = new Vector2(64, 32);

                    RoundedRectGraphic dupG = dupGO.AddComponent<RoundedRectGraphic>();
                    dupG.IsCapsule = true;
                    dupG.color = new Color(0.910f, 0.659f, 0.125f, 0.20f);
                    dupG.BorderWidth = 1.5f;
                    dupG.BorderColor = GoldBorder;

                    GameObject dupTextGO = new GameObject("Text");
                    dupTextGO.transform.SetParent(dupGO.transform, false);
                    RectTransform dtRect = dupTextGO.AddComponent<RectTransform>();
                    dtRect.anchorMin = Vector2.zero;
                    dtRect.anchorMax = Vector2.one;
                    dtRect.sizeDelta = Vector2.zero;
                    TextMeshProUGUI dtTMP = dupTextGO.AddComponent<TextMeshProUGUI>();
                    if (dmSansTMPFont != null) dtTMP.font = dmSansTMPFont;
                    dtTMP.text = $"×{card.count}";
                    dtTMP.fontSize = 18;
                    dtTMP.fontStyle = FontStyles.Bold;
                    dtTMP.alignment = TextAlignmentOptions.Center;
                    dtTMP.color = Gold;
                }

                // Player Avatar Ring (Central emblem with glow)
                GameObject avatarCircleGO = new GameObject("AvatarRing");
                avatarCircleGO.transform.SetParent(cardGO.transform, false);
                RectTransform avRect = avatarCircleGO.AddComponent<RectTransform>();
                avRect.anchorMin = new Vector2(0.5f, 1f);
                avRect.anchorMax = new Vector2(0.5f, 1f);
                avRect.pivot = new Vector2(0.5f, 1f);
                avRect.anchoredPosition = new Vector2(0, -68);
                avRect.sizeDelta = new Vector2(160, 160);

                RoundedRectGraphic avG = avatarCircleGO.AddComponent<RoundedRectGraphic>();
                avG.IsCapsule = true;
                avG.color = new Color(1f, 1f, 1f, 0.06f);
                avG.BorderWidth = 2.5f;
                avG.BorderColor = card.rarityColor;

                GameObject iniTextGO = new GameObject("Initials");
                iniTextGO.transform.SetParent(avatarCircleGO.transform, false);
                RectTransform iniRect = iniTextGO.AddComponent<RectTransform>();
                iniRect.anchorMin = Vector2.zero;
                iniRect.anchorMax = Vector2.one;
                iniRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI iniTMP = iniTextGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) iniTMP.font = dmSansTMPFont;
                iniTMP.text = card.initials;
                iniTMP.fontSize = 46;
                iniTMP.fontStyle = FontStyles.Bold;
                iniTMP.alignment = TextAlignmentOptions.Center;
                iniTMP.color = card.rarityColor;

                // Player Name
                GameObject pNameGO = new GameObject("PlayerName");
                pNameGO.transform.SetParent(cardGO.transform, false);
                RectTransform pnRect = pNameGO.AddComponent<RectTransform>();
                pnRect.anchorMin = new Vector2(0.04f, 0.28f);
                pnRect.anchorMax = new Vector2(0.96f, 0.44f);
                pnRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI pnTMP = pNameGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) pnTMP.font = dmSansTMPFont;
                pnTMP.text = card.name;
                pnTMP.fontSize = 30;
                pnTMP.fontStyle = FontStyles.Bold;
                pnTMP.alignment = TextAlignmentOptions.Center;
                pnTMP.color = TextWhite;

                // Rarity Pill Badge (Bottom)
                GameObject rPillGO = new GameObject("RarityPill");
                rPillGO.transform.SetParent(cardGO.transform, false);
                RectTransform rpRect = rPillGO.AddComponent<RectTransform>();
                rpRect.anchorMin = new Vector2(0.5f, 0f);
                rpRect.anchorMax = new Vector2(0.5f, 0f);
                rpRect.pivot = new Vector2(0.5f, 0f);
                rpRect.anchoredPosition = new Vector2(0, 36);
                rpRect.sizeDelta = new Vector2(240, 46);

                RoundedRectGraphic rpG = rPillGO.AddComponent<RoundedRectGraphic>();
                rpG.IsCapsule = true;
                rpG.color = new Color(card.rarityColor.r, card.rarityColor.g, card.rarityColor.b, 0.12f);
                rpG.BorderWidth = 1.5f;
                rpG.BorderColor = card.rarityColor;

                GameObject rlTextGO = new GameObject("Text");
                rlTextGO.transform.SetParent(rPillGO.transform, false);
                RectTransform rltRect = rlTextGO.AddComponent<RectTransform>();
                rltRect.anchorMin = Vector2.zero;
                rltRect.anchorMax = Vector2.one;
                rltRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI rlTMP = rlTextGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) rlTMP.font = dmSansTMPFont;
                rlTMP.text = card.rarity.ToUpper();
                rlTMP.fontSize = 20;
                rlTMP.fontStyle = FontStyles.Bold;
                rlTMP.characterSpacing = 3f;
                rlTMP.alignment = TextAlignmentOptions.Center;
                rlTMP.color = card.rarityColor;

                cardGO.AddComponent<Button>();
            }

            // ====================================================
            // 3. LIQUID-GLASS BOTTOM NAVIGATION BAR (Tab Cartas Active)
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
                bool isTabActive = (i == 1); // "Mis cartas" is Active
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
            so.FindProperty("totalCardsCountText").objectReferenceValue = countTMP;
            so.FindProperty("searchButton").objectReferenceValue = searchBtn;

            so.FindProperty("scrollLeftBtn").objectReferenceValue = leftArrowBtn;
            so.FindProperty("scrollRightBtn").objectReferenceValue = rightArrowBtn;
            so.FindProperty("filterScrollRect").objectReferenceValue = filterScrollRect;

            SerializedProperty fbProp = so.FindProperty("filterButtons");
            fbProp.arraySize = filterBtns.Length;
            for (int i = 0; i < filterBtns.Length; i++)
            {
                fbProp.GetArrayElementAtIndex(i).objectReferenceValue = filterBtns[i];
            }

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];

            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/MyCardsUI.prefab";
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
            if (File.Exists("Assets/_Project/Scenes/CommunityScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/CommunityScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/VitrinesScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/VitrinesScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/TradeScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/TradeScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/MarketScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MarketScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/FriendsScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/FriendsScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Mis Cartas calibrada con 5 Pestañas (MyCardsScene)!</color>");
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
