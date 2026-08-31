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
    public static class FriendsSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/FriendsScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Design Tokens
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);

        [MenuItem("JuegoTCG/Generar Pantalla de Amigos (Friends)")]
        public static void BuildFriendsScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();

            // Load Fonts
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite iconBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_back.png");
            Sprite iconLightning = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_lightning.png");
            Sprite iconCopy = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_copy.png");

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
            GameObject controllerGO = new GameObject("FriendsScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            FriendsScreenController controller = controllerGO.AddComponent<FriendsScreenController>();

            // ====================================================
            // 1. TOP FIXED HEADER (Back Arrow + "AMIGOS")
            // ====================================================
            GameObject topBarGO = new GameObject("TopHeader");
            topBarGO.transform.SetParent(canvasGO.transform, false);
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

            // Title: "AMIGOS"
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
            titleTMP.text = "AMIGOS";
            titleTMP.fontSize = 46;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 8f;
            titleTMP.color = TextWhite;

            // ====================================================
            // 2. SCROLLABLE CONTAINER (ScrollRect)
            // ====================================================
            GameObject scrollGO = new GameObject("ScrollArea");
            scrollGO.transform.SetParent(canvasGO.transform, false);
            RectTransform scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(0, -145);
            scrollRect.sizeDelta = new Vector2(1000, -145);

            ScrollRect sr = scrollGO.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = 0.1f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 35f;

            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpRect.pivot = new Vector2(0.5f, 1f);
            viewportGO.AddComponent<RectMask2D>();
            sr.viewport = vpRect;

            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(980, 2400);
            sr.content = contentRect;

            VerticalLayoutGroup cvlg = contentGO.AddComponent<VerticalLayoutGroup>();
            cvlg.childAlignment = TextAnchor.UpperCenter;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = false;
            cvlg.childForceExpandWidth = true;
            cvlg.childForceExpandHeight = false;
            cvlg.spacing = 24f;
            cvlg.padding = new RectOffset(0, 0, 10, 220); // Extra padding so it clears the bottom navbar

            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ====================================================
            // 3. TOP FRIEND CODE BOX ("FCX-2847" + COPIAR + AGREGAR)
            // ====================================================
            GameObject codeBoxGO = new GameObject("FriendCodeBox");
            codeBoxGO.transform.SetParent(contentGO.transform, false);
            RectTransform cbRect = codeBoxGO.AddComponent<RectTransform>();
            cbRect.sizeDelta = new Vector2(980, 205);

            LayoutElement cbLE = codeBoxGO.AddComponent<LayoutElement>();
            cbLE.minHeight = 205f;
            cbLE.preferredHeight = 205f;

            RoundedRectGraphic cbG = codeBoxGO.AddComponent<RoundedRectGraphic>();
            cbG.CornerRadius = 20f;
            cbG.color = CardBg;
            cbG.BorderWidth = 1.5f;
            cbG.BorderColor = BorderSubtle;

            // Label "Tu código de amigo"
            GameObject cblGO = new GameObject("Label");
            cblGO.transform.SetParent(codeBoxGO.transform, false);
            RectTransform cblRect = cblGO.AddComponent<RectTransform>();
            cblRect.anchorMin = new Vector2(0f, 1f);
            cblRect.anchorMax = new Vector2(0.6f, 1f);
            cblRect.pivot = new Vector2(0f, 1f);
            cblRect.anchoredPosition = new Vector2(24, -18);
            cblRect.sizeDelta = new Vector2(0, 26);
            TextMeshProUGUI cblTMP = cblGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) cblTMP.font = dmSansTMPFont;
            cblTMP.text = "Tu código de amigo";
            cblTMP.fontSize = 18;
            cblTMP.color = TextDim;

            // Big Code "FCX-2847"
            GameObject codeTextGO = new GameObject("CodeText");
            codeTextGO.transform.SetParent(codeBoxGO.transform, false);
            RectTransform ctRect = codeTextGO.AddComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0f, 1f);
            ctRect.anchorMax = new Vector2(0.6f, 1f);
            ctRect.pivot = new Vector2(0f, 1f);
            ctRect.anchoredPosition = new Vector2(24, -46);
            ctRect.sizeDelta = new Vector2(0, 48);
            TextMeshProUGUI ctTMP = codeTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) ctTMP.font = barlowTMPFont;
            ctTMP.text = "FCX-2847";
            ctTMP.fontSize = 42;
            ctTMP.fontStyle = FontStyles.Bold;
            ctTMP.characterSpacing = 6f;
            ctTMP.color = TextWhite;

            // Copy Button (Top Right)
            GameObject copyBtnGO = new GameObject("CopyButton");
            copyBtnGO.transform.SetParent(codeBoxGO.transform, false);
            RectTransform cpyRect = copyBtnGO.AddComponent<RectTransform>();
            cpyRect.anchorMin = new Vector2(1f, 1f);
            cpyRect.anchorMax = new Vector2(1f, 1f);
            cpyRect.pivot = new Vector2(1f, 1f);
            cpyRect.anchoredPosition = new Vector2(-24, -24);
            cpyRect.sizeDelta = new Vector2(165, 54);

            RoundedRectGraphic cpyG = copyBtnGO.AddComponent<RoundedRectGraphic>();
            cpyG.CornerRadius = 12f;
            cpyG.color = new Color(1f, 1f, 1f, 0.05f);
            cpyG.BorderWidth = 1.2f;
            cpyG.BorderColor = BorderSubtle;
            Button cpyBtn = copyBtnGO.AddComponent<Button>();

            GameObject cpyContentGO = new GameObject("Content");
            cpyContentGO.transform.SetParent(copyBtnGO.transform, false);
            RectTransform cpycRect = cpyContentGO.AddComponent<RectTransform>();
            cpycRect.anchorMin = Vector2.zero;
            cpycRect.anchorMax = Vector2.one;
            cpycRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup cpyhlg = cpyContentGO.AddComponent<HorizontalLayoutGroup>();
            cpyhlg.childAlignment = TextAnchor.MiddleCenter;
            cpyhlg.childControlWidth = false;
            cpyhlg.childControlHeight = false;
            cpyhlg.spacing = 8f;

            GameObject cpyIconGO = new GameObject("Icon");
            cpyIconGO.transform.SetParent(cpyContentGO.transform, false);
            RectTransform cpyiRect = cpyIconGO.AddComponent<RectTransform>();
            cpyiRect.sizeDelta = new Vector2(24, 24);
            Image cpyImg = cpyIconGO.AddComponent<Image>();
            if (iconCopy != null) cpyImg.sprite = iconCopy;
            cpyImg.color = TextGray;

            GameObject cpyTxtGO = new GameObject("Text");
            cpyTxtGO.transform.SetParent(cpyContentGO.transform, false);
            RectTransform cpytRect = cpyTxtGO.AddComponent<RectTransform>();
            cpytRect.sizeDelta = new Vector2(85, 30);
            TextMeshProUGUI cpyTMP = cpyTxtGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) cpyTMP.font = barlowTMPFont;
            cpyTMP.text = "COPIAR";
            cpyTMP.fontSize = 20;
            cpyTMP.fontStyle = FontStyles.Bold;
            cpyTMP.characterSpacing = 3f;
            cpyTMP.alignment = TextAlignmentOptions.Left;
            cpyTMP.color = TextGray;

            // Row 2: Add Friend Input + Button
            GameObject addInputBoxGO = new GameObject("AddInputBox");
            addInputBoxGO.transform.SetParent(codeBoxGO.transform, false);
            RectTransform aibRect = addInputBoxGO.AddComponent<RectTransform>();
            aibRect.anchorMin = new Vector2(0f, 0f);
            aibRect.anchorMax = new Vector2(1f, 0f);
            aibRect.pivot = new Vector2(0f, 0f);
            aibRect.anchoredPosition = new Vector2(24, 20);
            aibRect.sizeDelta = new Vector2(-235, 60);

            RoundedRectGraphic aibG = addInputBoxGO.AddComponent<RoundedRectGraphic>();
            aibG.CornerRadius = 12f;
            aibG.color = new Color(1f, 1f, 1f, 0.05f);
            aibG.BorderWidth = 1.0f;
            aibG.BorderColor = BorderSubtle;

            GameObject aibTextGO = new GameObject("InputPlaceholder");
            aibTextGO.transform.SetParent(addInputBoxGO.transform, false);
            RectTransform aibtRect = aibTextGO.AddComponent<RectTransform>();
            aibtRect.anchorMin = Vector2.zero;
            aibtRect.anchorMax = Vector2.one;
            aibtRect.anchoredPosition = new Vector2(18, 0);
            aibtRect.sizeDelta = new Vector2(-36, 0);
            TextMeshProUGUI aibTMP = aibTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) aibTMP.font = dmSansTMPFont;
            aibTMP.text = "🔍  Código de amigo...";
            aibTMP.fontSize = 20;
            aibTMP.alignment = TextAlignmentOptions.Left;
            aibTMP.color = TextDim;

            // Button AGREGAR
            GameObject addBtnGO = new GameObject("AddButton");
            addBtnGO.transform.SetParent(codeBoxGO.transform, false);
            RectTransform addBtnRect = addBtnGO.AddComponent<RectTransform>();
            addBtnRect.anchorMin = new Vector2(1f, 0f);
            addBtnRect.anchorMax = new Vector2(1f, 0f);
            addBtnRect.pivot = new Vector2(1f, 0f);
            addBtnRect.anchoredPosition = new Vector2(-24, 20);
            addBtnRect.sizeDelta = new Vector2(180, 60);

            RoundedRectGraphic addBtnG = addBtnGO.AddComponent<RoundedRectGraphic>();
            addBtnG.CornerRadius = 12f;
            addBtnG.color = Gold;
            Button addBtn = addBtnGO.AddComponent<Button>();

            GameObject addBtnTextGO = new GameObject("Text");
            addBtnTextGO.transform.SetParent(addBtnGO.transform, false);
            RectTransform abtRect = addBtnTextGO.AddComponent<RectTransform>();
            abtRect.anchorMin = Vector2.zero;
            abtRect.anchorMax = Vector2.one;
            abtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI abtTMP = addBtnTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) abtTMP.font = barlowTMPFont;
            abtTMP.text = "AGREGAR";
            abtTMP.fontSize = 22;
            abtTMP.fontStyle = FontStyles.Bold;
            abtTMP.characterSpacing = 3f;
            abtTMP.alignment = TextAlignmentOptions.Center;
            abtTMP.color = new Color(0.051f, 0.102f, 0.075f);

            // ====================================================
            // 4. SOLICITUDES SECTION (Header + 2 Request Cards)
            // ====================================================
            GameObject reqSecGO = new GameObject("RequestsSection");
            reqSecGO.transform.SetParent(contentGO.transform, false);
            RectTransform rsRect = reqSecGO.AddComponent<RectTransform>();
            rsRect.sizeDelta = new Vector2(980, 240);

            LayoutElement reqLE = reqSecGO.AddComponent<LayoutElement>();
            reqLE.minHeight = 240f;
            reqLE.preferredHeight = 240f;

            VerticalLayoutGroup rsvlg = reqSecGO.AddComponent<VerticalLayoutGroup>();
            rsvlg.childAlignment = TextAnchor.UpperCenter;
            rsvlg.childControlWidth = true;
            rsvlg.childControlHeight = false;
            rsvlg.childForceExpandWidth = true;
            rsvlg.childForceExpandHeight = false;
            rsvlg.spacing = 14f;

            // Header Row: "SOLICITUDES" + Badge (2)
            GameObject reqHeadGO = new GameObject("HeaderRow");
            reqHeadGO.transform.SetParent(reqSecGO.transform, false);
            RectTransform rhRect = reqHeadGO.AddComponent<RectTransform>();
            rhRect.sizeDelta = new Vector2(980, 36);

            HorizontalLayoutGroup rhlg = reqHeadGO.AddComponent<HorizontalLayoutGroup>();
            rhlg.childAlignment = TextAnchor.MiddleLeft;
            rhlg.childControlWidth = false;
            rhlg.childControlHeight = false;
            rhlg.spacing = 10f;

            GameObject rhTextGO = new GameObject("Title");
            rhTextGO.transform.SetParent(reqHeadGO.transform, false);
            RectTransform rhtRect = rhTextGO.AddComponent<RectTransform>();
            rhtRect.sizeDelta = new Vector2(230, 36);
            TextMeshProUGUI rhtTMP = rhTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) rhtTMP.font = barlowTMPFont;
            rhtTMP.text = "SOLICITUDES";
            rhtTMP.fontSize = 28;
            rhtTMP.fontStyle = FontStyles.Bold;
            rhtTMP.characterSpacing = 3f;
            rhtTMP.enableWordWrapping = false;
            rhtTMP.overflowMode = TextOverflowModes.Overflow;
            rhtTMP.color = TextWhite;

            GameObject reqBadgeGO = new GameObject("Badge");
            reqBadgeGO.transform.SetParent(reqHeadGO.transform, false);
            RectTransform rbRect = reqBadgeGO.AddComponent<RectTransform>();
            rbRect.sizeDelta = new Vector2(32, 32);
            RoundedRectGraphic rbG = reqBadgeGO.AddComponent<RoundedRectGraphic>();
            rbG.IsCapsule = true;
            rbG.color = Color.clear;
            rbG.BorderWidth = 1.5f;
            rbG.BorderColor = Gold;

            GameObject rbtTextGO = new GameObject("Text");
            rbtTextGO.transform.SetParent(reqBadgeGO.transform, false);
            RectTransform rbtRect = rbtTextGO.AddComponent<RectTransform>();
            rbtRect.anchorMin = Vector2.zero;
            rbtRect.anchorMax = Vector2.one;
            rbtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rbtTMP = rbtTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) rbtTMP.font = dmSansTMPFont;
            rbtTMP.text = "2";
            rbtTMP.fontSize = 18;
            rbtTMP.fontStyle = FontStyles.Bold;
            rbtTMP.alignment = TextAlignmentOptions.Center;
            rbtTMP.color = Gold;

            List<FriendRequestCardView> reqCardViews = new List<FriendRequestCardView>();
            for (int i = 0; i < 2; i++)
            {
                GameObject reqCardGO = CreateRequestCardItem(reqSecGO.transform, $"RequestCard_{i}", barlowTMPFont, dmSansTMPFont);
                reqCardViews.Add(reqCardGO.GetComponent<FriendRequestCardView>());
            }

            // ====================================================
            // 5. MIS AMIGOS SECTION (Header + 4 Friend Cards)
            // ====================================================
            GameObject friendsSecGO = new GameObject("FriendsSection");
            friendsSecGO.transform.SetParent(contentGO.transform, false);
            RectTransform fsRect = friendsSecGO.AddComponent<RectTransform>();
            fsRect.sizeDelta = new Vector2(980, 1050);

            LayoutElement fLE = friendsSecGO.AddComponent<LayoutElement>();
            fLE.minHeight = 1050f;
            fLE.preferredHeight = 1050f;

            VerticalLayoutGroup fsvlg = friendsSecGO.AddComponent<VerticalLayoutGroup>();
            fsvlg.childAlignment = TextAnchor.UpperCenter;
            fsvlg.childControlWidth = true;
            fsvlg.childControlHeight = false;
            fsvlg.childForceExpandWidth = true;
            fsvlg.childForceExpandHeight = false;
            fsvlg.spacing = 16f;

            // Header "MIS AMIGOS"
            GameObject friendsHeadGO = new GameObject("Header");
            friendsHeadGO.transform.SetParent(friendsSecGO.transform, false);
            RectTransform fhRect = friendsHeadGO.AddComponent<RectTransform>();
            fhRect.sizeDelta = new Vector2(980, 36);
            TextMeshProUGUI fhTMP = friendsHeadGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) fhTMP.font = barlowTMPFont;
            fhTMP.text = "MIS AMIGOS";
            fhTMP.fontSize = 28;
            fhTMP.fontStyle = FontStyles.Bold;
            fhTMP.characterSpacing = 3f;
            fhTMP.enableWordWrapping = false;
            fhTMP.color = TextWhite;

            List<FriendCardView> friendViews = new List<FriendCardView>();
            for (int i = 0; i < 4; i++)
            {
                GameObject fCardGO = CreateFriendCardItem(friendsSecGO.transform, $"FriendCard_{i}", barlowTMPFont, dmSansTMPFont, iconLightning);
                friendViews.Add(fCardGO.GetComponent<FriendCardView>());
            }

            // ====================================================
            // 6. RANKING DE AMIGOS SECTION (Header + 5 Ranked Rows)
            // ====================================================
            GameObject rankSecGO = new GameObject("RankingSection");
            rankSecGO.transform.SetParent(contentGO.transform, false);
            RectTransform rankSecRect = rankSecGO.AddComponent<RectTransform>();
            rankSecRect.sizeDelta = new Vector2(980, 520);

            LayoutElement rkLE = rankSecGO.AddComponent<LayoutElement>();
            rkLE.minHeight = 520f;
            rkLE.preferredHeight = 520f;

            VerticalLayoutGroup rkvlg = rankSecGO.AddComponent<VerticalLayoutGroup>();
            rkvlg.childAlignment = TextAnchor.UpperCenter;
            rkvlg.childControlWidth = true;
            rkvlg.childControlHeight = false;
            rkvlg.childForceExpandWidth = true;
            rkvlg.childForceExpandHeight = false;
            rkvlg.spacing = 12f;

            // Header "RANKING DE AMIGOS"
            GameObject rankHeadGO = new GameObject("Header");
            rankHeadGO.transform.SetParent(rankSecGO.transform, false);
            RectTransform rkhRect = rankHeadGO.AddComponent<RectTransform>();
            rkhRect.sizeDelta = new Vector2(980, 36);
            TextMeshProUGUI rkhTMP = rankHeadGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) rkhTMP.font = barlowTMPFont;
            rkhTMP.text = "RANKING DE AMIGOS";
            rkhTMP.fontSize = 28;
            rkhTMP.fontStyle = FontStyles.Bold;
            rkhTMP.characterSpacing = 3f;
            rkhTMP.enableWordWrapping = false;
            rkhTMP.color = TextWhite;

            List<FriendRankingRowView> rankingViews = new List<FriendRankingRowView>();
            for (int i = 0; i < 5; i++)
            {
                GameObject rowGO = CreateRankingRowItem(rankSecGO.transform, $"RankingRow_{i}", barlowTMPFont, dmSansTMPFont, iconLightning);
                rankingViews.Add(rowGO.GetComponent<FriendRankingRowView>());
            }

            // ====================================================
            // 7. BOTTOM NAVIGATION BAR (5 Tabs, "Comunidad" Active)
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

            // Assign Serialized Properties on FriendsScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("myFriendCodeText").objectReferenceValue = ctTMP;
            so.FindProperty("copyCodeButton").objectReferenceValue = cpyBtn;
            so.FindProperty("copyButtonText").objectReferenceValue = cpyTMP;
            so.FindProperty("addFriendButton").objectReferenceValue = addBtn;

            so.FindProperty("requestsSectionGO").objectReferenceValue = reqSecGO;
            so.FindProperty("requestsBadgeText").objectReferenceValue = rbtTMP;

            SerializedProperty reqProp = so.FindProperty("requestCardViews");
            reqProp.arraySize = reqCardViews.Count;
            for (int i = 0; i < reqCardViews.Count; i++) reqProp.GetArrayElementAtIndex(i).objectReferenceValue = reqCardViews[i];

            SerializedProperty fProp = so.FindProperty("friendCardViews");
            fProp.arraySize = friendViews.Count;
            for (int i = 0; i < friendViews.Count; i++) fProp.GetArrayElementAtIndex(i).objectReferenceValue = friendViews[i];

            SerializedProperty rkProp = so.FindProperty("rankingRowViews");
            rkProp.arraySize = rankingViews.Count;
            for (int i = 0; i < rankingViews.Count; i++) rkProp.GetArrayElementAtIndex(i).objectReferenceValue = rankingViews[i];

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/FriendsScreenUI.prefab";
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
            if (File.Exists("Assets/_Project/Scenes/PackOpeningScene.unity")) buildScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/PackOpeningScene.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Amigos guardada con Scroll y Ranking (FriendsScene.unity & FriendsScreenUI.prefab)!</color>");
        }

        private static GameObject CreateRequestCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(980, 84);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 16f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.2f;
            cardG.BorderColor = BorderSubtle;

            LayoutElement le = cardGO.AddComponent<LayoutElement>();
            le.minHeight = 84f;
            le.preferredHeight = 84f;
            le.flexibleHeight = 0f;

            FriendRequestCardView reqView = cardGO.AddComponent<FriendRequestCardView>();

            // Avatar Circle Left
            GameObject avGO = new GameObject("AvatarCircle");
            avGO.transform.SetParent(cardGO.transform, false);
            RectTransform avRect = avGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(18, 0);
            avRect.sizeDelta = new Vector2(48, 48);

            RoundedRectGraphic avG = avGO.AddComponent<RoundedRectGraphic>();
            avG.IsCapsule = true;
            avG.color = new Color(1f, 1f, 1f, 0.08f);
            avG.BorderWidth = 1.0f;
            avG.BorderColor = BorderSubtle;

            GameObject avtGO = new GameObject("Text");
            avtGO.transform.SetParent(avGO.transform, false);
            RectTransform avtRect = avtGO.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI avTMP = avtGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) avTMP.font = dmSansFont;
            avTMP.text = "NJ";
            avTMP.fontSize = 18;
            avTMP.fontStyle = FontStyles.Bold;
            avTMP.alignment = TextAlignmentOptions.Center;
            avTMP.color = TextWhite;

            // User Name
            GameObject nameGO = new GameObject("UserName");
            nameGO.transform.SetParent(cardGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(80, 0);
            nameRect.sizeDelta = new Vector2(0, 32);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) nameTMP.font = dmSansFont;
            nameTMP.text = "NuevoJugador_01";
            nameTMP.fontSize = 22;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = TextWhite;

            // Buttons Container Right
            GameObject btnsGO = new GameObject("ButtonsHolder");
            btnsGO.transform.SetParent(cardGO.transform, false);
            RectTransform bRect = btnsGO.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(1f, 0.5f);
            bRect.anchorMax = new Vector2(1f, 0.5f);
            bRect.pivot = new Vector2(1f, 0.5f);
            bRect.anchoredPosition = new Vector2(-16, 0);
            bRect.sizeDelta = new Vector2(370, 56);

            HorizontalLayoutGroup bhlg = btnsGO.AddComponent<HorizontalLayoutGroup>();
            bhlg.childAlignment = TextAnchor.MiddleRight;
            bhlg.childControlWidth = false;
            bhlg.childControlHeight = false;
            bhlg.spacing = 12f;

            // ACEPTAR Button
            GameObject accBtnGO = new GameObject("AcceptButton");
            accBtnGO.transform.SetParent(btnsGO.transform, false);
            RectTransform accRect = accBtnGO.AddComponent<RectTransform>();
            accRect.sizeDelta = new Vector2(170, 54);

            RoundedRectGraphic accG = accBtnGO.AddComponent<RoundedRectGraphic>();
            accG.CornerRadius = 10f;
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
            acctTMP.fontSize = 22;
            acctTMP.fontStyle = FontStyles.Bold;
            acctTMP.characterSpacing = 3f;
            acctTMP.alignment = TextAlignmentOptions.Center;
            acctTMP.color = new Color(0.051f, 0.102f, 0.075f);

            // RECHAZAR Button
            GameObject rejBtnGO = new GameObject("RejectButton");
            rejBtnGO.transform.SetParent(btnsGO.transform, false);
            RectTransform rejRect = rejBtnGO.AddComponent<RectTransform>();
            rejRect.sizeDelta = new Vector2(170, 54);

            RoundedRectGraphic rejG = rejBtnGO.AddComponent<RoundedRectGraphic>();
            rejG.CornerRadius = 10f;
            rejG.color = new Color(1f, 1f, 1f, 0.05f);
            rejG.BorderWidth = 1.0f;
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
            rejtTMP.fontSize = 22;
            rejtTMP.fontStyle = FontStyles.Bold;
            rejtTMP.characterSpacing = 3f;
            rejtTMP.alignment = TextAlignmentOptions.Center;
            rejtTMP.color = TextGray;

            // Serialize FriendRequestCardView
            SerializedObject reqSO = new SerializedObject(reqView);
            reqSO.FindProperty("avatarText").objectReferenceValue = avTMP;
            reqSO.FindProperty("userNameText").objectReferenceValue = nameTMP;
            reqSO.FindProperty("acceptButton").objectReferenceValue = accBtn;
            reqSO.FindProperty("rejectButton").objectReferenceValue = rejBtn;
            reqSO.ApplyModifiedProperties();

            return cardGO;
        }

        private static GameObject CreateFriendCardItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont, Sprite lightningSprite)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(980, 230);

            RoundedRectGraphic cardG = cardGO.AddComponent<RoundedRectGraphic>();
            cardG.CornerRadius = 20f;
            cardG.color = CardBg;
            cardG.BorderWidth = 1.2f;
            cardG.BorderColor = BorderSubtle;

            LayoutElement le = cardGO.AddComponent<LayoutElement>();
            le.minHeight = 230f;
            le.preferredHeight = 230f;
            le.flexibleHeight = 0f;

            FriendCardView fView = cardGO.AddComponent<FriendCardView>();

            // Top Row: Avatar + Name + Level + Power
            GameObject topRowGO = new GameObject("TopRow");
            topRowGO.transform.SetParent(cardGO.transform, false);
            RectTransform trRect = topRowGO.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.pivot = new Vector2(0.5f, 1f);
            trRect.anchoredPosition = new Vector2(0, -16);
            trRect.sizeDelta = new Vector2(-40, 56);

            // Avatar Circle
            GameObject avGO = new GameObject("Avatar");
            avGO.transform.SetParent(topRowGO.transform, false);
            RectTransform avRect = avGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(0, 0);
            avRect.sizeDelta = new Vector2(54, 54);

            RoundedRectGraphic avG = avGO.AddComponent<RoundedRectGraphic>();
            avG.IsCapsule = true;
            avG.color = new Color(1f, 1f, 1f, 0.08f);
            avG.BorderWidth = 1.0f;
            avG.BorderColor = BorderSubtle;

            GameObject avTextGO = new GameObject("Text");
            avTextGO.transform.SetParent(avGO.transform, false);
            RectTransform avtRect = avTextGO.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI avTMP = avTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) avTMP.font = dmSansFont;
            avTMP.text = "GS";
            avTMP.fontSize = 20;
            avTMP.fontStyle = FontStyles.Bold;
            avTMP.alignment = TextAlignmentOptions.Center;
            avTMP.color = TextWhite;

            // Name + Level Container
            GameObject nlGO = new GameObject("NameLevelCol");
            nlGO.transform.SetParent(topRowGO.transform, false);
            RectTransform nlRect = nlGO.AddComponent<RectTransform>();
            nlRect.anchorMin = new Vector2(0f, 0.5f);
            nlRect.anchorMax = new Vector2(0.65f, 0.5f);
            nlRect.pivot = new Vector2(0f, 0.5f);
            nlRect.anchoredPosition = new Vector2(68, 0);
            nlRect.sizeDelta = new Vector2(0, 52);

            GameObject fnTextGO = new GameObject("UserName");
            fnTextGO.transform.SetParent(nlGO.transform, false);
            RectTransform fntRect = fnTextGO.AddComponent<RectTransform>();
            fntRect.anchorMin = new Vector2(0f, 1f);
            fntRect.anchorMax = new Vector2(1f, 1f);
            fntRect.pivot = new Vector2(0f, 1f);
            fntRect.anchoredPosition = new Vector2(0, 0);
            fntRect.sizeDelta = new Vector2(0, 28);
            TextMeshProUGUI fnTMP = fnTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) fnTMP.font = dmSansFont;
            fnTMP.text = "GoldenShot_7";
            fnTMP.fontSize = 22;
            fnTMP.fontStyle = FontStyles.Bold;
            fnTMP.color = TextWhite;

            GameObject flTextGO = new GameObject("Level");
            flTextGO.transform.SetParent(nlGO.transform, false);
            RectTransform fltRect = flTextGO.AddComponent<RectTransform>();
            fltRect.anchorMin = new Vector2(0f, 0f);
            fltRect.anchorMax = new Vector2(1f, 0f);
            fltRect.pivot = new Vector2(0f, 0f);
            fltRect.anchoredPosition = new Vector2(0, 0);
            fltRect.sizeDelta = new Vector2(0, 22);
            TextMeshProUGUI flTMP = flTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) flTMP.font = dmSansFont;
            flTMP.text = "Nivel 24";
            flTMP.fontSize = 17;
            flTMP.color = TextDim;

            // Power Top Right (⚡ 9120)
            GameObject powerGO = new GameObject("PowerHolder");
            powerGO.transform.SetParent(topRowGO.transform, false);
            RectTransform pwrRect = powerGO.AddComponent<RectTransform>();
            pwrRect.anchorMin = new Vector2(1f, 0.5f);
            pwrRect.anchorMax = new Vector2(1f, 0.5f);
            pwrRect.pivot = new Vector2(1f, 0.5f);
            pwrRect.anchoredPosition = new Vector2(0, 0);
            pwrRect.sizeDelta = new Vector2(150, 36);

            HorizontalLayoutGroup pwrhlg = powerGO.AddComponent<HorizontalLayoutGroup>();
            pwrhlg.childAlignment = TextAnchor.MiddleRight;
            pwrhlg.childControlWidth = false;
            pwrhlg.childControlHeight = false;
            pwrhlg.spacing = 6f;

            GameObject pwrIconGO = new GameObject("LightningIcon");
            pwrIconGO.transform.SetParent(powerGO.transform, false);
            RectTransform pwriRect = pwrIconGO.AddComponent<RectTransform>();
            pwriRect.sizeDelta = new Vector2(22, 22);
            Image pwriImg = pwrIconGO.AddComponent<Image>();
            if (lightningSprite != null) pwriImg.sprite = lightningSprite;
            pwriImg.color = Gold;

            GameObject pwrTextGO = new GameObject("PowerText");
            pwrTextGO.transform.SetParent(powerGO.transform, false);
            RectTransform pwrtRect = pwrTextGO.AddComponent<RectTransform>();
            pwrtRect.sizeDelta = new Vector2(85, 30);
            TextMeshProUGUI pwrtTMP = pwrTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) pwrtTMP.font = barlowFont;
            pwrtTMP.text = "9120";
            pwrtTMP.fontSize = 28;
            pwrtTMP.fontStyle = FontStyles.Bold;
            pwrtTMP.alignment = TextAlignmentOptions.Right;
            pwrtTMP.color = Gold;

            // Middle Row: Stats + Album Progress Bar
            GameObject midRowGO = new GameObject("StatsRow");
            midRowGO.transform.SetParent(cardGO.transform, false);
            RectTransform mrRect = midRowGO.AddComponent<RectTransform>();
            mrRect.anchorMin = new Vector2(0.5f, 1f);
            mrRect.anchorMax = new Vector2(0.5f, 1f);
            mrRect.pivot = new Vector2(0.5f, 1f);
            mrRect.anchoredPosition = new Vector2(0, -84);
            mrRect.sizeDelta = new Vector2(920, 30);

            HorizontalLayoutGroup mrhlg = midRowGO.AddComponent<HorizontalLayoutGroup>();
            mrhlg.childAlignment = TextAnchor.MiddleLeft;
            mrhlg.childControlWidth = false;
            mrhlg.childControlHeight = false;
            mrhlg.spacing = 10f;

            GameObject cardsCntGO = new GameObject("CardsCount");
            cardsCntGO.transform.SetParent(midRowGO.transform, false);
            RectTransform ccRect = cardsCntGO.AddComponent<RectTransform>();
            ccRect.sizeDelta = new Vector2(130, 26);
            TextMeshProUGUI ccTMP = cardsCntGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) ccTMP.font = dmSansFont;
            ccTMP.text = "445 cartas";
            ccTMP.fontSize = 18;
            ccTMP.color = TextDim;

            GameObject dotGO = new GameObject("Dot");
            dotGO.transform.SetParent(midRowGO.transform, false);
            RectTransform dRect = dotGO.AddComponent<RectTransform>();
            dRect.sizeDelta = new Vector2(10, 26);
            TextMeshProUGUI dTMP = dotGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) dTMP.font = dmSansFont;
            dTMP.text = "·";
            dTMP.fontSize = 18;
            dTMP.color = TextDim;

            GameObject albumLblGO = new GameObject("AlbumLabel");
            albumLblGO.transform.SetParent(midRowGO.transform, false);
            RectTransform alRect = albumLblGO.AddComponent<RectTransform>();
            alRect.sizeDelta = new Vector2(70, 26);
            TextMeshProUGUI alTMP = albumLblGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) alTMP.font = dmSansFont;
            alTMP.text = "Álbum";
            alTMP.fontSize = 18;
            alTMP.color = TextDim;

            // Progress Bar Track
            GameObject progTrackGO = new GameObject("ProgressBarTrack");
            progTrackGO.transform.SetParent(midRowGO.transform, false);
            RectTransform ptRect = progTrackGO.AddComponent<RectTransform>();
            ptRect.sizeDelta = new Vector2(580, 10);

            RoundedRectGraphic ptG = progTrackGO.AddComponent<RoundedRectGraphic>();
            ptG.IsCapsule = true;
            ptG.color = new Color(1f, 1f, 1f, 0.10f);

            GameObject progFillGO = new GameObject("ProgressFill");
            progFillGO.transform.SetParent(progTrackGO.transform, false);
            RectTransform pfRect = progFillGO.AddComponent<RectTransform>();
            pfRect.anchorMin = Vector2.zero;
            pfRect.anchorMax = new Vector2(0.89f, 1f);
            pfRect.sizeDelta = Vector2.zero;

            RoundedRectGraphic pfG = progFillGO.AddComponent<RoundedRectGraphic>();
            pfG.IsCapsule = true;
            pfG.color = Gold;

            GameObject albPctGO = new GameObject("AlbumPct");
            albPctGO.transform.SetParent(midRowGO.transform, false);
            RectTransform apRect = albPctGO.AddComponent<RectTransform>();
            apRect.sizeDelta = new Vector2(65, 26);
            TextMeshProUGUI apTMP = albPctGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) apTMP.font = dmSansFont;
            apTMP.text = "89%";
            apTMP.fontSize = 18;
            apTMP.alignment = TextAlignmentOptions.Right;
            apTMP.color = TextGray;

            // Bottom Row: Action Buttons (COMPARAR / INTERCAMBIAR)
            GameObject actionsGO = new GameObject("ActionsRow");
            actionsGO.transform.SetParent(cardGO.transform, false);
            RectTransform actRect = actionsGO.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.5f, 0f);
            actRect.anchorMax = new Vector2(0.5f, 0f);
            actRect.pivot = new Vector2(0.5f, 0f);
            actRect.anchoredPosition = new Vector2(0, 16);
            actRect.sizeDelta = new Vector2(920, 54);

            HorizontalLayoutGroup acthlg = actionsGO.AddComponent<HorizontalLayoutGroup>();
            acthlg.childAlignment = TextAnchor.MiddleCenter;
            acthlg.childControlWidth = true;
            acthlg.childControlHeight = true;
            acthlg.childForceExpandWidth = true;
            acthlg.childForceExpandHeight = true;
            acthlg.spacing = 16f;

            // COMPARAR Button
            GameObject cmpBtnGO = new GameObject("CompareButton");
            cmpBtnGO.transform.SetParent(actionsGO.transform, false);
            RoundedRectGraphic cmpG = cmpBtnGO.AddComponent<RoundedRectGraphic>();
            cmpG.CornerRadius = 12f;
            cmpG.color = new Color(1f, 1f, 1f, 0.05f);
            cmpG.BorderWidth = 1.0f;
            cmpG.BorderColor = BorderSubtle;
            Button cmpBtn = cmpBtnGO.AddComponent<Button>();

            GameObject cmpTextGO = new GameObject("Text");
            cmpTextGO.transform.SetParent(cmpBtnGO.transform, false);
            RectTransform cmptRect = cmpTextGO.AddComponent<RectTransform>();
            cmptRect.anchorMin = Vector2.zero;
            cmptRect.anchorMax = Vector2.one;
            cmptRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI cmptTMP = cmpTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) cmptTMP.font = barlowFont;
            cmptTMP.text = "COMPARAR";
            cmptTMP.fontSize = 20;
            cmptTMP.fontStyle = FontStyles.Bold;
            cmptTMP.characterSpacing = 3f;
            cmptTMP.alignment = TextAlignmentOptions.Center;
            cmptTMP.color = TextGray;

            // INTERCAMBIAR Button
            GameObject trdBtnGO = new GameObject("TradeButton");
            trdBtnGO.transform.SetParent(actionsGO.transform, false);
            RoundedRectGraphic trdG = trdBtnGO.AddComponent<RoundedRectGraphic>();
            trdG.CornerRadius = 12f;
            trdG.color = new Color(1f, 1f, 1f, 0.05f);
            trdG.BorderWidth = 1.0f;
            trdG.BorderColor = BorderSubtle;
            Button trdBtn = trdBtnGO.AddComponent<Button>();

            GameObject trdTextGO = new GameObject("Text");
            trdTextGO.transform.SetParent(trdBtnGO.transform, false);
            RectTransform trdtRect = trdTextGO.AddComponent<RectTransform>();
            trdtRect.anchorMin = Vector2.zero;
            trdtRect.anchorMax = Vector2.one;
            trdtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI trdtTMP = trdTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) trdtTMP.font = barlowFont;
            trdtTMP.text = "INTERCAMBIAR";
            trdtTMP.fontSize = 20;
            trdtTMP.fontStyle = FontStyles.Bold;
            trdtTMP.characterSpacing = 3f;
            trdtTMP.alignment = TextAlignmentOptions.Center;
            trdtTMP.color = TextGray;

            // Serialize FriendCardView
            SerializedObject fSO = new SerializedObject(fView);
            fSO.FindProperty("avatarText").objectReferenceValue = avTMP;
            fSO.FindProperty("userNameText").objectReferenceValue = fnTMP;
            fSO.FindProperty("levelText").objectReferenceValue = flTMP;
            fSO.FindProperty("powerText").objectReferenceValue = pwrtTMP;
            fSO.FindProperty("cardsCountText").objectReferenceValue = ccTMP;
            fSO.FindProperty("albumPctText").objectReferenceValue = apTMP;
            fSO.FindProperty("albumProgressBarFill").objectReferenceValue = pfRect;
            fSO.FindProperty("compareButton").objectReferenceValue = cmpBtn;
            fSO.FindProperty("tradeButton").objectReferenceValue = trdBtn;
            fSO.ApplyModifiedProperties();

            return cardGO;
        }

        private static GameObject CreateRankingRowItem(Transform parent, string name, TMP_FontAsset barlowFont, TMP_FontAsset dmSansFont, Sprite lightningSprite)
        {
            GameObject rowGO = new GameObject(name);
            rowGO.transform.SetParent(parent, false);
            RectTransform rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(980, 74);

            RoundedRectGraphic rowG = rowGO.AddComponent<RoundedRectGraphic>();
            rowG.CornerRadius = 14f;
            rowG.color = CardBg;
            rowG.BorderWidth = 1.0f;
            rowG.BorderColor = BorderSubtle;

            LayoutElement le = rowGO.AddComponent<LayoutElement>();
            le.minHeight = 74f;
            le.preferredHeight = 74f;
            le.flexibleHeight = 0f;

            FriendRankingRowView rView = rowGO.AddComponent<FriendRankingRowView>();

            // Position Number Left (1, 2, 3...)
            GameObject posGO = new GameObject("Position");
            posGO.transform.SetParent(rowGO.transform, false);
            RectTransform posRect = posGO.AddComponent<RectTransform>();
            posRect.anchorMin = new Vector2(0f, 0.5f);
            posRect.anchorMax = new Vector2(0f, 0.5f);
            posRect.pivot = new Vector2(0f, 0.5f);
            posRect.anchoredPosition = new Vector2(18, 0);
            posRect.sizeDelta = new Vector2(36, 32);
            TextMeshProUGUI posTMP = posGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) posTMP.font = barlowFont;
            posTMP.text = "1";
            posTMP.fontSize = 24;
            posTMP.fontStyle = FontStyles.Bold;
            posTMP.alignment = TextAlignmentOptions.Center;
            posTMP.color = Gold;

            // Avatar Circle
            GameObject avGO = new GameObject("Avatar");
            avGO.transform.SetParent(rowGO.transform, false);
            RectTransform avRect = avGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(62, 0);
            avRect.sizeDelta = new Vector2(44, 44);

            RoundedRectGraphic avG = avGO.AddComponent<RoundedRectGraphic>();
            avG.IsCapsule = true;
            avG.color = new Color(1f, 1f, 1f, 0.08f);
            avG.BorderWidth = 1.0f;
            avG.BorderColor = BorderSubtle;

            GameObject avTextGO = new GameObject("Text");
            avTextGO.transform.SetParent(avGO.transform, false);
            RectTransform avtRect = avTextGO.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI avTMP = avTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) avTMP.font = dmSansFont;
            avTMP.text = "GS";
            avTMP.fontSize = 17;
            avTMP.fontStyle = FontStyles.Bold;
            avTMP.alignment = TextAlignmentOptions.Center;
            avTMP.color = TextWhite;

            // User Name
            GameObject nameGO = new GameObject("UserName");
            nameGO.transform.SetParent(rowGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0.6f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(120, 0);
            nameRect.sizeDelta = new Vector2(0, 30);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansFont != null) nameTMP.font = dmSansFont;
            nameTMP.text = "GoldenShot_7";
            nameTMP.fontSize = 22;
            nameTMP.fontStyle = FontStyles.Normal;
            nameTMP.color = TextWhite;

            // Power Top Right (⚡ 9120)
            GameObject powerGO = new GameObject("PowerHolder");
            powerGO.transform.SetParent(rowGO.transform, false);
            RectTransform pwrRect = powerGO.AddComponent<RectTransform>();
            pwrRect.anchorMin = new Vector2(1f, 0.5f);
            pwrRect.anchorMax = new Vector2(1f, 0.5f);
            pwrRect.pivot = new Vector2(1f, 0.5f);
            pwrRect.anchoredPosition = new Vector2(-20, 0);
            pwrRect.sizeDelta = new Vector2(140, 32);

            HorizontalLayoutGroup pwrhlg = powerGO.AddComponent<HorizontalLayoutGroup>();
            pwrhlg.childAlignment = TextAnchor.MiddleRight;
            pwrhlg.childControlWidth = false;
            pwrhlg.childControlHeight = false;
            pwrhlg.spacing = 6f;

            GameObject pwrIconGO = new GameObject("LightningIcon");
            pwrIconGO.transform.SetParent(powerGO.transform, false);
            RectTransform pwriRect = pwrIconGO.AddComponent<RectTransform>();
            pwriRect.sizeDelta = new Vector2(20, 20);
            Image pwriImg = pwrIconGO.AddComponent<Image>();
            if (lightningSprite != null) pwriImg.sprite = lightningSprite;
            pwriImg.color = new Color(1f, 1f, 1f, 0.35f);

            GameObject pwrTextGO = new GameObject("PowerText");
            pwrTextGO.transform.SetParent(powerGO.transform, false);
            RectTransform pwrtRect = pwrTextGO.AddComponent<RectTransform>();
            pwrtRect.sizeDelta = new Vector2(80, 28);
            TextMeshProUGUI pwrtTMP = pwrTextGO.AddComponent<TextMeshProUGUI>();
            if (barlowFont != null) pwrtTMP.font = barlowFont;
            pwrtTMP.text = "9120";
            pwrtTMP.fontSize = 24;
            pwrtTMP.fontStyle = FontStyles.Bold;
            pwrtTMP.alignment = TextAlignmentOptions.Right;
            pwrtTMP.color = TextGray;

            // Serialize FriendRankingRowView
            SerializedObject rSO = new SerializedObject(rView);
            rSO.FindProperty("positionText").objectReferenceValue = posTMP;
            rSO.FindProperty("avatarText").objectReferenceValue = avTMP;
            rSO.FindProperty("avatarBorderGraphic").objectReferenceValue = avG;
            rSO.FindProperty("userNameText").objectReferenceValue = nameTMP;
            rSO.FindProperty("lightningIcon").objectReferenceValue = pwriImg;
            rSO.FindProperty("powerText").objectReferenceValue = pwrtTMP;
            rSO.FindProperty("rowBorderGraphic").objectReferenceValue = rowG;
            rSO.ApplyModifiedProperties();

            return rowGO;
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
