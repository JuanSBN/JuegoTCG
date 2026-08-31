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
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.50f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);   // rgba(14,32,22,0.85)

        [MenuItem("JuegoTCG/Generar Pantalla de Vitrinas Públicas")]
        public static void BuildVitrinesScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconSearch = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_search.png");
            Sprite iconBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_back.png");
            Sprite iconLike = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_like.png");
            Sprite iconClose = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_close.png");

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

            // Title: "VITRINAS PÚBLICAS"
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
            titleTMP.text = "VITRINAS PÚBLICAS";
            titleTMP.fontSize = 46;
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
            searchRect.anchoredPosition = new Vector2(0, -155);
            searchRect.sizeDelta = new Vector2(980, 80);

            RoundedRectGraphic searchG = searchBoxGO.AddComponent<RoundedRectGraphic>();
            searchG.CornerRadius = 18f;
            searchG.color = new Color(1f, 1f, 1f, 0.05f);
            searchG.BorderWidth = 1.2f;
            searchG.BorderColor = BorderSubtle;

            GameObject sIconGO = new GameObject("SearchIcon");
            sIconGO.transform.SetParent(searchBoxGO.transform, false);
            RectTransform sIconRect = sIconGO.AddComponent<RectTransform>();
            sIconRect.anchorMin = new Vector2(0f, 0.5f);
            sIconRect.anchorMax = new Vector2(0f, 0.5f);
            sIconRect.pivot = new Vector2(0f, 0.5f);
            sIconRect.anchoredPosition = new Vector2(24, 0);
            sIconRect.sizeDelta = new Vector2(36, 36);
            Image sIconImg = sIconGO.AddComponent<Image>();
            if (iconSearch != null) sIconImg.sprite = iconSearch;
            sIconImg.color = new Color(1f, 1f, 1f, 0.40f);

            GameObject inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(searchBoxGO.transform, false);
            RectTransform inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.anchoredPosition = new Vector2(74, 0);
            inputRect.sizeDelta = new Vector2(-90, 0);

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
            // 3. SECTION "POPULARES" (4 Cards Grid)
            // ====================================================
            GameObject sec1GO = new GameObject("Section_Populares");
            sec1GO.transform.SetParent(contentGO.transform, false);
            RectTransform sec1Rect = sec1GO.AddComponent<RectTransform>();
            sec1Rect.anchorMin = new Vector2(0.5f, 1f);
            sec1Rect.anchorMax = new Vector2(0.5f, 1f);
            sec1Rect.pivot = new Vector2(0.5f, 1f);
            sec1Rect.anchoredPosition = new Vector2(0, -260);
            sec1Rect.sizeDelta = new Vector2(980, 560);

            GameObject sec1TitleGO = new GameObject("Title");
            sec1TitleGO.transform.SetParent(sec1GO.transform, false);
            RectTransform sec1TitleRect = sec1TitleGO.AddComponent<RectTransform>();
            sec1TitleRect.anchorMin = new Vector2(0f, 1f);
            sec1TitleRect.anchorMax = new Vector2(1f, 1f);
            sec1TitleRect.pivot = new Vector2(0f, 1f);
            sec1TitleRect.anchoredPosition = new Vector2(0, 0);
            sec1TitleRect.sizeDelta = new Vector2(0, 36);
            TextMeshProUGUI sec1TitleTMP = sec1TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec1TitleTMP.font = barlowTMPFont;
            sec1TitleTMP.text = "POPULARES";
            sec1TitleTMP.fontSize = 30;
            sec1TitleTMP.fontStyle = FontStyles.Bold;
            sec1TitleTMP.characterSpacing = 8f;
            sec1TitleTMP.color = TextWhite;

            List<VitrineCardView> popularViews = new List<VitrineCardView>();
            float vCardW = 475f;
            float vCardH = 225f;
            float vGapX = 30f;
            float vGapY = 24f;

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;
                GameObject cardGO = CreateVitrineCardGO(sec1GO.transform, $"PopCard_{i}", col * (vCardW + vGapX), -50 - row * (vCardH + vGapY), vCardW, vCardH, dmSansTMPFont, iconLike);
                popularViews.Add(cardGO.GetComponent<VitrineCardView>());
            }

            // ====================================================
            // 4. SECTION "AMIGOS" (2 Cards Grid)
            // ====================================================
            GameObject sec2GO = new GameObject("Section_Amigos");
            sec2GO.transform.SetParent(contentGO.transform, false);
            RectTransform sec2Rect = sec2GO.AddComponent<RectTransform>();
            sec2Rect.anchorMin = new Vector2(0.5f, 1f);
            sec2Rect.anchorMax = new Vector2(0.5f, 1f);
            sec2Rect.pivot = new Vector2(0.5f, 1f);
            sec2Rect.anchoredPosition = new Vector2(0, -830);
            sec2Rect.sizeDelta = new Vector2(980, 320);

            GameObject sec2TitleGO = new GameObject("Title");
            sec2TitleGO.transform.SetParent(sec2GO.transform, false);
            RectTransform sec2TitleRect = sec2TitleGO.AddComponent<RectTransform>();
            sec2TitleRect.anchorMin = new Vector2(0f, 1f);
            sec2TitleRect.anchorMax = new Vector2(1f, 1f);
            sec2TitleRect.pivot = new Vector2(0f, 1f);
            sec2TitleRect.anchoredPosition = new Vector2(0, 0);
            sec2TitleRect.sizeDelta = new Vector2(0, 36);
            TextMeshProUGUI sec2TitleTMP = sec2TitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) sec2TitleTMP.font = barlowTMPFont;
            sec2TitleTMP.text = "AMIGOS";
            sec2TitleTMP.fontSize = 30;
            sec2TitleTMP.fontStyle = FontStyles.Bold;
            sec2TitleTMP.characterSpacing = 8f;
            sec2TitleTMP.color = TextWhite;

            List<VitrineCardView> friendViews = new List<VitrineCardView>();
            for (int i = 0; i < 2; i++)
            {
                GameObject cardGO = CreateVitrineCardGO(sec2GO.transform, $"FriendCard_{i}", i * (vCardW + vGapX), -50, vCardW, vCardH, dmSansTMPFont, iconLike);
                friendViews.Add(cardGO.GetComponent<VitrineCardView>());
            }

            // ====================================================
            // 5. VITRINE DETAIL MODAL / POPUP
            // ====================================================
            GameObject detailModalGO = new GameObject("VitrineDetailModal");
            detailModalGO.transform.SetParent(canvasGO.transform, false);
            RectTransform dmRect = detailModalGO.AddComponent<RectTransform>();
            dmRect.anchorMin = Vector2.zero;
            dmRect.anchorMax = Vector2.one;
            dmRect.sizeDelta = Vector2.zero;
            VitrineDetailController detailCtrl = detailModalGO.AddComponent<VitrineDetailController>();

            // Detail BG
            Image dmBg = detailModalGO.AddComponent<Image>();
            dmBg.color = new Color(0.047f, 0.094f, 0.063f, 0.98f);

            // Detail Header
            GameObject dhGO = new GameObject("Header");
            dhGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform dhRect = dhGO.AddComponent<RectTransform>();
            dhRect.anchorMin = new Vector2(0.5f, 1f);
            dhRect.anchorMax = new Vector2(0.5f, 1f);
            dhRect.pivot = new Vector2(0.5f, 1f);
            dhRect.anchoredPosition = new Vector2(0, -60);
            dhRect.sizeDelta = new Vector2(980, 110);

            // Avatar Circle
            GameObject dAvatarGO = new GameObject("AvatarCircle");
            dAvatarGO.transform.SetParent(dhGO.transform, false);
            RectTransform daRect = dAvatarGO.AddComponent<RectTransform>();
            daRect.anchorMin = new Vector2(0f, 0.5f);
            daRect.anchorMax = new Vector2(0f, 0.5f);
            daRect.pivot = new Vector2(0f, 0.5f);
            daRect.anchoredPosition = new Vector2(0, 0);
            daRect.sizeDelta = new Vector2(80, 80);
            RoundedRectGraphic daG = dAvatarGO.AddComponent<RoundedRectGraphic>();
            daG.IsCapsule = true;
            daG.color = new Color(1f, 1f, 1f, 0.08f);
            daG.BorderWidth = 1.5f;
            daG.BorderColor = BorderSubtle;

            GameObject daTextGO = new GameObject("Text");
            daTextGO.transform.SetParent(dAvatarGO.transform, false);
            RectTransform datRect = daTextGO.AddComponent<RectTransform>();
            datRect.anchorMin = Vector2.zero;
            datRect.anchorMax = Vector2.one;
            datRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI daTMP = daTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) daTMP.font = dmSansTMPFont;
            daTMP.text = "PP";
            daTMP.fontSize = 28;
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
            duRect.anchoredPosition = new Vector2(100, -10);
            duRect.sizeDelta = new Vector2(0, 42);
            TextMeshProUGUI duTMP = dUserGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) duTMP.font = barlowTMPFont;
            duTMP.text = "ProPlayer_99";
            duTMP.fontSize = 38;
            duTMP.fontStyle = FontStyles.Bold;
            duTMP.color = TextWhite;

            GameObject dCountGO = new GameObject("CardCount");
            dCountGO.transform.SetParent(dhGO.transform, false);
            RectTransform dcRect = dCountGO.AddComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0f, 0f);
            dcRect.anchorMax = new Vector2(0.8f, 0f);
            dcRect.pivot = new Vector2(0f, 0f);
            dcRect.anchoredPosition = new Vector2(100, 16);
            dcRect.sizeDelta = new Vector2(0, 30);
            TextMeshProUGUI dcTMP = dCountGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) dcTMP.font = dmSansTMPFont;
            dcTMP.text = "Vitrina pública · 6 cartas";
            dcTMP.fontSize = 22;
            dcTMP.color = TextDim;

            // Close button (X)
            GameObject dCloseGO = new GameObject("CloseButton");
            dCloseGO.transform.SetParent(dhGO.transform, false);
            RectTransform dCloseRect = dCloseGO.AddComponent<RectTransform>();
            dCloseRect.anchorMin = new Vector2(1f, 0.5f);
            dCloseRect.anchorMax = new Vector2(1f, 0.5f);
            dCloseRect.pivot = new Vector2(1f, 0.5f);
            dCloseRect.anchoredPosition = new Vector2(-10, 0);
            dCloseRect.sizeDelta = new Vector2(50, 50);
            Image dCloseImg = dCloseGO.AddComponent<Image>();
            if (iconClose != null) dCloseImg.sprite = iconClose;
            dCloseImg.color = new Color(1f, 1f, 1f, 0.65f);
            Button dCloseBtn = dCloseGO.AddComponent<Button>();

            // Detail Cards Grid Container
            GameObject dGridGO = new GameObject("CardsGrid");
            dGridGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform dgRect = dGridGO.AddComponent<RectTransform>();
            dgRect.anchorMin = new Vector2(0.5f, 1f);
            dgRect.anchorMax = new Vector2(0.5f, 1f);
            dgRect.pivot = new Vector2(0.5f, 1f);
            dgRect.anchoredPosition = new Vector2(0, -195);
            dgRect.sizeDelta = new Vector2(980, 1450);

            GridLayoutGroup dglg = dGridGO.AddComponent<GridLayoutGroup>();
            dglg.cellSize = new Vector2(475, 420);
            dglg.spacing = new Vector2(30, 24);
            dglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            dglg.constraintCount = 2;

            for (int i = 0; i < 6; i++)
            {
                CreateDetailCardItem(dGridGO.transform, $"DetailCard_{i}", barlowTMPFont, dmSansTMPFont);
            }

            // Floating Like Pill (Bottom Right)
            GameObject likePillGO = new GameObject("FloatingLikePill");
            likePillGO.transform.SetParent(detailModalGO.transform, false);
            RectTransform lpRect = likePillGO.AddComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(1f, 0f);
            lpRect.anchorMax = new Vector2(1f, 0f);
            lpRect.pivot = new Vector2(1f, 0f);
            lpRect.anchoredPosition = new Vector2(-50, 210);
            lpRect.sizeDelta = new Vector2(180, 74);

            RoundedRectGraphic lpG = likePillGO.AddComponent<RoundedRectGraphic>();
            lpG.IsCapsule = true;
            lpG.color = new Color(0.04f, 0.09f, 0.06f, 0.90f);
            lpG.BorderWidth = 2.0f;
            lpG.BorderColor = GoldBorder;

            Button lpBtn = likePillGO.AddComponent<Button>();

            GameObject lpHolderGO = new GameObject("Content");
            lpHolderGO.transform.SetParent(likePillGO.transform, false);
            RectTransform lphRect = lpHolderGO.AddComponent<RectTransform>();
            lphRect.anchorMin = Vector2.zero;
            lphRect.anchorMax = Vector2.one;
            lphRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup lphlg = lpHolderGO.AddComponent<HorizontalLayoutGroup>();
            lphlg.childAlignment = TextAnchor.MiddleCenter;
            lphlg.childControlWidth = false;
            lphlg.childControlHeight = false;
            lphlg.spacing = 10f;

            GameObject lpIconGO = new GameObject("LikeIcon");
            lpIconGO.transform.SetParent(lpHolderGO.transform, false);
            RectTransform lpiRect = lpIconGO.AddComponent<RectTransform>();
            lpiRect.sizeDelta = new Vector2(34, 34);
            Image lpIconImg = lpIconGO.AddComponent<Image>();
            if (iconLike != null) lpIconImg.sprite = iconLike;
            lpIconImg.color = Gold;
            lpIconImg.raycastTarget = false;

            GameObject lpTextGO = new GameObject("LikeCountText");
            lpTextGO.transform.SetParent(lpHolderGO.transform, false);
            RectTransform lptRect = lpTextGO.AddComponent<RectTransform>();
            lptRect.sizeDelta = new Vector2(70, 36);
            TextMeshProUGUI lpTMP = lpTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) lpTMP.font = barlowTMPFont;
            lpTMP.text = "234";
            lpTMP.fontSize = 32;
            lpTMP.fontStyle = FontStyles.Bold;
            lpTMP.alignment = TextAlignmentOptions.Left;
            lpTMP.color = Gold;
            lpTMP.raycastTarget = false;

            // Serialize VitrineDetailController
            SerializedObject dtSO = new SerializedObject(detailCtrl);
            dtSO.FindProperty("detailRoot").objectReferenceValue = detailModalGO;
            dtSO.FindProperty("avatarText").objectReferenceValue = daTMP;
            dtSO.FindProperty("userNameText").objectReferenceValue = duTMP;
            dtSO.FindProperty("cardCountText").objectReferenceValue = dcTMP;
            dtSO.FindProperty("closeButton").objectReferenceValue = dCloseBtn;
            dtSO.FindProperty("likeButton").objectReferenceValue = lpBtn;
            dtSO.FindProperty("likeCountText").objectReferenceValue = lpTMP;
            dtSO.FindProperty("likeIconImage").objectReferenceValue = lpIconImg;
            dtSO.FindProperty("likePillGraphic").objectReferenceValue = lpG;
            dtSO.FindProperty("cardsGridParent").objectReferenceValue = dGridGO.transform;
            dtSO.ApplyModifiedProperties();

            // Set initial state of detail modal: hidden
            detailModalGO.SetActive(false);

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

            // Assign Serialized Properties on VitrinesScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("searchInputField").objectReferenceValue = inputField;
            so.FindProperty("detailModal").objectReferenceValue = detailCtrl;

            SerializedProperty popProp = so.FindProperty("popularCardViews");
            popProp.arraySize = popularViews.Count;
            for (int i = 0; i < popularViews.Count; i++) popProp.GetArrayElementAtIndex(i).objectReferenceValue = popularViews[i];

            SerializedProperty friendProp = so.FindProperty("friendCardViews");
            friendProp.arraySize = friendViews.Count;
            for (int i = 0; i < friendViews.Count; i++) friendProp.GetArrayElementAtIndex(i).objectReferenceValue = friendViews[i];

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/VitrinesScreenUI.prefab";
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
            if (File.Exists("Assets/_Project/Scenes/ProfileScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/ProfileScene.unity", true));
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Vitrinas Públicas guardada como Escena Oficial (VitrinesScene.unity) y Prefab (VitrinesScreenUI.prefab)!</color>");
        }

        private static GameObject CreateVitrineCardGO(Transform parent, string name, float x, float y, float w, float h, TMP_FontAsset font, Sprite likeSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.anchoredPosition = new Vector2(x, y);
            cardRect.sizeDelta = new Vector2(w, h);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 20f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.5f;
            cardG.BorderColor = BorderSubtle;

            Button cardBtn = cardGO.AddComponent<Button>();
            VitrineCardView cardView = cardGO.AddComponent<VitrineCardView>();

            // User Info Header
            GameObject uHeadGO = new GameObject("UserHeader");
            uHeadGO.transform.SetParent(cardGO.transform, false);
            RectTransform uhRect = uHeadGO.AddComponent<RectTransform>();
            uhRect.anchorMin = new Vector2(0f, 1f);
            uhRect.anchorMax = new Vector2(1f, 1f);
            uhRect.pivot = new Vector2(0f, 1f);
            uhRect.anchoredPosition = new Vector2(20, -18);
            uhRect.sizeDelta = new Vector2(-40, 50);

            // Avatar circle
            GameObject avGO = new GameObject("AvatarCircle");
            avGO.transform.SetParent(uHeadGO.transform, false);
            RectTransform avRect = avGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(0, 0);
            avRect.sizeDelta = new Vector2(46, 46);
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
            if (font != null) avTMP.font = font;
            avTMP.text = "PP";
            avTMP.fontSize = 20;
            avTMP.fontStyle = FontStyles.Bold;
            avTMP.alignment = TextAlignmentOptions.Center;
            avTMP.color = TextWhite;
            avTMP.raycastTarget = false;

            // Name
            GameObject nameGO = new GameObject("UserNameText");
            nameGO.transform.SetParent(uHeadGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(58, 0);
            nameRect.sizeDelta = new Vector2(-58, 40);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (font != null) nameTMP.font = font;
            nameTMP.text = "ProPlayer_99";
            nameTMP.fontSize = 24;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.color = TextWhite;
            nameTMP.raycastTarget = false;

            // Mini Cards Center
            GameObject miniContGO = new GameObject("MiniCardsContainer");
            miniContGO.transform.SetParent(cardGO.transform, false);
            RectTransform mcRect = miniContGO.AddComponent<RectTransform>();
            mcRect.anchorMin = new Vector2(0.5f, 0.5f);
            mcRect.anchorMax = new Vector2(0.5f, 0.5f);
            mcRect.pivot = new Vector2(0.5f, 0.5f);
            mcRect.anchoredPosition = new Vector2(0, -6);
            mcRect.sizeDelta = new Vector2(260, 80);

            HorizontalLayoutGroup mchlg = miniContGO.AddComponent<HorizontalLayoutGroup>();
            mchlg.childAlignment = TextAnchor.MiddleCenter;
            mchlg.childControlWidth = false;
            mchlg.childControlHeight = false;
            mchlg.spacing = 14f;

            List<RoundedRectGraphic> miniImages = new List<RoundedRectGraphic>();
            for (int m = 0; m < 3; m++)
            {
                GameObject miniGO = new GameObject($"MiniCard_{m}");
                miniGO.transform.SetParent(miniContGO.transform, false);
                RectTransform mRect = miniGO.AddComponent<RectTransform>();
                mRect.sizeDelta = new Vector2(56, 75);

                RoundedRectGraphic mg = miniGO.AddComponent<RoundedRectGraphic>();
                mg.CornerRadius = 10f;
                mg.color = new Color(0.035f, 0.07f, 0.05f);
                mg.BorderWidth = 2.0f;
                mg.BorderColor = Gold;
                mg.raycastTarget = false;

                miniImages.Add(mg);
            }

            // Likes Bottom Right
            GameObject likesContGO = new GameObject("LikesContainer");
            likesContGO.transform.SetParent(cardGO.transform, false);
            RectTransform lcRect = likesContGO.AddComponent<RectTransform>();
            lcRect.anchorMin = new Vector2(1f, 0f);
            lcRect.anchorMax = new Vector2(1f, 0f);
            lcRect.pivot = new Vector2(1f, 0f);
            lcRect.anchoredPosition = new Vector2(-20, 16);
            lcRect.sizeDelta = new Vector2(100, 30);

            HorizontalLayoutGroup lchlg = likesContGO.AddComponent<HorizontalLayoutGroup>();
            lchlg.childAlignment = TextAnchor.MiddleRight;
            lchlg.childControlWidth = false;
            lchlg.childControlHeight = false;
            lchlg.spacing = 6f;

            GameObject lIconGO = new GameObject("LikeIcon");
            lIconGO.transform.SetParent(likesContGO.transform, false);
            RectTransform liRect = lIconGO.AddComponent<RectTransform>();
            liRect.sizeDelta = new Vector2(22, 22);
            Image liImg = lIconGO.AddComponent<Image>();
            if (likeSprite != null) liImg.sprite = likeSprite;
            liImg.color = TextDim;
            liImg.raycastTarget = false;

            GameObject lTextGO = new GameObject("LikesText");
            lTextGO.transform.SetParent(likesContGO.transform, false);
            RectTransform ltRect = lTextGO.AddComponent<RectTransform>();
            ltRect.sizeDelta = new Vector2(50, 26);
            TextMeshProUGUI ltTMP = lTextGO.AddComponent<TextMeshProUGUI>();
            if (font != null) ltTMP.font = font;
            ltTMP.text = "234";
            ltTMP.fontSize = 20;
            ltTMP.alignment = TextAlignmentOptions.Right;
            ltTMP.color = TextDim;
            ltTMP.raycastTarget = false;

            // Serialize VitrineCardView
            SerializedObject cSO = new SerializedObject(cardView);
            cSO.FindProperty("userNameText").objectReferenceValue = nameTMP;
            cSO.FindProperty("avatarText").objectReferenceValue = avTMP;
            cSO.FindProperty("likesText").objectReferenceValue = ltTMP;
            cSO.FindProperty("cardButton").objectReferenceValue = cardBtn;

            SerializedProperty miniProp = cSO.FindProperty("miniCardBorders");
            miniProp.arraySize = miniImages.Count;
            for (int m = 0; m < miniImages.Count; m++) miniProp.GetArrayElementAtIndex(m).objectReferenceValue = miniImages[m];
            cSO.ApplyModifiedProperties();

            return cardGO;
        }

        private static void CreateDetailCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 18f;
            cardG.color = CardBg;
            cardG.BorderWidth = 2.0f;
            cardG.BorderColor = GoldBorder;

            // Initials Big Center (e.g. "EH", "KM")
            GameObject iniGO = new GameObject("InitialsText");
            iniGO.transform.SetParent(cardGO.transform, false);
            RectTransform iniRect = iniGO.AddComponent<RectTransform>();
            iniRect.anchorMin = new Vector2(0f, 0.45f);
            iniRect.anchorMax = new Vector2(1f, 0.85f);
            iniRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI iniTMP = iniGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) iniTMP.font = barlowFont;
            iniTMP.text = "EH";
            iniTMP.fontSize = 68;
            iniTMP.fontStyle = FontStyles.Bold;
            iniTMP.alignment = TextAlignmentOptions.Center;
            iniTMP.color = new Color(1f, 1f, 1f, 0.18f);

            // Rarity Label
            GameObject rarGO = new GameObject("RarityText");
            rarGO.transform.SetParent(cardGO.transform, false);
            RectTransform rarRect = rarGO.AddComponent<RectTransform>();
            rarRect.anchorMin = new Vector2(0f, 0.28f);
            rarRect.anchorMax = new Vector2(1f, 0.42f);
            rarRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rarTMP = rarGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) rarTMP.font = barlowFont;
            rarTMP.text = "MÍTICA";
            rarTMP.fontSize = 22;
            rarTMP.fontStyle = FontStyles.Bold;
            rarTMP.characterSpacing = 6f;
            rarTMP.alignment = TextAlignmentOptions.Center;
            rarTMP.color = Gold;

            // Player Name
            GameObject pNameGO = new GameObject("PlayerNameText");
            pNameGO.transform.SetParent(cardGO.transform, false);
            RectTransform pnRect = pNameGO.AddComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0f, 0.08f);
            pnRect.anchorMax = new Vector2(1f, 0.24f);
            pnRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI pnTMP = pNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) pnTMP.font = dmSansFont;
            pnTMP.text = "Haaland";
            pnTMP.fontSize = 24;
            pnTMP.alignment = TextAlignmentOptions.Center;
            pnTMP.color = TextWhite;
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
