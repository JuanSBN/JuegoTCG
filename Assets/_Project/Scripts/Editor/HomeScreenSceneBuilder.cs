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
    public static class HomeScreenSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/HomeScreenScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Exact Design Tokens from docs/Pantallas/src/App.tsx
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.50f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);   // rgba(14,32,22,0.85)

        [MenuItem("JuegoTCG/Generar Pantalla de Inicio (Home)")]
        public static void BuildHomeScreenScene()
        {
            ProceduralAssetGenerator.GenerateUISprites();
            ConfigureFontImporters();

            // Load and create persistent SDF Font Assets
            TMP_FontAsset barlowTMPFont = GetOrCreateTMPFont("BarlowCondensed-Bold");
            TMP_FontAsset dmSansTMPFont = GetOrCreateTMPFont("DMSans-SemiBold") ?? GetOrCreateTMPFont("DMSans-Bold");

            // Load UI Sprites
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_circle.png");
            Sprite tacticalPitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_tactical_pitch.png");
            Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_coin.png");

            Sprite iconUser = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_user.png");
            Sprite iconMail = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_mail.png");
            Sprite iconGift = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_gift.png");
            Sprite iconClock = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_clock.png");
            Sprite iconShop = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_shop.png");
            Sprite iconCheckMisiones = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_check_misiones.png");
            Sprite iconHome = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_home.png");
            Sprite iconCards = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_cards.png");
            Sprite iconUsers = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_users.png");
            Sprite checkGold = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_check_racha.png");

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
            GameObject controllerGO = new GameObject("HomeScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            HomeScreenController controller = controllerGO.AddComponent<HomeScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // ====================================================
            // 1. TOP BAR
            // ====================================================
            GameObject topBarGO = new GameObject("TopBar");
            topBarGO.transform.SetParent(contentGO.transform, false);
            RectTransform topBarRect = topBarGO.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0.5f, 1f);
            topBarRect.anchorMax = new Vector2(0.5f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0, -50);
            topBarRect.sizeDelta = new Vector2(1000, 130);

            // Left: 2 square slot badges
            for (int i = 0; i < 2; i++)
            {
                GameObject slotGO = new GameObject($"SlotBadge_{i}");
                slotGO.transform.SetParent(topBarGO.transform, false);
                RectTransform slotRect = slotGO.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0.5f);
                slotRect.anchorMax = new Vector2(0f, 0.5f);
                slotRect.pivot = new Vector2(0f, 0.5f);
                slotRect.anchoredPosition = new Vector2(i * 85, 0);
                slotRect.sizeDelta = new Vector2(74, 74);

                RoundedRectGraphic slotG = slotGO.AddComponent<RoundedRectGraphic>();
                slotG.CornerRadius = 14f;
                slotG.color = new Color(1f, 1f, 1f, 0.05f);
                slotG.BorderWidth = 1.5f;
                slotG.BorderColor = BorderSubtle;
            }

            // Center: Avatar + Name + Level
            GameObject centerProfileGO = new GameObject("CenterProfile");
            centerProfileGO.transform.SetParent(topBarGO.transform, false);
            RectTransform profileRect = centerProfileGO.AddComponent<RectTransform>();
            profileRect.anchorMin = new Vector2(0.5f, 0.5f);
            profileRect.anchorMax = new Vector2(0.5f, 0.5f);
            profileRect.pivot = new Vector2(0.5f, 0.5f);
            profileRect.anchoredPosition = new Vector2(0, 0);
            profileRect.sizeDelta = new Vector2(280, 130);

            // Avatar Circle
            GameObject avatarGO = new GameObject("AvatarCircle");
            avatarGO.transform.SetParent(centerProfileGO.transform, false);
            RectTransform avatarRect = avatarGO.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.5f, 1f);
            avatarRect.anchorMax = new Vector2(0.5f, 1f);
            avatarRect.pivot = new Vector2(0.5f, 1f);
            avatarRect.anchoredPosition = new Vector2(0, 0);
            avatarRect.sizeDelta = new Vector2(68, 68);

            RoundedRectGraphic avatarG = avatarGO.AddComponent<RoundedRectGraphic>();
            avatarG.IsCapsule = true;
            avatarG.color = new Color(1f, 1f, 1f, 0.07f);
            avatarG.BorderWidth = 1.5f;
            avatarG.BorderColor = BorderSubtle;

            GameObject avatarIconGO = new GameObject("Icon");
            avatarIconGO.transform.SetParent(avatarGO.transform, false);
            RectTransform avatarIconRect = avatarIconGO.AddComponent<RectTransform>();
            avatarIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            avatarIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            avatarIconRect.sizeDelta = new Vector2(40, 40);
            Image avatarIconImg = avatarIconGO.AddComponent<Image>();
            avatarIconImg.sprite = iconUser;
            avatarIconImg.color = TextGray;

            // Player Name
            GameObject playerNameGO = new GameObject("PlayerNameText");
            playerNameGO.transform.SetParent(centerProfileGO.transform, false);
            RectTransform nameRect = playerNameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0, -74);
            nameRect.sizeDelta = new Vector2(260, 26);
            TextMeshProUGUI nameTMP = playerNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) nameTMP.font = dmSansTMPFont;
            nameTMP.text = "JUGADOR_01";
            nameTMP.fontSize = 21;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.characterSpacing = 4f;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = TextWhite;

            // Player Level
            GameObject playerLevelGO = new GameObject("PlayerLevelText");
            playerLevelGO.transform.SetParent(centerProfileGO.transform, false);
            RectTransform levelRect = playerLevelGO.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 1f);
            levelRect.anchorMax = new Vector2(0.5f, 1f);
            levelRect.pivot = new Vector2(0.5f, 1f);
            levelRect.anchoredPosition = new Vector2(0, -102);
            levelRect.sizeDelta = new Vector2(260, 20);
            TextMeshProUGUI levelTMP = playerLevelGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) levelTMP.font = dmSansTMPFont;
            levelTMP.text = "Nivel 7";
            levelTMP.fontSize = 15;
            levelTMP.alignment = TextAlignmentOptions.Center;
            levelTMP.color = TextGray;

            // Right: Coins Pill + Mail + Gift
            GameObject topRightGO = new GameObject("TopRightActions");
            topRightGO.transform.SetParent(topBarGO.transform, false);
            RectTransform rightRect = topRightGO.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1f, 0.5f);
            rightRect.anchorMax = new Vector2(1f, 0.5f);
            rightRect.pivot = new Vector2(1f, 0.5f);
            rightRect.anchoredPosition = new Vector2(0, 0);
            rightRect.sizeDelta = new Vector2(330, 60);

            // Coins Pill (Perfect Procedural Capsule with Gold Border)
            GameObject coinsBadgeGO = new GameObject("CoinsBadge");
            coinsBadgeGO.transform.SetParent(topRightGO.transform, false);
            RectTransform coinsBadgeRect = coinsBadgeGO.AddComponent<RectTransform>();
            coinsBadgeRect.anchorMin = new Vector2(0f, 0.5f);
            coinsBadgeRect.anchorMax = new Vector2(0f, 0.5f);
            coinsBadgeRect.pivot = new Vector2(0f, 0.5f);
            coinsBadgeRect.anchoredPosition = new Vector2(0, 0);
            coinsBadgeRect.sizeDelta = new Vector2(148, 48);

            RoundedRectGraphic coinsG = coinsBadgeGO.AddComponent<RoundedRectGraphic>();
            coinsG.IsCapsule = true;
            coinsG.color = new Color(0f, 0f, 0f, 0.45f);
            coinsG.BorderWidth = 2f;
            coinsG.BorderColor = GoldBorder;

            GameObject coinsIconGO = new GameObject("CoinIcon");
            coinsIconGO.transform.SetParent(coinsBadgeGO.transform, false);
            RectTransform coinsIconRect = coinsIconGO.AddComponent<RectTransform>();
            coinsIconRect.anchorMin = new Vector2(0f, 0.5f);
            coinsIconRect.anchorMax = new Vector2(0f, 0.5f);
            coinsIconRect.pivot = new Vector2(0f, 0.5f);
            coinsIconRect.anchoredPosition = new Vector2(14, 0);
            coinsIconRect.sizeDelta = new Vector2(28, 28);
            Image coinsIconImg = coinsIconGO.AddComponent<Image>();
            coinsIconImg.sprite = coinSprite;
            coinsIconImg.color = Color.white;

            GameObject coinsTextGO = new GameObject("CoinsText");
            coinsTextGO.transform.SetParent(coinsBadgeGO.transform, false);
            RectTransform coinsTextRect = coinsTextGO.AddComponent<RectTransform>();
            coinsTextRect.anchorMin = new Vector2(0f, 0f);
            coinsTextRect.anchorMax = new Vector2(1f, 1f);
            coinsTextRect.offsetMin = new Vector2(46, 0);
            coinsTextRect.offsetMax = new Vector2(-10, 0);
            TextMeshProUGUI coinsTMP = coinsTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) coinsTMP.font = dmSansTMPFont;
            coinsTMP.text = "240";
            coinsTMP.fontSize = 20;
            coinsTMP.fontStyle = FontStyles.Bold;
            coinsTMP.alignment = TextAlignmentOptions.Center;
            coinsTMP.color = TextWhite;

            // Mail Button
            GameObject mailGO = new GameObject("MailButton");
            mailGO.transform.SetParent(topRightGO.transform, false);
            RectTransform mailRect = mailGO.AddComponent<RectTransform>();
            mailRect.anchorMin = new Vector2(0f, 0.5f);
            mailRect.anchorMax = new Vector2(0f, 0.5f);
            mailRect.pivot = new Vector2(0f, 0.5f);
            mailRect.anchoredPosition = new Vector2(170, 0);
            mailRect.sizeDelta = new Vector2(48, 48);
            Image mailImg = mailGO.AddComponent<Image>();
            mailImg.sprite = iconMail;
            mailImg.color = TextGray;

            // Gold notification dot on Mail
            GameObject mailDotGO = new GameObject("Dot");
            mailDotGO.transform.SetParent(mailGO.transform, false);
            RectTransform dotRect = mailDotGO.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(1f, 1f);
            dotRect.anchorMax = new Vector2(1f, 1f);
            dotRect.pivot = new Vector2(1f, 1f);
            dotRect.anchoredPosition = new Vector2(-2, -2);
            dotRect.sizeDelta = new Vector2(14, 14);
            RoundedRectGraphic mailDotG = mailDotGO.AddComponent<RoundedRectGraphic>();
            mailDotG.IsCapsule = true;
            mailDotG.color = Gold;

            // Gift Button
            GameObject giftGO = new GameObject("GiftButton");
            giftGO.transform.SetParent(topRightGO.transform, false);
            RectTransform giftRect = giftGO.AddComponent<RectTransform>();
            giftRect.anchorMin = new Vector2(0f, 0.5f);
            giftRect.anchorMax = new Vector2(0f, 0.5f);
            giftRect.pivot = new Vector2(0f, 0.5f);
            giftRect.anchoredPosition = new Vector2(240, 0);
            giftRect.sizeDelta = new Vector2(48, 48);
            Image giftImg = giftGO.AddComponent<Image>();
            giftImg.sprite = iconGift;
            giftImg.color = TextGray;

            // ====================================================
            // 2. SECTION "SOBRES DISPONIBLES" (Barlow Condensed Bold)
            // ====================================================
            GameObject titleSobresGO = new GameObject("TitleSobres");
            titleSobresGO.transform.SetParent(contentGO.transform, false);
            RectTransform titleSobresRect = titleSobresGO.AddComponent<RectTransform>();
            titleSobresRect.anchorMin = new Vector2(0.5f, 1f);
            titleSobresRect.anchorMax = new Vector2(0.5f, 1f);
            titleSobresRect.pivot = new Vector2(0.5f, 1f);
            titleSobresRect.anchoredPosition = new Vector2(0, -210);
            titleSobresRect.sizeDelta = new Vector2(980, 40);
            TextMeshProUGUI titleSobresTMP = titleSobresGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleSobresTMP.font = barlowTMPFont;
            titleSobresTMP.text = "SOBRES DISPONIBLES";
            titleSobresTMP.fontSize = 32;
            titleSobresTMP.fontStyle = FontStyles.Bold;
            titleSobresTMP.characterSpacing = 10f;
            titleSobresTMP.color = TextWhite;

            // 3 Pack Cards Container
            GameObject packsRowGO = new GameObject("PacksRow");
            packsRowGO.transform.SetParent(contentGO.transform, false);
            RectTransform packsRowRect = packsRowGO.AddComponent<RectTransform>();
            packsRowRect.anchorMin = new Vector2(0.5f, 1f);
            packsRowRect.anchorMax = new Vector2(0.5f, 1f);
            packsRowRect.pivot = new Vector2(0.5f, 1f);
            packsRowRect.anchoredPosition = new Vector2(0, -265);
            packsRowRect.sizeDelta = new Vector2(980, 480);

            Button[] packBtns = new Button[3];
            string[] packLabels = { "SOBRE A", "SOBRE B", "SOBRE C" };
            float packSpacing = 330f;
            float startPackX = -packSpacing;

            for (int i = 0; i < 3; i++)
            {
                bool isFeatured = (i == 1);
                GameObject packCardGO = new GameObject($"PackCard_{i}");
                packCardGO.transform.SetParent(packsRowGO.transform, false);
                RectTransform pRect = packCardGO.AddComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0.5f, 0.5f);
                pRect.anchorMax = new Vector2(0.5f, 0.5f);
                pRect.pivot = new Vector2(0.5f, 0.5f);
                pRect.anchoredPosition = new Vector2(startPackX + i * packSpacing, 0);
                pRect.sizeDelta = new Vector2(305, 470);

                RoundedRectGraphic pG = packCardGO.AddComponent<RoundedRectGraphic>();
                pG.CornerRadius = 24f;
                pG.color = CardBg;
                pG.BorderWidth = isFeatured ? 3f : 1.5f;
                pG.BorderColor = isFeatured ? GoldBorder : BorderSubtle;

                // Label at bottom
                GameObject pLabelGO = new GameObject("Label");
                pLabelGO.transform.SetParent(packCardGO.transform, false);
                RectTransform pLabelRect = pLabelGO.AddComponent<RectTransform>();
                pLabelRect.anchorMin = new Vector2(0f, 0.05f);
                pLabelRect.anchorMax = new Vector2(1f, 0.16f);
                pLabelRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI pLabelTMP = pLabelGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) pLabelTMP.font = dmSansTMPFont;
                pLabelTMP.text = packLabels[i];
                pLabelTMP.fontSize = 20;
                pLabelTMP.fontStyle = FontStyles.Bold;
                pLabelTMP.characterSpacing = 4f;
                pLabelTMP.alignment = TextAlignmentOptions.Center;
                pLabelTMP.color = isFeatured ? Gold : TextGray;

                packBtns[i] = packCardGO.AddComponent<Button>();
            }

            // ====================================================
            // 3. ACTION CARDS
            // ====================================================
            GameObject actionCardsGO = new GameObject("ActionCardsRow");
            actionCardsGO.transform.SetParent(contentGO.transform, false);
            RectTransform actionCardsRect = actionCardsGO.AddComponent<RectTransform>();
            actionCardsRect.anchorMin = new Vector2(0.5f, 1f);
            actionCardsRect.anchorMax = new Vector2(0.5f, 1f);
            actionCardsRect.pivot = new Vector2(0.5f, 1f);
            actionCardsRect.anchoredPosition = new Vector2(0, -780);
            actionCardsRect.sizeDelta = new Vector2(980, 200);

            // Card 1: Evento Especial
            GameObject eventCardGO = new GameObject("EventCard");
            eventCardGO.transform.SetParent(actionCardsGO.transform, false);
            RectTransform eventCardRect = eventCardGO.AddComponent<RectTransform>();
            eventCardRect.anchorMin = new Vector2(0f, 0f);
            eventCardRect.anchorMax = new Vector2(0.485f, 1f);
            eventCardRect.sizeDelta = Vector2.zero;

            RoundedRectGraphic eventG = eventCardGO.AddComponent<RoundedRectGraphic>();
            eventG.CornerRadius = 24f;
            eventG.color = CardBg;
            eventG.BorderWidth = 1.5f;
            eventG.BorderColor = BorderSubtle;

            GameObject eventIconGO = new GameObject("Icon");
            eventIconGO.transform.SetParent(eventCardGO.transform, false);
            RectTransform eventIconRect = eventIconGO.AddComponent<RectTransform>();
            eventIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            eventIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            eventIconRect.anchoredPosition = new Vector2(0, 28);
            eventIconRect.sizeDelta = new Vector2(56, 56);
            Image eventIconImg = eventIconGO.AddComponent<Image>();
            eventIconImg.sprite = iconClock;
            eventIconImg.color = TextGray;

            GameObject eventTitleGO = new GameObject("Title");
            eventTitleGO.transform.SetParent(eventCardGO.transform, false);
            RectTransform eventTitleRect = eventTitleGO.AddComponent<RectTransform>();
            eventTitleRect.anchorMin = new Vector2(0f, 0.22f);
            eventTitleRect.anchorMax = new Vector2(1f, 0.48f);
            eventTitleRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI eventTitleTMP = eventTitleGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) eventTitleTMP.font = dmSansTMPFont;
            eventTitleTMP.text = "Evento especial";
            eventTitleTMP.fontSize = 22;
            eventTitleTMP.fontStyle = FontStyles.Bold;
            eventTitleTMP.alignment = TextAlignmentOptions.Center;
            eventTitleTMP.color = TextWhite;

            GameObject eventTimerGO = new GameObject("Timer");
            eventTimerGO.transform.SetParent(eventCardGO.transform, false);
            RectTransform eventTimerRect = eventTimerGO.AddComponent<RectTransform>();
            eventTimerRect.anchorMin = new Vector2(0f, 0.05f);
            eventTimerRect.anchorMax = new Vector2(1f, 0.22f);
            eventTimerRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI eventTimerTMP = eventTimerGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) eventTimerTMP.font = dmSansTMPFont;
            eventTimerTMP.text = "2d 14h";
            eventTimerTMP.fontSize = 17;
            eventTimerTMP.alignment = TextAlignmentOptions.Center;
            eventTimerTMP.color = TextGray;
            Button eventBtn = eventCardGO.AddComponent<Button>();

            // Card 2: Tienda
            GameObject shopCardGO = new GameObject("ShopCard");
            shopCardGO.transform.SetParent(actionCardsGO.transform, false);
            RectTransform shopCardRect = shopCardGO.AddComponent<RectTransform>();
            shopCardRect.anchorMin = new Vector2(0.515f, 0f);
            shopCardRect.anchorMax = new Vector2(1f, 1f);
            shopCardRect.sizeDelta = Vector2.zero;

            RoundedRectGraphic shopG = shopCardGO.AddComponent<RoundedRectGraphic>();
            shopG.CornerRadius = 24f;
            shopG.color = CardBg;
            shopG.BorderWidth = 1.5f;
            shopG.BorderColor = BorderSubtle;

            GameObject shopIconGO = new GameObject("Icon");
            shopIconGO.transform.SetParent(shopCardGO.transform, false);
            RectTransform shopIconRect = shopIconGO.AddComponent<RectTransform>();
            shopIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            shopIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            shopIconRect.anchoredPosition = new Vector2(0, 24);
            shopIconRect.sizeDelta = new Vector2(56, 56);
            Image shopIconImg = shopIconGO.AddComponent<Image>();
            shopIconImg.sprite = iconShop;
            shopIconImg.color = TextGray;

            GameObject shopTitleGO = new GameObject("Title");
            shopTitleGO.transform.SetParent(shopCardGO.transform, false);
            RectTransform shopTitleRect = shopTitleGO.AddComponent<RectTransform>();
            shopTitleRect.anchorMin = new Vector2(0f, 0.08f);
            shopTitleRect.anchorMax = new Vector2(1f, 0.38f);
            shopTitleRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI shopTitleTMP = shopTitleGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) shopTitleTMP.font = dmSansTMPFont;
            shopTitleTMP.text = "Tienda";
            shopTitleTMP.fontSize = 22;
            shopTitleTMP.fontStyle = FontStyles.Bold;
            shopTitleTMP.alignment = TextAlignmentOptions.Center;
            shopTitleTMP.color = TextWhite;
            Button shopBtn = shopCardGO.AddComponent<Button>();

            // ====================================================
            // 4. BUTTON "MISIONES" (True Procedural Pill Capsule + CheckSquare + Red Dot)
            // ====================================================
            GameObject missionsBtnGO = new GameObject("MissionsButton");
            missionsBtnGO.transform.SetParent(contentGO.transform, false);
            RectTransform missionsRect = missionsBtnGO.AddComponent<RectTransform>();
            missionsRect.anchorMin = new Vector2(1f, 1f);
            missionsRect.anchorMax = new Vector2(1f, 1f);
            missionsRect.pivot = new Vector2(1f, 1f);
            missionsRect.anchoredPosition = new Vector2(-50, -1005);
            missionsRect.sizeDelta = new Vector2(330, 80);

            // Procedural Capsule Graphic (100% Mathematically Perfect Semicircles)
            RoundedRectGraphic missionsG = missionsBtnGO.AddComponent<RoundedRectGraphic>();
            missionsG.IsCapsule = true;
            missionsG.color = Gold;
            Button missionsBtn = missionsBtnGO.AddComponent<Button>();

            // Grouped centered layout
            GameObject contentHolderGO = new GameObject("ContentHolder");
            contentHolderGO.transform.SetParent(missionsBtnGO.transform, false);
            RectTransform chRect = contentHolderGO.AddComponent<RectTransform>();
            chRect.anchorMin = Vector2.zero;
            chRect.anchorMax = Vector2.one;
            chRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup hlg = contentHolderGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 12f;

            GameObject missionsIconGO = new GameObject("CheckIcon");
            missionsIconGO.transform.SetParent(contentHolderGO.transform, false);
            RectTransform missionsIconRect = missionsIconGO.AddComponent<RectTransform>();
            missionsIconRect.sizeDelta = new Vector2(34, 34);
            Image missionsIconImg = missionsIconGO.AddComponent<Image>();
            missionsIconImg.sprite = iconCheckMisiones;
            missionsIconImg.color = Color.white;

            GameObject missionsTextGO = new GameObject("Text");
            missionsTextGO.transform.SetParent(contentHolderGO.transform, false);
            RectTransform missionsTextRect = missionsTextGO.AddComponent<RectTransform>();
            missionsTextRect.sizeDelta = new Vector2(165, 34);
            TextMeshProUGUI missionsTMP = missionsTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) missionsTMP.font = dmSansTMPFont;
            missionsTMP.text = "MISIONES";
            missionsTMP.fontSize = 24;
            missionsTMP.fontStyle = FontStyles.Bold;
            missionsTMP.characterSpacing = 5f;
            missionsTMP.alignment = TextAlignmentOptions.Center;
            missionsTMP.color = Color.black;

            // Red Notification Dot on Misiones (from Figma)
            GameObject redDotGO = new GameObject("RedDotBadge");
            redDotGO.transform.SetParent(missionsBtnGO.transform, false);
            RectTransform redDotRect = redDotGO.AddComponent<RectTransform>();
            redDotRect.anchorMin = new Vector2(1f, 1f);
            redDotRect.anchorMax = new Vector2(1f, 1f);
            redDotRect.pivot = new Vector2(1f, 1f);
            redDotRect.anchoredPosition = new Vector2(-8, -6);
            redDotRect.sizeDelta = new Vector2(16, 16);
            RoundedRectGraphic redDotG = redDotGO.AddComponent<RoundedRectGraphic>();
            redDotG.IsCapsule = true;
            redDotG.color = new Color(1f, 0.231f, 0.188f); // #ff3b30

            // ====================================================
            // 5. SECTION "RACHA DIARIA" (Exact Figma Geometry & Progress)
            // ====================================================
            GameObject streakSectionGO = new GameObject("StreakSection");
            streakSectionGO.transform.SetParent(contentGO.transform, false);
            RectTransform streakSectionRect = streakSectionGO.AddComponent<RectTransform>();
            streakSectionRect.anchorMin = new Vector2(0.5f, 1f);
            streakSectionRect.anchorMax = new Vector2(0.5f, 1f);
            streakSectionRect.pivot = new Vector2(0.5f, 1f);
            streakSectionRect.anchoredPosition = new Vector2(0, -1115);
            streakSectionRect.sizeDelta = new Vector2(980, 230);

            RoundedRectGraphic streakG = streakSectionGO.AddComponent<RoundedRectGraphic>();
            streakG.CornerRadius = 24f;
            streakG.color = CardBg;
            streakG.BorderWidth = 1.5f;
            streakG.BorderColor = BorderSubtle;

            // Header: Title Left (Barlow) + Counter Right (DM Sans)
            GameObject streakTitleGO = new GameObject("StreakTitle");
            streakTitleGO.transform.SetParent(streakSectionGO.transform, false);
            RectTransform streakTitleRect = streakTitleGO.AddComponent<RectTransform>();
            streakTitleRect.anchorMin = new Vector2(0f, 1f);
            streakTitleRect.anchorMax = new Vector2(0.6f, 1f);
            streakTitleRect.pivot = new Vector2(0f, 1f);
            streakTitleRect.anchoredPosition = new Vector2(30, -22);
            streakTitleRect.sizeDelta = new Vector2(0, 32);
            TextMeshProUGUI streakTitleTMP = streakTitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) streakTitleTMP.font = barlowTMPFont;
            streakTitleTMP.text = "RACHA DIARIA";
            streakTitleTMP.fontSize = 28;
            streakTitleTMP.fontStyle = FontStyles.Bold;
            streakTitleTMP.characterSpacing = 8f;
            streakTitleTMP.color = TextWhite;

            GameObject streakDaysGO = new GameObject("StreakDaysText");
            streakDaysGO.transform.SetParent(streakSectionGO.transform, false);
            RectTransform streakDaysRect = streakDaysGO.AddComponent<RectTransform>();
            streakDaysRect.anchorMin = new Vector2(0.4f, 1f);
            streakDaysRect.anchorMax = new Vector2(1f, 1f);
            streakDaysRect.pivot = new Vector2(1f, 1f);
            streakDaysRect.anchoredPosition = new Vector2(-30, -22);
            streakDaysRect.sizeDelta = new Vector2(0, 32);
            TextMeshProUGUI streakDaysTMP = streakDaysGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) streakDaysTMP.font = dmSansTMPFont;
            streakDaysTMP.text = "3 / 5 días";
            streakDaysTMP.fontSize = 20;
            streakDaysTMP.alignment = TextAlignmentOptions.Right;
            streakDaysTMP.color = TextGray;

            // Progress Slider with Procedural Rounded Ends
            GameObject sliderGO = new GameObject("StreakSlider");
            sliderGO.transform.SetParent(streakSectionGO.transform, false);
            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = new Vector2(0, -68);
            sliderRect.sizeDelta = new Vector2(920, 10);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.6f;
            slider.interactable = false;

            GameObject backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(sliderGO.transform, false);
            RectTransform sBgRect = backgroundGO.AddComponent<RectTransform>();
            sBgRect.anchorMin = Vector2.zero;
            sBgRect.anchorMax = Vector2.one;
            sBgRect.sizeDelta = Vector2.zero;
            RoundedRectGraphic sBgG = backgroundGO.AddComponent<RoundedRectGraphic>();
            sBgG.IsCapsule = true;
            sBgG.color = new Color(1f, 1f, 1f, 0.10f);

            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform sFillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            sFillAreaRect.anchorMin = Vector2.zero;
            sFillAreaRect.anchorMax = Vector2.one;
            sFillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform sFillRect = fillGO.AddComponent<RectTransform>();
            sFillRect.anchorMin = Vector2.zero;
            sFillRect.anchorMax = Vector2.one;
            sFillRect.sizeDelta = Vector2.zero;
            RoundedRectGraphic sFillG = fillGO.AddComponent<RoundedRectGraphic>();
            sFillG.IsCapsule = true;
            sFillG.color = Gold;

            slider.fillRect = sFillRect;

            // 5 Day Squircle Boxes (Exact Figma Geometry)
            GameObject[] checkGOs = new GameObject[5];
            float boxSpacing = 184f;
            float startBoxX = -boxSpacing * 2f;

            for (int i = 0; i < 5; i++)
            {
                bool isCompleted = (i < 3);
                GameObject boxGO = new GameObject($"DayBox_{i + 1}");
                boxGO.transform.SetParent(streakSectionGO.transform, false);
                RectTransform bRect = boxGO.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(0.5f, 1f);
                bRect.anchorMax = new Vector2(0.5f, 1f);
                bRect.pivot = new Vector2(0.5f, 1f);
                bRect.anchoredPosition = new Vector2(startBoxX + i * boxSpacing, -98);
                bRect.sizeDelta = new Vector2(76, 76);

                RoundedRectGraphic bG = boxGO.AddComponent<RoundedRectGraphic>();
                bG.CornerRadius = 14f;
                bG.color = isCompleted ? new Color(0.910f, 0.659f, 0.125f, 0.15f) : new Color(1f, 1f, 1f, 0.05f);
                bG.BorderWidth = 1.8f;
                bG.BorderColor = isCompleted ? GoldBorder : BorderSubtle;

                if (isCompleted)
                {
                    GameObject checkIconGO = new GameObject("CheckIcon");
                    checkIconGO.transform.SetParent(boxGO.transform, false);
                    RectTransform checkRect = checkIconGO.AddComponent<RectTransform>();
                    checkRect.anchorMin = new Vector2(0.5f, 0.5f);
                    checkRect.anchorMax = new Vector2(0.5f, 0.5f);
                    checkRect.sizeDelta = new Vector2(38, 38);
                    Image checkImg = checkIconGO.AddComponent<Image>();
                    checkImg.sprite = checkGold;
                    checkImg.color = Color.white;
                    checkGOs[i] = checkIconGO;
                }
                else
                {
                    GameObject dayNumGO = new GameObject("DayNum");
                    dayNumGO.transform.SetParent(boxGO.transform, false);
                    RectTransform numRect = dayNumGO.AddComponent<RectTransform>();
                    numRect.anchorMin = Vector2.zero;
                    numRect.anchorMax = Vector2.one;
                    numRect.sizeDelta = Vector2.zero;
                    TextMeshProUGUI numTMP = dayNumGO.AddComponent<TextMeshProUGUI>();
                    if (dmSansTMPFont != null) numTMP.font = dmSansTMPFont;
                    numTMP.text = $"{i + 1}";
                    numTMP.fontSize = 24;
                    numTMP.fontStyle = FontStyles.Bold;
                    numTMP.alignment = TextAlignmentOptions.Center;
                    numTMP.color = TextDim;
                }
            }

            // ====================================================
            // 6. LIQUID-GLASS BOTTOM NAVIGATION BAR
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

            string[] tabLabels = { "Inicio", "Mis cartas", "Comunidad", "Perfil" };
            Sprite[] tabIcons = { iconHome, iconCards, iconUsers, iconUser };
            Button[] tabBtns = new Button[4];
            float tabSpacing = 235f;
            float startTabX = -tabSpacing * 1.5f;

            for (int i = 0; i < 4; i++)
            {
                bool isTabActive = (i == 0);
                GameObject tabGO = new GameObject($"Tab_{tabLabels[i]}");
                tabGO.transform.SetParent(bottomBarGO.transform, false);
                RectTransform tabRect = tabGO.AddComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(0.5f, 0.5f);
                tabRect.anchorMax = new Vector2(0.5f, 0.5f);
                tabRect.pivot = new Vector2(0.5f, 0.5f);
                tabRect.anchoredPosition = new Vector2(startTabX + i * tabSpacing, 0);
                tabRect.sizeDelta = isTabActive ? new Vector2(185, 100) : new Vector2(155, 100);

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

            // Assign Serialized Properties on HomeScreenController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("playerNameText").objectReferenceValue = nameTMP;
            so.FindProperty("playerLevelText").objectReferenceValue = levelTMP;
            so.FindProperty("coinsText").objectReferenceValue = coinsTMP;

            so.FindProperty("streakProgressBar").objectReferenceValue = slider;
            so.FindProperty("streakProgressText").objectReferenceValue = streakDaysTMP;

            SerializedProperty checkProp = so.FindProperty("streakDayCheckIcons");
            checkProp.arraySize = checkGOs.Length;
            for (int i = 0; i < checkGOs.Length; i++)
            {
                checkProp.GetArrayElementAtIndex(i).objectReferenceValue = checkGOs[i];
            }

            so.FindProperty("packAButton").objectReferenceValue = packBtns[0];
            so.FindProperty("packBButton").objectReferenceValue = packBtns[1];
            so.FindProperty("packCButton").objectReferenceValue = packBtns[2];

            so.FindProperty("specialEventButton").objectReferenceValue = eventBtn;
            so.FindProperty("shopButton").objectReferenceValue = shopBtn;
            so.FindProperty("missionsButton").objectReferenceValue = missionsBtn;

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[3];

            // Save Prefab in Assets/_Project/Prefabs/UI/
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/HomeScreenUI.prefab";
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

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Inicio guardada como Escena Principal (Build Index 0) y Prefab Oficial (HomeScreenUI.prefab)!</color>");
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
