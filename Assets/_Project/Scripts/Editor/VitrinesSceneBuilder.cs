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
    public static class VitrinesSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/VitrinesScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Exact Design Tokens from docs/Pantallas 2.0/src/App.tsx
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);   // rgba(14,32,22,0.85)

        public static void BuildVitrinesScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("JuegoTCG", "Por favor sal del modo Play (detén la ejecución) antes de generar la escena.", "Entendido");
                return;
            }

            ProceduralAssetGenerator.GenerateUISprites();
            ConfigureFontImporters();
            AssetDatabase.Refresh();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconSearch = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_search.png");
            Sprite iconBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_back.png");
            Sprite iconLike = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_like.png");
            Sprite iconClose = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_close.png");
            Sprite iconStar = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_star.png");

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

            // Canvas (1080x2400 Match Width for AAA Mobile Display)
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
            GameObject controllerGO = new GameObject("VitrinesScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            VitrinesScreenController controller = controllerGO.AddComponent<VitrinesScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. TOP HEADER (Back Arrow + "VITRINAS PÚBLICAS")
            // ====================================================
            GameObject topBarGO = new GameObject("TopHeader");
            topBarGO.transform.SetParent(contentGO.transform, false);
            RectTransform topBarRect = topBarGO.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0.5f, 1f);
            topBarRect.anchorMax = new Vector2(0.5f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0, -80); // Safe Area
            topBarRect.sizeDelta = new Vector2(1000, 90);

            // Back Button Left
            GameObject backBtnGO = new GameObject("BackButton");
            backBtnGO.transform.SetParent(topBarGO.transform, false);
            RectTransform backBtnRect = backBtnGO.AddComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0f, 0.5f);
            backBtnRect.anchorMax = new Vector2(0f, 0.5f);
            backBtnRect.pivot = new Vector2(0f, 0.5f);
            backBtnRect.anchoredPosition = new Vector2(0, 0);
            backBtnRect.sizeDelta = new Vector2(56, 56);

            Image backImg = backBtnGO.AddComponent<Image>();
            if (iconBack != null) backImg.sprite = iconBack;
            backImg.color = TextWhite;
            Button backBtn = backBtnGO.AddComponent<Button>();

            // Title: "VITRINAS PÚBLICAS"
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(topBarGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(74, 0);
            titleRect.sizeDelta = new Vector2(0, 80);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleTMP.font = barlowTMPFont;
            titleTMP.text = "VITRINAS PÚBLICAS";
            titleTMP.fontSize = 48;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // ====================================================
            // 2. SEARCH INPUT FIELD
            // ====================================================
            GameObject searchBoxGO = new GameObject("SearchInputBox");
            searchBoxGO.transform.SetParent(contentGO.transform, false);
            RectTransform searchRect = searchBoxGO.AddComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0.5f, 1f);
            searchRect.anchorMax = new Vector2(0.5f, 1f);
            searchRect.pivot = new Vector2(0.5f, 1f);
            searchRect.anchoredPosition = new Vector2(0, -180);
            searchRect.sizeDelta = new Vector2(1000, 84);

            RoundedRectGraphic searchG = searchBoxGO.AddComponent<RoundedRectGraphic>();
            searchG.CornerRadius = 20f;
            searchG.color = new Color(1f, 1f, 1f, 0.05f);
            searchG.BorderWidth = 1.5f;
            searchG.BorderColor = BorderSubtle;

            GameObject sIconGO = new GameObject("SearchIcon");
            sIconGO.transform.SetParent(searchBoxGO.transform, false);
            RectTransform sIconRect = sIconGO.AddComponent<RectTransform>();
            sIconRect.anchorMin = new Vector2(0f, 0.5f);
            sIconRect.anchorMax = new Vector2(0f, 0.5f);
            sIconRect.pivot = new Vector2(0f, 0.5f);
            sIconRect.anchoredPosition = new Vector2(24, 0);
            sIconRect.sizeDelta = new Vector2(40, 40);
            Image sIconImg = sIconGO.AddComponent<Image>();
            if (iconSearch != null) sIconImg.sprite = iconSearch;
            sIconImg.color = new Color(1f, 1f, 1f, 0.45f);

            GameObject inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(searchBoxGO.transform, false);
            RectTransform inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.anchoredPosition = new Vector2(80, 0);
            inputRect.sizeDelta = new Vector2(-100, 0);

            TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();

            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            RectTransform phRect = placeholderGO.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI phTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) phTMP.font = dmSansTMPFont;
            phTMP.text = "Busca por usuario o código de amigo…";
            phTMP.fontSize = 24;
            phTMP.color = new Color(1f, 1f, 1f, 0.35f);
            phTMP.alignment = TextAlignmentOptions.Left;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            RectTransform tRect = textGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI inputTMP = textGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) inputTMP.font = dmSansTMPFont;
            inputTMP.fontSize = 24;
            inputTMP.color = TextWhite;
            inputTMP.alignment = TextAlignmentOptions.Left;

            inputField.textViewport = inputRect;
            inputField.textComponent = inputTMP;
            inputField.placeholder = phTMP;

            // ====================================================
            // 3. SCROLLABLE VITRINES VIEW (Populares & Amigos)
            // ====================================================
            GameObject vitrinesScrollGO = new GameObject("VitrinesScrollView");
            vitrinesScrollGO.transform.SetParent(contentGO.transform, false);
            RectTransform vsRect = vitrinesScrollGO.AddComponent<RectTransform>();
            vsRect.anchorMin = new Vector2(0.5f, 0f);
            vsRect.anchorMax = new Vector2(0.5f, 1f);
            vsRect.pivot = new Vector2(0.5f, 0.5f);
            vsRect.offsetMin = new Vector2(-510, 0);
            vsRect.offsetMax = new Vector2(510, -280);

            ScrollRect vitrinesScrollRect = vitrinesScrollGO.AddComponent<ScrollRect>();
            vitrinesScrollRect.horizontal = false;
            vitrinesScrollRect.vertical = true;
            vitrinesScrollRect.scrollSensitivity = 35f;

            // Viewport
            GameObject vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(vitrinesScrollGO.transform, false);
            RectTransform vpRect = vpGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpGO.AddComponent<RectMask2D>();
            vitrinesScrollRect.viewport = vpRect;

            // Scroll Content Holder
            GameObject scrollContentGO = new GameObject("Content");
            scrollContentGO.transform.SetParent(vpGO.transform, false);
            RectTransform scRect = scrollContentGO.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0f, 1f);
            scRect.anchorMax = new Vector2(1f, 1f);
            scRect.pivot = new Vector2(0.5f, 1f);
            scRect.anchoredPosition = Vector2.zero;
            scRect.sizeDelta = new Vector2(0, 1600);

            VerticalLayoutGroup vlg = scrollContentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 40f;
            vlg.padding = new RectOffset(10, 10, 10, 260); // 260px espacio inferior

            ContentSizeFitter csf = scrollContentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            vitrinesScrollRect.content = scRect;

            // ====================================================
            // SECTION "POPULARES" (4 Cards Grid)
            // ====================================================
            GameObject sec1GO = new GameObject("Section_Populares");
            sec1GO.transform.SetParent(scrollContentGO.transform, false);
            RectTransform sec1Rect = sec1GO.AddComponent<RectTransform>();
            sec1Rect.sizeDelta = new Vector2(1000, 580);

            GameObject sec1TitleGO = new GameObject("Title");
            sec1TitleGO.transform.SetParent(sec1GO.transform, false);
            RectTransform sec1TitleRect = sec1TitleGO.AddComponent<RectTransform>();
            sec1TitleRect.anchorMin = new Vector2(0f, 1f);
            sec1TitleRect.anchorMax = new Vector2(1f, 1f);
            sec1TitleRect.pivot = new Vector2(0f, 1f);
            sec1TitleRect.anchoredPosition = new Vector2(10, 0);
            sec1TitleRect.sizeDelta = new Vector2(0, 44);
            TextMeshProUGUI sec1TitleTMP = sec1TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec1TitleTMP.font = barlowTMPFont;
            sec1TitleTMP.text = "POPULARES";
            sec1TitleTMP.fontSize = 34;
            sec1TitleTMP.fontStyle = FontStyles.Bold;
            sec1TitleTMP.characterSpacing = 8f;
            sec1TitleTMP.color = TextWhite;

            GameObject popGridGO = new GameObject("Grid");
            popGridGO.transform.SetParent(sec1GO.transform, false);
            RectTransform popGridRect = popGridGO.AddComponent<RectTransform>();
            popGridRect.anchorMin = new Vector2(0f, 1f);
            popGridRect.anchorMax = new Vector2(1f, 1f);
            popGridRect.pivot = new Vector2(0.5f, 1f);
            popGridRect.anchoredPosition = new Vector2(0, -55);
            popGridRect.sizeDelta = new Vector2(1000, 510);

            GridLayoutGroup popGLG = popGridGO.AddComponent<GridLayoutGroup>();
            popGLG.cellSize = new Vector2(485, 240);
            popGLG.spacing = new Vector2(30, 24);
            popGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            popGLG.constraintCount = 2;

            List<VitrineCardView> popularViews = new List<VitrineCardView>();
            for (int i = 0; i < 4; i++)
            {
                GameObject cardGO = CreateVitrineCardGO(popGridGO.transform, $"PopCard_{i}", 485, 240, dmSansTMPFont, iconLike);
                popularViews.Add(cardGO.GetComponent<VitrineCardView>());
            }

            // ====================================================
            // SECTION "AMIGOS" (2 Cards Grid)
            // ====================================================
            GameObject sec2GO = new GameObject("Section_Amigos");
            sec2GO.transform.SetParent(scrollContentGO.transform, false);
            RectTransform sec2Rect = sec2GO.AddComponent<RectTransform>();
            sec2Rect.sizeDelta = new Vector2(1000, 340);

            GameObject sec2TitleGO = new GameObject("Title");
            sec2TitleGO.transform.SetParent(sec2GO.transform, false);
            RectTransform sec2TitleRect = sec2TitleGO.AddComponent<RectTransform>();
            sec2TitleRect.anchorMin = new Vector2(0f, 1f);
            sec2TitleRect.anchorMax = new Vector2(1f, 1f);
            sec2TitleRect.pivot = new Vector2(0f, 1f);
            sec2TitleRect.anchoredPosition = new Vector2(10, 0);
            sec2TitleRect.sizeDelta = new Vector2(0, 44);
            TextMeshProUGUI sec2TitleTMP = sec2TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec2TitleTMP.font = barlowTMPFont;
            sec2TitleTMP.text = "AMIGOS";
            sec2TitleTMP.fontSize = 34;
            sec2TitleTMP.fontStyle = FontStyles.Bold;
            sec2TitleTMP.characterSpacing = 8f;
            sec2TitleTMP.color = TextWhite;

            GameObject friendGridGO = new GameObject("Grid");
            friendGridGO.transform.SetParent(sec2GO.transform, false);
            RectTransform friendGridRect = friendGridGO.AddComponent<RectTransform>();
            friendGridRect.anchorMin = new Vector2(0f, 1f);
            friendGridRect.anchorMax = new Vector2(1f, 1f);
            friendGridRect.pivot = new Vector2(0.5f, 1f);
            friendGridRect.anchoredPosition = new Vector2(0, -55);
            friendGridRect.sizeDelta = new Vector2(1000, 250);

            GridLayoutGroup friendGLG = friendGridGO.AddComponent<GridLayoutGroup>();
            friendGLG.cellSize = new Vector2(485, 240);
            friendGLG.spacing = new Vector2(30, 24);
            friendGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            friendGLG.constraintCount = 2;

            List<VitrineCardView> friendViews = new List<VitrineCardView>();
            for (int i = 0; i < 2; i++)
            {
                GameObject cardGO = CreateVitrineCardGO(friendGridGO.transform, $"FriendCard_{i}", 485, 240, dmSansTMPFont, iconLike);
                friendViews.Add(cardGO.GetComponent<VitrineCardView>());
            }

            // ====================================================
            // 4. VITRINE DETAIL MODAL / POPUP
            // ====================================================
            GameObject detailModalGO = new GameObject("VitrineDetailModal");
            detailModalGO.transform.SetParent(canvasGO.transform, false);
            RectTransform dmRect = detailModalGO.AddComponent<RectTransform>();
            dmRect.anchorMin = Vector2.zero;
            dmRect.anchorMax = Vector2.one;
            dmRect.sizeDelta = Vector2.zero;
            VitrineDetailController detailCtrl = detailModalGO.AddComponent<VitrineDetailController>();

            // Detail BG (100% opaque, no bleed-through from behind)
            Image dmBg = detailModalGO.AddComponent<Image>();
            if (tacticalPitchSprite != null)
            {
                dmBg.sprite = tacticalPitchSprite;
                dmBg.type = Image.Type.Simple;
            }
            dmBg.color = new Color(0.043f, 0.082f, 0.059f, 1f);

            // Detail Header
            GameObject dhGO = new GameObject("Header");
            dhGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform dhRect = dhGO.AddComponent<RectTransform>();
            dhRect.anchorMin = new Vector2(0.5f, 1f);
            dhRect.anchorMax = new Vector2(0.5f, 1f);
            dhRect.pivot = new Vector2(0.5f, 1f);
            dhRect.anchoredPosition = new Vector2(0, -80);
            dhRect.sizeDelta = new Vector2(1000, 110);

            // Avatar Circle
            GameObject dAvatarGO = new GameObject("AvatarCircle");
            dAvatarGO.transform.SetParent(dhGO.transform, false);
            RectTransform daRect = dAvatarGO.AddComponent<RectTransform>();
            daRect.anchorMin = new Vector2(0f, 0.5f);
            daRect.anchorMax = new Vector2(0f, 0.5f);
            daRect.pivot = new Vector2(0f, 0.5f);
            daRect.anchoredPosition = new Vector2(10, 0);
            daRect.sizeDelta = new Vector2(88, 88);
            RoundedRectGraphic daG = dAvatarGO.AddComponent<RoundedRectGraphic>();
            daG.IsCapsule = true;
            daG.color = new Color(1f, 1f, 1f, 0.08f);
            daG.BorderWidth = 1.5f;
            daG.BorderColor = new Color(1f, 1f, 1f, 0.25f);

            GameObject daTextGO = new GameObject("Text");
            daTextGO.transform.SetParent(dAvatarGO.transform, false);
            RectTransform datRect = daTextGO.AddComponent<RectTransform>();
            datRect.anchorMin = Vector2.zero;
            datRect.anchorMax = Vector2.one;
            datRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI daTMP = daTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) daTMP.font = dmSansTMPFont;
            daTMP.text = "PP";
            daTMP.fontSize = 32;
            daTMP.fontStyle = FontStyles.Bold;
            daTMP.alignment = TextAlignmentOptions.Center;
            daTMP.color = TextWhite;

            // User Info text
            GameObject dUserGO = new GameObject("UserName");
            dUserGO.transform.SetParent(dhGO.transform, false);
            RectTransform duRect = dUserGO.AddComponent<RectTransform>();
            duRect.anchorMin = new Vector2(0f, 1f);
            duRect.anchorMax = new Vector2(0.8f, 1f);
            duRect.pivot = new Vector2(0f, 1f);
            duRect.anchoredPosition = new Vector2(115, -8);
            duRect.sizeDelta = new Vector2(0, 48);
            TextMeshProUGUI duTMP = dUserGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) duTMP.font = barlowTMPFont;
            duTMP.text = "PROPLAYER_99";
            duTMP.fontSize = 44;
            duTMP.fontStyle = FontStyles.Bold;
            duTMP.characterSpacing = 3f;
            duTMP.color = TextWhite;

            GameObject dCountGO = new GameObject("CardCount");
            dCountGO.transform.SetParent(dhGO.transform, false);
            RectTransform dcRect = dCountGO.AddComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0f, 0f);
            dcRect.anchorMax = new Vector2(0.8f, 0f);
            dcRect.pivot = new Vector2(0f, 0f);
            dcRect.anchoredPosition = new Vector2(115, 14);
            dcRect.sizeDelta = new Vector2(0, 32);
            TextMeshProUGUI dcTMP = dCountGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) dcTMP.font = dmSansTMPFont;
            dcTMP.text = "Vitrina pública · 6 cartas";
            dcTMP.fontSize = 24;
            dcTMP.color = TextDim;

            // Close button (X)
            GameObject dCloseGO = new GameObject("CloseButton");
            dCloseGO.transform.SetParent(dhGO.transform, false);
            RectTransform dCloseRect = dCloseGO.AddComponent<RectTransform>();
            dCloseRect.anchorMin = new Vector2(1f, 0.5f);
            dCloseRect.anchorMax = new Vector2(1f, 0.5f);
            dCloseRect.pivot = new Vector2(1f, 0.5f);
            dCloseRect.anchoredPosition = new Vector2(-10, 0);
            dCloseRect.sizeDelta = new Vector2(56, 56);
            Image dCloseImg = dCloseGO.AddComponent<Image>();
            if (iconClose != null) dCloseImg.sprite = iconClose;
            dCloseImg.color = new Color(1f, 1f, 1f, 0.7f);
            Button dCloseBtn = dCloseGO.AddComponent<Button>();

            // Detail Cards Grid (2 cols x 3 rows = 6 Cards matching Figma)
            GameObject dCardsGridGO = new GameObject("ShowcaseCardsGrid");
            dCardsGridGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform dcgRect = dCardsGridGO.AddComponent<RectTransform>();
            dcgRect.anchorMin = new Vector2(0.5f, 1f);
            dcgRect.anchorMax = new Vector2(0.5f, 1f);
            dcgRect.pivot = new Vector2(0.5f, 1f);
            dcgRect.anchoredPosition = new Vector2(-6, -205);
            dcgRect.sizeDelta = new Vector2(980, 1800);

            GridLayoutGroup dcgGLG = dCardsGridGO.AddComponent<GridLayoutGroup>();
            dcgGLG.cellSize = new Vector2(470, 580);
            dcgGLG.spacing = new Vector2(32, 26);
            dcgGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            dcgGLG.constraintCount = 2;

            // 6 Distinct Showcase Cards matching Figma!
            CreateShowcaseMiniCard(dCardsGridGO.transform, "Card_0", "EH", "MÍTICA", "Haaland", Gold, true, barlowTMPFont, dmSansTMPFont, iconStar);
            CreateShowcaseMiniCard(dCardsGridGO.transform, "Card_1", "KM", "RARA", "Mbappé", new Color(0.678f, 0.369f, 0.941f), false, barlowTMPFont, dmSansTMPFont, iconStar);
            CreateShowcaseMiniCard(dCardsGridGO.transform, "Card_2", "KDB", "RARA", "De Bruyne", new Color(0.678f, 0.369f, 0.941f), false, barlowTMPFont, dmSansTMPFont, iconStar);
            CreateShowcaseMiniCard(dCardsGridGO.transform, "Card_3", "MS", "POCO COMÚN", "Salah", new Color(0.706f, 0.784f, 0.765f), false, barlowTMPFont, dmSansTMPFont, iconStar);
            CreateShowcaseMiniCard(dCardsGridGO.transform, "Card_4", "PE", "RARA", "Pedri", new Color(0.678f, 0.369f, 0.941f), false, barlowTMPFont, dmSansTMPFont, iconStar);
            CreateShowcaseMiniCard(dCardsGridGO.transform, "Card_5", "RO", "COMÚN", "Rodri", new Color(0.153f, 0.788f, 0.416f), false, barlowTMPFont, dmSansTMPFont, iconStar);

            // Right Scroll Indicator (matching Figma)
            GameObject scrollBarGO = new GameObject("RightScrollIndicator");
            scrollBarGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform sbRect = scrollBarGO.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 1f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 1f);
            sbRect.anchoredPosition = new Vector2(-10, -210);
            sbRect.sizeDelta = new Vector2(6, 1760);

            Image sbTrack = scrollBarGO.AddComponent<Image>();
            sbTrack.color = new Color(0.910f, 0.659f, 0.125f, 0.15f);

            GameObject sbThumbGO = new GameObject("Thumb");
            sbThumbGO.transform.SetParent(scrollBarGO.transform, false);
            RectTransform sbtRect = sbThumbGO.AddComponent<RectTransform>();
            sbtRect.anchorMin = new Vector2(0f, 1f);
            sbtRect.anchorMax = new Vector2(1f, 1f);
            sbtRect.pivot = new Vector2(0.5f, 1f);
            sbtRect.anchoredPosition = Vector2.zero;
            sbtRect.sizeDelta = new Vector2(0, 320);

            Image sbThumb = sbThumbGO.AddComponent<Image>();
            sbThumb.color = Gold;

            GameObject topArrowGO = new GameObject("TopArrow");
            topArrowGO.transform.SetParent(scrollBarGO.transform, false);
            RectTransform taRect = topArrowGO.AddComponent<RectTransform>();
            taRect.anchorMin = new Vector2(0.5f, 1f);
            taRect.anchorMax = new Vector2(0.5f, 1f);
            taRect.pivot = new Vector2(0.5f, 0f);
            taRect.anchoredPosition = new Vector2(0, 8);
            taRect.sizeDelta = new Vector2(18, 18);
            TextMeshProUGUI taTMP = topArrowGO.AddComponent<TextMeshProUGUI>();
            taTMP.text = "▲";
            taTMP.fontSize = 14;
            taTMP.color = Gold;
            taTMP.alignment = TextAlignmentOptions.Center;

            // Floating Like Pill (Bottom Right, matching Figma)
            GameObject dLikeRowGO = new GameObject("FloatingLikePill");
            dLikeRowGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform dlrRect = dLikeRowGO.AddComponent<RectTransform>();
            dlrRect.anchorMin = new Vector2(1f, 0f);
            dlrRect.anchorMax = new Vector2(1f, 0f);
            dlrRect.pivot = new Vector2(1f, 0f);
            dlrRect.anchoredPosition = new Vector2(-46, 215);
            dlrRect.sizeDelta = new Vector2(185, 78);

            RoundedRectGraphic dlrG = dLikeRowGO.AddComponent<RoundedRectGraphic>();
            dlrG.IsCapsule = true;
            dlrG.color = new Color(0.04f, 0.09f, 0.06f, 0.94f);
            dlrG.BorderWidth = 2.0f;
            dlrG.BorderColor = Gold;

            Button dLikeBtn = dLikeRowGO.AddComponent<Button>();

            GameObject dlContentGO = new GameObject("Content");
            dlContentGO.transform.SetParent(dLikeRowGO.transform, false);
            RectTransform dlcRect = dlContentGO.AddComponent<RectTransform>();
            dlcRect.anchorMin = Vector2.zero;
            dlcRect.anchorMax = Vector2.one;
            dlcRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup dlhlg = dlContentGO.AddComponent<HorizontalLayoutGroup>();
            dlhlg.childAlignment = TextAnchor.MiddleCenter;
            dlhlg.childControlWidth = false;
            dlhlg.childControlHeight = false;
            dlhlg.spacing = 12f;

            GameObject dlIconGO = new GameObject("LikeIcon");
            dlIconGO.transform.SetParent(dlContentGO.transform, false);
            RectTransform dliRect = dlIconGO.AddComponent<RectTransform>();
            dliRect.sizeDelta = new Vector2(34, 34);
            Image dliImg = dlIconGO.AddComponent<Image>();
            if (iconLike != null) dliImg.sprite = iconLike;
            dliImg.color = Gold;
            dliImg.raycastTarget = false;

            GameObject dlTextGO = new GameObject("LikeText");
            dlTextGO.transform.SetParent(dlContentGO.transform, false);
            RectTransform dltRect = dlTextGO.AddComponent<RectTransform>();
            dltRect.sizeDelta = new Vector2(85, 42);
            TextMeshProUGUI dltTMP = dlTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) dltTMP.font = barlowTMPFont;
            dltTMP.text = "234";
            dltTMP.fontSize = 34;
            dltTMP.fontStyle = FontStyles.Bold;
            dltTMP.color = Gold;
            dltTMP.alignment = TextAlignmentOptions.Left;
            dltTMP.raycastTarget = false;

            // Modal Starts Hidden so Catalog is visible by default
            detailModalGO.SetActive(false);

            // ====================================================
            // 5. BOTTOM NAVIGATION BAR (Tab Comunidad Active)
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
                bool isTabActive = (i == 3); // Comunidad Active
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

            // Assign Serialized Properties on Controller
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("searchInputField").objectReferenceValue = inputField;
            so.FindProperty("detailModal").objectReferenceValue = detailCtrl;

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];

            SerializedProperty popListProp = so.FindProperty("popularCardViews");
            popListProp.ClearArray();
            for (int i = 0; i < popularViews.Count; i++)
            {
                popListProp.InsertArrayElementAtIndex(i);
                popListProp.GetArrayElementAtIndex(i).objectReferenceValue = popularViews[i];
            }

            SerializedProperty friendListProp = so.FindProperty("friendCardViews");
            friendListProp.ClearArray();
            for (int i = 0; i < friendViews.Count; i++)
            {
                friendListProp.InsertArrayElementAtIndex(i);
                friendListProp.GetArrayElementAtIndex(i).objectReferenceValue = friendViews[i];
            }

            so.ApplyModifiedProperties();

            // Assign Detail Controller fields
            SerializedObject soDetail = new SerializedObject(detailCtrl);
            soDetail.FindProperty("detailRoot").objectReferenceValue = detailModalGO;
            soDetail.FindProperty("avatarText").objectReferenceValue = daTMP;
            soDetail.FindProperty("userNameText").objectReferenceValue = duTMP;
            soDetail.FindProperty("cardCountText").objectReferenceValue = dcTMP;
            soDetail.FindProperty("closeButton").objectReferenceValue = dCloseBtn;

            soDetail.FindProperty("likeButton").objectReferenceValue = dLikeBtn;
            soDetail.FindProperty("likeCountText").objectReferenceValue = dltTMP;
            soDetail.FindProperty("likeIconImage").objectReferenceValue = dliImg;
            soDetail.FindProperty("likePillGraphic").objectReferenceValue = dlrG;

            soDetail.FindProperty("cardsGridParent").objectReferenceValue = dCardsGridGO.transform;
            soDetail.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/VitrinesScreenUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

            // Save Scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register in Build Settings
            JuegoTCG.Editor.AutoRegisterBuildScenes.RegisterScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Vitrinas Públicas generada con calidad AAA en VitrinesScene.unity!</color>");
        }

        private static GameObject CreateVitrineCardGO(Transform parent, string name, float w, float h, TMP_FontAsset font, Sprite likeSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cRect = cardGO.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(w, h);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 24f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.5f;
            cardG.BorderColor = BorderSubtle;

            VitrineCardView cardView = cardGO.AddComponent<VitrineCardView>();
            Button cardBtn = cardGO.AddComponent<Button>();

            // Avatar Circle (Left)
            GameObject avGO = new GameObject("AvatarCircle");
            avGO.transform.SetParent(cardGO.transform, false);
            RectTransform avRect = avGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(20, 0);
            avRect.sizeDelta = new Vector2(76, 76);

            RoundedRectGraphic avG = avGO.AddComponent<RoundedRectGraphic>();
            avG.IsCapsule = true;
            avG.color = new Color(1f, 1f, 1f, 0.08f);
            avG.BorderWidth = 1.5f;
            avG.BorderColor = GoldBorder;

            GameObject avTextGO = new GameObject("Text");
            avTextGO.transform.SetParent(avGO.transform, false);
            RectTransform avtRect = avTextGO.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI avTMP = avTextGO.AddComponent<TextMeshProUGUI>();
            if (font != null) avTMP.font = font;
            avTMP.text = "CR";
            avTMP.fontSize = 28;
            avTMP.fontStyle = FontStyles.Bold;
            avTMP.alignment = TextAlignmentOptions.Center;
            avTMP.color = TextWhite;

            // Info Column (Center)
            GameObject uNameGO = new GameObject("UserName");
            uNameGO.transform.SetParent(cardGO.transform, false);
            RectTransform unRect = uNameGO.AddComponent<RectTransform>();
            unRect.anchorMin = new Vector2(0f, 0.5f);
            unRect.anchorMax = new Vector2(1f, 0.5f);
            unRect.pivot = new Vector2(0f, 0.5f);
            unRect.anchoredPosition = new Vector2(110, 16);
            unRect.sizeDelta = new Vector2(-190, 32);
            TextMeshProUGUI unTMP = uNameGO.AddComponent<TextMeshProUGUI>();
            if (font != null) unTMP.font = font;
            unTMP.text = "Carlos_R";
            unTMP.fontSize = 26;
            unTMP.fontStyle = FontStyles.Bold;
            unTMP.alignment = TextAlignmentOptions.Left;
            unTMP.color = TextWhite;

            GameObject uCountGO = new GameObject("CardCount");
            uCountGO.transform.SetParent(cardGO.transform, false);
            RectTransform ucRect = uCountGO.AddComponent<RectTransform>();
            ucRect.anchorMin = new Vector2(0f, 0.5f);
            ucRect.anchorMax = new Vector2(1f, 0.5f);
            ucRect.pivot = new Vector2(0f, 0.5f);
            ucRect.anchoredPosition = new Vector2(110, -18);
            ucRect.sizeDelta = new Vector2(-190, 26);
            TextMeshProUGUI ucTMP = uCountGO.AddComponent<TextMeshProUGUI>();
            if (font != null) ucTMP.font = font;
            ucTMP.text = "6 cartas";
            ucTMP.fontSize = 20;
            ucTMP.alignment = TextAlignmentOptions.Left;
            ucTMP.color = TextDim;

            // Likes Pill (Right)
            GameObject likePillGO = new GameObject("LikePill");
            likePillGO.transform.SetParent(cardGO.transform, false);
            RectTransform lpRect = likePillGO.AddComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(1f, 0.5f);
            lpRect.anchorMax = new Vector2(1f, 0.5f);
            lpRect.pivot = new Vector2(1f, 0.5f);
            lpRect.anchoredPosition = new Vector2(-16, 0);
            lpRect.sizeDelta = new Vector2(76, 52);

            RoundedRectGraphic lpG = likePillGO.AddComponent<RoundedRectGraphic>();
            lpG.CornerRadius = 12f;
            lpG.color = new Color(0.910f, 0.659f, 0.125f, 0.12f);
            lpG.BorderWidth = 1.2f;
            lpG.BorderColor = GoldBorder;

            GameObject lCountGO = new GameObject("CountText");
            lCountGO.transform.SetParent(likePillGO.transform, false);
            RectTransform lcRect = lCountGO.AddComponent<RectTransform>();
            lcRect.anchorMin = Vector2.zero;
            lcRect.anchorMax = Vector2.one;
            lcRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI lcTMP = lCountGO.AddComponent<TextMeshProUGUI>();
            if (font != null) lcTMP.font = font;
            lcTMP.text = "48";
            lcTMP.fontSize = 22;
            lcTMP.fontStyle = FontStyles.Bold;
            lcTMP.alignment = TextAlignmentOptions.Center;
            lcTMP.color = Gold;

            // Wire CardView fields
            SerializedObject so = new SerializedObject(cardView);
            so.FindProperty("avatarText").objectReferenceValue = avTMP;
            so.FindProperty("userNameText").objectReferenceValue = unTMP;
            so.FindProperty("likesText").objectReferenceValue = lcTMP;
            so.FindProperty("cardButton").objectReferenceValue = cardBtn;
            so.ApplyModifiedProperties();

            return cardGO;
        }

        private static void CreateShowcaseMiniCard(Transform parent, string name, string initials, string rarity, string playerName, Color rarityColor, bool hasStar, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont, Sprite starSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 24f;
            cardG.color = CardBg;
            cardG.BorderWidth = (rarity == "MÍTICA" || rarity == "Mitica") ? 2.5f : 1.8f;
            cardG.BorderColor = rarityColor;

            // Star Icon (Top Right)
            GameObject starGO = new GameObject("StarIcon");
            starGO.transform.SetParent(cardGO.transform, false);
            RectTransform starRect = starGO.AddComponent<RectTransform>();
            starRect.anchorMin = new Vector2(1f, 1f);
            starRect.anchorMax = new Vector2(1f, 1f);
            starRect.pivot = new Vector2(1f, 1f);
            starRect.anchoredPosition = new Vector2(-22, -22);
            starRect.sizeDelta = new Vector2(28, 28);
            Image starImg = starGO.AddComponent<Image>();
            if (starSprite != null) starImg.sprite = starSprite;
            starImg.color = Gold;
            starImg.raycastTarget = false;
            starGO.SetActive(hasStar);

            // Initials Big Athletic Center
            GameObject iniGO = new GameObject("InitialsText");
            iniGO.transform.SetParent(cardGO.transform, false);
            RectTransform iniRect = iniGO.AddComponent<RectTransform>();
            iniRect.anchorMin = new Vector2(0f, 0.48f);
            iniRect.anchorMax = new Vector2(1f, 0.82f);
            iniRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI iniTMP = iniGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) iniTMP.font = barlowFont;
            iniTMP.text = initials;
            iniTMP.fontSize = 96;
            iniTMP.fontStyle = FontStyles.Bold;
            iniTMP.alignment = TextAlignmentOptions.Center;
            iniTMP.color = new Color(1f, 1f, 1f, 0.22f);

            // Rarity Label
            GameObject rarGO = new GameObject("RarityText");
            rarGO.transform.SetParent(cardGO.transform, false);
            RectTransform rarRect = rarGO.AddComponent<RectTransform>();
            rarRect.anchorMin = new Vector2(0f, 0.35f);
            rarRect.anchorMax = new Vector2(1f, 0.47f);
            rarRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rarTMP = rarGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rarTMP.font = barlowFont;
            rarTMP.text = rarity;
            rarTMP.fontSize = 24;
            rarTMP.fontStyle = FontStyles.Bold;
            rarTMP.characterSpacing = 5f;
            rarTMP.alignment = TextAlignmentOptions.Center;
            rarTMP.color = rarityColor;

            // Player Name
            GameObject pNameGO = new GameObject("PlayerNameText");
            pNameGO.transform.SetParent(cardGO.transform, false);
            RectTransform pnRect = pNameGO.AddComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0f, 0.20f);
            pnRect.anchorMax = new Vector2(1f, 0.34f);
            pnRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI pnTMP = pNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) pnTMP.font = dmSansFont;
            pnTMP.text = playerName;
            pnTMP.fontSize = 30;
            pnTMP.fontStyle = FontStyles.Normal;
            pnTMP.alignment = TextAlignmentOptions.Center;
            pnTMP.color = TextWhite;
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
            if (font == null) font = AssetDatabase.LoadAssetAtPath<Font>($"{FontPath}/{fontName}.otf");
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
            return TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
    }
}
#endif
