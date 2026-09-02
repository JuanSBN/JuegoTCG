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
            topBarRect.anchoredPosition = new Vector2(0, -70);
            topBarRect.sizeDelta = new Vector2(1000, 160);

            // Left: 2 square slot badges
            for (int i = 0; i < 2; i++)
            {
                GameObject slotGO = new GameObject($"SlotBadge_{i}");
                slotGO.transform.SetParent(topBarGO.transform, false);
                RectTransform slotRect = slotGO.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0.5f);
                slotRect.anchorMax = new Vector2(0f, 0.5f);
                slotRect.pivot = new Vector2(0f, 0.5f);
                slotRect.anchoredPosition = new Vector2(i * 100, 0);
                slotRect.sizeDelta = new Vector2(88, 88);

                RoundedRectGraphic slotG = slotGO.AddComponent<RoundedRectGraphic>();
                slotG.CornerRadius = 18f;
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
            profileRect.sizeDelta = new Vector2(340, 160);

            // Avatar Circle
            GameObject avatarGO = new GameObject("AvatarCircle");
            avatarGO.transform.SetParent(centerProfileGO.transform, false);
            RectTransform avatarRect = avatarGO.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.5f, 1f);
            avatarRect.anchorMax = new Vector2(0.5f, 1f);
            avatarRect.pivot = new Vector2(0.5f, 1f);
            avatarRect.anchoredPosition = new Vector2(0, 0);
            avatarRect.sizeDelta = new Vector2(86, 86);

            RoundedRectGraphic avatarG = avatarGO.AddComponent<RoundedRectGraphic>();
            avatarG.IsCapsule = true;
            avatarG.color = new Color(1f, 1f, 1f, 0.07f);
            avatarG.BorderWidth = 2f;
            avatarG.BorderColor = GoldBorder;

            GameObject avatarIconGO = new GameObject("Icon");
            avatarIconGO.transform.SetParent(avatarGO.transform, false);
            RectTransform avatarIconRect = avatarIconGO.AddComponent<RectTransform>();
            avatarIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            avatarIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            avatarIconRect.sizeDelta = new Vector2(52, 52);
            Image avatarIconImg = avatarIconGO.AddComponent<Image>();
            avatarIconImg.sprite = iconUser;
            avatarIconImg.color = TextWhite;

            // Player Name
            GameObject playerNameGO = new GameObject("PlayerNameText");
            playerNameGO.transform.SetParent(centerProfileGO.transform, false);
            RectTransform nameRect = playerNameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0, -94);
            nameRect.sizeDelta = new Vector2(320, 32);
            TextMeshProUGUI nameTMP = playerNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) nameTMP.font = dmSansTMPFont;
            nameTMP.text = "JUGADOR_01";
            nameTMP.fontSize = 26;
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
            levelRect.anchoredPosition = new Vector2(0, -128);
            levelRect.sizeDelta = new Vector2(320, 26);
            TextMeshProUGUI levelTMP = playerLevelGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) levelTMP.font = dmSansTMPFont;
            levelTMP.text = "Nivel 7";
            levelTMP.fontSize = 20;
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
            rightRect.sizeDelta = new Vector2(360, 70);

            // Coins Pill
            GameObject coinsBadgeGO = new GameObject("CoinsBadge");
            coinsBadgeGO.transform.SetParent(topRightGO.transform, false);
            RectTransform coinsBadgeRect = coinsBadgeGO.AddComponent<RectTransform>();
            coinsBadgeRect.anchorMin = new Vector2(0f, 0.5f);
            coinsBadgeRect.anchorMax = new Vector2(0f, 0.5f);
            coinsBadgeRect.pivot = new Vector2(0f, 0.5f);
            coinsBadgeRect.anchoredPosition = new Vector2(0, 0);
            coinsBadgeRect.sizeDelta = new Vector2(170, 58);

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
            coinsIconRect.anchoredPosition = new Vector2(16, 0);
            coinsIconRect.sizeDelta = new Vector2(34, 34);
            Image coinsIconImg = coinsIconGO.AddComponent<Image>();
            coinsIconImg.sprite = coinSprite;
            coinsIconImg.color = Color.white;

            GameObject coinsTextGO = new GameObject("CoinsText");
            coinsTextGO.transform.SetParent(coinsBadgeGO.transform, false);
            RectTransform coinsTextRect = coinsTextGO.AddComponent<RectTransform>();
            coinsTextRect.anchorMin = new Vector2(0f, 0f);
            coinsTextRect.anchorMax = new Vector2(1f, 1f);
            coinsTextRect.offsetMin = new Vector2(56, 0);
            coinsTextRect.offsetMax = new Vector2(-12, 0);
            TextMeshProUGUI coinsTMP = coinsTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) coinsTMP.font = dmSansTMPFont;
            coinsTMP.text = "240";
            coinsTMP.fontSize = 24;
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
            mailRect.anchoredPosition = new Vector2(190, 0);
            mailRect.sizeDelta = new Vector2(58, 58);
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
            dotRect.sizeDelta = new Vector2(16, 16);
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
            giftRect.anchoredPosition = new Vector2(270, 0);
            giftRect.sizeDelta = new Vector2(58, 58);
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
            titleSobresRect.anchoredPosition = new Vector2(0, -255);
            titleSobresRect.sizeDelta = new Vector2(1000, 50);
            TextMeshProUGUI titleSobresTMP = titleSobresGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) titleSobresTMP.font = barlowTMPFont;
            titleSobresTMP.text = "SOBRES DISPONIBLES";
            titleSobresTMP.fontSize = 40;
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
            packsRowRect.anchoredPosition = new Vector2(0, -325);
            packsRowRect.sizeDelta = new Vector2(1000, 680);

            Button[] packBtns = new Button[3];
            string[] packLabels = { "SOBRE A", "SOBRE B", "SOBRE C" };
            float packSpacing = 340f;
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
                pRect.sizeDelta = isFeatured ? new Vector2(340, 660) : new Vector2(310, 610);

                RoundedRectGraphic pG = packCardGO.AddComponent<RoundedRectGraphic>();
                pG.CornerRadius = 28f;
                pG.color = isFeatured ? new Color(0.06f, 0.12f, 0.09f) : CardBg;
                pG.BorderWidth = isFeatured ? 3.5f : 1.5f;
                pG.BorderColor = isFeatured ? Gold : BorderSubtle;

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
                pLabelTMP.fontSize = isFeatured ? 26 : 22;
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
            actionCardsRect.anchoredPosition = new Vector2(0, -1040);
            actionCardsRect.sizeDelta = new Vector2(1000, 240);

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
            eventIconRect.anchoredPosition = new Vector2(0, 36);
            eventIconRect.sizeDelta = new Vector2(68, 68);
            Image eventIconImg = eventIconGO.AddComponent<Image>();
            eventIconImg.sprite = iconClock;
            eventIconImg.color = TextWhite;

            GameObject eventTitleGO = new GameObject("Title");
            eventTitleGO.transform.SetParent(eventCardGO.transform, false);
            RectTransform eventTitleRect = eventTitleGO.AddComponent<RectTransform>();
            eventTitleRect.anchorMin = new Vector2(0f, 0.22f);
            eventTitleRect.anchorMax = new Vector2(1f, 0.48f);
            eventTitleRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI eventTitleTMP = eventTitleGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) eventTitleTMP.font = dmSansTMPFont;
            eventTitleTMP.text = "Evento especial";
            eventTitleTMP.fontSize = 26;
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
            eventTimerTMP.fontSize = 20;
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
            shopIconRect.anchoredPosition = new Vector2(0, 32);
            shopIconRect.sizeDelta = new Vector2(68, 68);
            Image shopIconImg = shopIconGO.AddComponent<Image>();
            shopIconImg.sprite = iconShop;
            shopIconImg.color = Gold;

            GameObject shopTitleGO = new GameObject("Title");
            shopTitleGO.transform.SetParent(shopCardGO.transform, false);
            RectTransform shopTitleRect = shopTitleGO.AddComponent<RectTransform>();
            shopTitleRect.anchorMin = new Vector2(0f, 0.08f);
            shopTitleRect.anchorMax = new Vector2(1f, 0.38f);
            shopTitleRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI shopTitleTMP = shopTitleGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) shopTitleTMP.font = dmSansTMPFont;
            shopTitleTMP.text = "Tienda";
            shopTitleTMP.fontSize = 26;
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
            missionsRect.anchoredPosition = new Vector2(-40, -1310);
            missionsRect.sizeDelta = new Vector2(340, 90);

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
            hlg.spacing = 14f;

            GameObject missionsIconGO = new GameObject("CheckIcon");
            missionsIconGO.transform.SetParent(contentHolderGO.transform, false);
            RectTransform missionsIconRect = missionsIconGO.AddComponent<RectTransform>();
            missionsIconRect.sizeDelta = new Vector2(38, 38);
            Image missionsIconImg = missionsIconGO.AddComponent<Image>();
            missionsIconImg.sprite = iconCheckMisiones;
            missionsIconImg.color = Color.black;
            missionsIconImg.raycastTarget = false;

            GameObject missionsTextGO = new GameObject("Text");
            missionsTextGO.transform.SetParent(contentHolderGO.transform, false);
            RectTransform missionsTextRect = missionsTextGO.AddComponent<RectTransform>();
            missionsTextRect.sizeDelta = new Vector2(180, 38);
            TextMeshProUGUI missionsTMP = missionsTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) missionsTMP.font = dmSansTMPFont;
            missionsTMP.text = "MISIONES";
            missionsTMP.fontSize = 28;
            missionsTMP.fontStyle = FontStyles.Bold;
            missionsTMP.characterSpacing = 5f;
            missionsTMP.alignment = TextAlignmentOptions.Center;
            missionsTMP.color = Color.black;
            missionsTMP.raycastTarget = false;

            // Red Notification Dot on Misiones (from Figma)
            GameObject redDotGO = new GameObject("RedDotBadge");
            redDotGO.transform.SetParent(missionsBtnGO.transform, false);
            RectTransform redDotRect = redDotGO.AddComponent<RectTransform>();
            redDotRect.anchorMin = new Vector2(1f, 1f);
            redDotRect.anchorMax = new Vector2(1f, 1f);
            redDotRect.pivot = new Vector2(1f, 1f);
            redDotRect.anchoredPosition = new Vector2(-8, -6);
            redDotRect.sizeDelta = new Vector2(20, 20);
            RoundedRectGraphic redDotG = redDotGO.AddComponent<RoundedRectGraphic>();
            redDotG.IsCapsule = true;
            redDotG.color = new Color(1f, 0.231f, 0.188f); // #ff3b30
            redDotG.raycastTarget = false;

            missionsBtnGO.AddComponent<MissionsButtonTrigger>();

            // ====================================================
            // 5. SECTION "RACHA DIARIA" (Exact Figma Geometry & Progress)
            // ====================================================
            GameObject streakSectionGO = new GameObject("StreakSection");
            streakSectionGO.transform.SetParent(contentGO.transform, false);
            RectTransform streakSectionRect = streakSectionGO.AddComponent<RectTransform>();
            streakSectionRect.anchorMin = new Vector2(0.5f, 1f);
            streakSectionRect.anchorMax = new Vector2(0.5f, 1f);
            streakSectionRect.pivot = new Vector2(0.5f, 1f);
            streakSectionRect.anchoredPosition = new Vector2(0, -1430);
            streakSectionRect.sizeDelta = new Vector2(1000, 300);

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
            streakTitleRect.anchoredPosition = new Vector2(36, -26);
            streakTitleRect.sizeDelta = new Vector2(0, 38);
            TextMeshProUGUI streakTitleTMP = streakTitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) streakTitleTMP.font = barlowTMPFont;
            streakTitleTMP.text = "RACHA DIARIA";
            streakTitleTMP.fontSize = 32;
            streakTitleTMP.fontStyle = FontStyles.Bold;
            streakTitleTMP.characterSpacing = 8f;
            streakTitleTMP.color = TextWhite;

            GameObject streakDaysGO = new GameObject("StreakDaysText");
            streakDaysGO.transform.SetParent(streakSectionGO.transform, false);
            RectTransform streakDaysRect = streakDaysGO.AddComponent<RectTransform>();
            streakDaysRect.anchorMin = new Vector2(0.4f, 1f);
            streakDaysRect.anchorMax = new Vector2(1f, 1f);
            streakDaysRect.pivot = new Vector2(1f, 1f);
            streakDaysRect.anchoredPosition = new Vector2(-36, -26);
            streakDaysRect.sizeDelta = new Vector2(0, 38);
            TextMeshProUGUI streakDaysTMP = streakDaysGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) streakDaysTMP.font = dmSansTMPFont;
            streakDaysTMP.text = "3 / 5 días";
            streakDaysTMP.fontSize = 24;
            streakDaysTMP.alignment = TextAlignmentOptions.Right;
            streakDaysTMP.color = TextGray;

            // Progress Slider with Procedural Rounded Ends
            GameObject sliderGO = new GameObject("StreakSlider");
            sliderGO.transform.SetParent(streakSectionGO.transform, false);
            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = new Vector2(0, -84);
            sliderRect.sizeDelta = new Vector2(930, 12);

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
                bRect.anchoredPosition = new Vector2(startBoxX + i * boxSpacing, -130);
                bRect.sizeDelta = new Vector2(98, 98);

                RoundedRectGraphic bG = boxGO.AddComponent<RoundedRectGraphic>();
                bG.CornerRadius = 18f;
                bG.color = isCompleted ? new Color(0.910f, 0.659f, 0.125f, 0.15f) : new Color(1f, 1f, 1f, 0.05f);
                bG.BorderWidth = 2f;
                bG.BorderColor = isCompleted ? GoldBorder : BorderSubtle;

                if (isCompleted)
                {
                    GameObject checkIconGO = new GameObject("CheckIcon");
                    checkIconGO.transform.SetParent(boxGO.transform, false);
                    RectTransform checkRect = checkIconGO.AddComponent<RectTransform>();
                    checkRect.anchorMin = new Vector2(0.5f, 0.5f);
                    checkRect.anchorMax = new Vector2(0.5f, 0.5f);
                    checkRect.sizeDelta = new Vector2(48, 48);
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
                    numTMP.fontSize = 28;
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

            string[] tabLabels = { "Inicio", "Mis cartas", "Tienda", "Comunidad", "Perfil" };
            Sprite[] tabIcons = { iconHome, iconCards, iconShop, iconUsers, iconUser };
            Button[] tabBtns = new Button[5];
            float tabSpacing = 188f;
            float startTabX = -tabSpacing * 2f;

            for (int i = 0; i < 5; i++)
            {
                bool isTabActive = (i == 0);
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

            // ====================================================
            // 7. MISSIONS MODAL (Sub-Pantalla de Misiones Diarias)
            // ====================================================
            Sprite iconClose = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_close.png");
            Sprite milestoneGiftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_milestone_gift.png");
            Sprite modalBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_modal_bg.png");
            Sprite missionCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_mission_card.png");
            Sprite missionCardDoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_mission_card_done.png");

            GameObject modalRootGO = new GameObject("MissionsModal");
            modalRootGO.transform.SetParent(canvasGO.transform, false);
            RectTransform modalRootRect = modalRootGO.AddComponent<RectTransform>();
            modalRootRect.anchorMin = Vector2.zero;
            modalRootRect.anchorMax = Vector2.one;
            modalRootRect.sizeDelta = Vector2.zero;
            CanvasGroup modalCG = modalRootGO.AddComponent<CanvasGroup>();
            MissionsModalController modalCtrl = modalRootGO.AddComponent<MissionsModalController>();

            // 1. Blurred Background Snapshot (Optical Blur)
            GameObject blurBackdropGO = new GameObject("BlurBackdrop");
            blurBackdropGO.transform.SetParent(modalRootGO.transform, false);
            RectTransform blurRect = blurBackdropGO.AddComponent<RectTransform>();
            blurRect.anchorMin = Vector2.zero;
            blurRect.anchorMax = Vector2.one;
            blurRect.sizeDelta = Vector2.zero;
            RawImage blurImg = blurBackdropGO.AddComponent<RawImage>();
            blurImg.raycastTarget = false;
            blurBackdropGO.SetActive(false);

            // 2. Backdrop (Deep Semi-Transparent Dark Overlay)
            GameObject modalBackdropGO = new GameObject("Backdrop");
            modalBackdropGO.transform.SetParent(modalRootGO.transform, false);
            RectTransform backdropRect = modalBackdropGO.AddComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.sizeDelta = Vector2.zero;
            Image backdropImg = modalBackdropGO.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.58f);
            Button backdropBtn = modalBackdropGO.AddComponent<Button>();

            // Modal Box Card
            GameObject modalBoxGO = new GameObject("ModalBox");
            modalBoxGO.transform.SetParent(modalRootGO.transform, false);
            RectTransform modalBoxRect = modalBoxGO.AddComponent<RectTransform>();
            modalBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalBoxRect.pivot = new Vector2(0.5f, 0.5f);
            modalBoxRect.anchoredPosition = new Vector2(0, 20);
            modalBoxRect.sizeDelta = new Vector2(940, 1180);

            Image modalBoxImg = modalBoxGO.AddComponent<Image>();
            if (modalBgSprite != null)
            {
                modalBoxImg.sprite = modalBgSprite;
                modalBoxImg.type = Image.Type.Sliced;
            }
            else
            {
                modalBoxImg.color = new Color(0.047f, 0.094f, 0.063f);
            }

            // Header: Title + Close Button
            GameObject modalHeaderGO = new GameObject("Header");
            modalHeaderGO.transform.SetParent(modalBoxGO.transform, false);
            RectTransform modalHeaderRect = modalHeaderGO.AddComponent<RectTransform>();
            modalHeaderRect.anchorMin = new Vector2(0f, 1f);
            modalHeaderRect.anchorMax = new Vector2(1f, 1f);
            modalHeaderRect.pivot = new Vector2(0.5f, 1f);
            modalHeaderRect.anchoredPosition = new Vector2(0, -35);
            modalHeaderRect.sizeDelta = new Vector2(0, 60);

            GameObject modalTitleGO = new GameObject("Title");
            modalTitleGO.transform.SetParent(modalHeaderGO.transform, false);
            RectTransform modalTitleRect = modalTitleGO.AddComponent<RectTransform>();
            modalTitleRect.anchorMin = new Vector2(0f, 0f);
            modalTitleRect.anchorMax = new Vector2(0.8f, 1f);
            modalTitleRect.anchoredPosition = new Vector2(40, 0);
            modalTitleRect.sizeDelta = new Vector2(0, 0);
            TextMeshProUGUI modalTitleTMP = modalTitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) modalTitleTMP.font = barlowTMPFont;
            modalTitleTMP.text = "MISIONES DIARIAS";
            modalTitleTMP.fontSize = 38;
            modalTitleTMP.fontStyle = FontStyles.Bold;
            modalTitleTMP.characterSpacing = 8f;
            modalTitleTMP.color = TextWhite;

            GameObject closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(modalHeaderGO.transform, false);
            RectTransform closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRect.pivot = new Vector2(1f, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-36, 0);
            closeBtnRect.sizeDelta = new Vector2(46, 46);
            Image closeBtnImg = closeBtnGO.AddComponent<Image>();
            if (iconClose != null) closeBtnImg.sprite = iconClose;
            closeBtnImg.color = new Color(1f, 1f, 1f, 0.65f);
            Button closeBtn = closeBtnGO.AddComponent<Button>();

            // Milestone Track Container
            GameObject milestoneTrackGO = new GameObject("MilestoneTrackContainer");
            milestoneTrackGO.transform.SetParent(modalBoxGO.transform, false);
            RectTransform milestoneTrackRect = milestoneTrackGO.AddComponent<RectTransform>();
            milestoneTrackRect.anchorMin = new Vector2(0.5f, 1f);
            milestoneTrackRect.anchorMax = new Vector2(0.5f, 1f);
            milestoneTrackRect.pivot = new Vector2(0.5f, 1f);
            milestoneTrackRect.anchoredPosition = new Vector2(0, -115);
            milestoneTrackRect.sizeDelta = new Vector2(860, 180);

            // Milestone Gift 1 at 50%
            GameObject gift1GO = new GameObject("MilestoneGift1");
            gift1GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform gift1Rect = gift1GO.AddComponent<RectTransform>();
            gift1Rect.anchorMin = new Vector2(0.5f, 1f);
            gift1Rect.anchorMax = new Vector2(0.5f, 1f);
            gift1Rect.pivot = new Vector2(0.5f, 1f);
            gift1Rect.anchoredPosition = new Vector2(0, 0);
            gift1Rect.sizeDelta = new Vector2(80, 80);
            Image gift1Img = gift1GO.AddComponent<Image>();
            if (milestoneGiftSprite != null) gift1Img.sprite = milestoneGiftSprite;

            // Connector 1
            GameObject conn1GO = new GameObject("Connector1");
            conn1GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform conn1Rect = conn1GO.AddComponent<RectTransform>();
            conn1Rect.anchorMin = new Vector2(0.5f, 1f);
            conn1Rect.anchorMax = new Vector2(0.5f, 1f);
            conn1Rect.anchoredPosition = new Vector2(0, -82);
            conn1Rect.sizeDelta = new Vector2(2, 16);
            Image conn1Img = conn1GO.AddComponent<Image>();
            conn1Img.color = new Color(GoldBorder.r, GoldBorder.g, GoldBorder.b, 0.4f);

            // Milestone Gift 2 at 100%
            GameObject gift2GO = new GameObject("MilestoneGift2");
            gift2GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform gift2Rect = gift2GO.AddComponent<RectTransform>();
            gift2Rect.anchorMin = new Vector2(1f, 1f);
            gift2Rect.anchorMax = new Vector2(1f, 1f);
            gift2Rect.pivot = new Vector2(1f, 1f);
            gift2Rect.anchoredPosition = new Vector2(0, 0);
            gift2Rect.sizeDelta = new Vector2(80, 80);
            Image gift2Img = gift2GO.AddComponent<Image>();
            if (milestoneGiftSprite != null) gift2Img.sprite = milestoneGiftSprite;

            // Connector 2
            GameObject conn2GO = new GameObject("Connector2");
            conn2GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform conn2Rect = conn2GO.AddComponent<RectTransform>();
            conn2Rect.anchorMin = new Vector2(1f, 1f);
            conn2Rect.anchorMax = new Vector2(1f, 1f);
            conn2Rect.pivot = new Vector2(1f, 1f);
            conn2Rect.anchoredPosition = new Vector2(-40, -82);
            conn2Rect.sizeDelta = new Vector2(2, 16);
            Image conn2Img = conn2GO.AddComponent<Image>();
            conn2Img.color = new Color(GoldBorder.r, GoldBorder.g, GoldBorder.b, 0.4f);

            // Milestone Slider Bar (Track)
            GameObject mSliderGO = new GameObject("MilestoneSlider");
            mSliderGO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform mSliderRect = mSliderGO.AddComponent<RectTransform>();
            mSliderRect.anchorMin = new Vector2(0.5f, 1f);
            mSliderRect.anchorMax = new Vector2(0.5f, 1f);
            mSliderRect.pivot = new Vector2(0.5f, 1f);
            mSliderRect.anchoredPosition = new Vector2(0, -100);
            mSliderRect.sizeDelta = new Vector2(860, 14);

            Slider mSlider = mSliderGO.AddComponent<Slider>();
            mSlider.interactable = false;

            GameObject mBgGO = new GameObject("Background");
            mBgGO.transform.SetParent(mSliderGO.transform, false);
            RectTransform mBgRect = mBgGO.AddComponent<RectTransform>();
            mBgRect.anchorMin = Vector2.zero;
            mBgRect.anchorMax = Vector2.one;
            mBgRect.sizeDelta = Vector2.zero;
            RoundedRectGraphic mBgG = mBgGO.AddComponent<RoundedRectGraphic>();
            mBgG.IsCapsule = true;
            mBgG.color = new Color(1f, 1f, 1f, 0.10f);

            GameObject mFillAreaGO = new GameObject("Fill Area");
            mFillAreaGO.transform.SetParent(mSliderGO.transform, false);
            RectTransform mFillAreaRect = mFillAreaGO.AddComponent<RectTransform>();
            mFillAreaRect.anchorMin = Vector2.zero;
            mFillAreaRect.anchorMax = Vector2.one;
            mFillAreaRect.sizeDelta = Vector2.zero;

            GameObject mFillGO = new GameObject("Fill");
            mFillGO.transform.SetParent(mFillAreaGO.transform, false);
            RectTransform mFillRect = mFillGO.AddComponent<RectTransform>();
            mFillRect.anchorMin = Vector2.zero;
            mFillRect.anchorMax = Vector2.one;
            mFillRect.sizeDelta = Vector2.zero;
            RoundedRectGraphic mFillG = mFillGO.AddComponent<RoundedRectGraphic>();
            mFillG.IsCapsule = true;
            mFillG.color = Gold;
            mSlider.fillRect = mFillRect;

            // Checkpoint Dots on Track
            GameObject dot1GO = new GameObject("Dot1");
            dot1GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform dot1Rect = dot1GO.AddComponent<RectTransform>();
            dot1Rect.anchorMin = new Vector2(0.5f, 1f);
            dot1Rect.anchorMax = new Vector2(0.5f, 1f);
            dot1Rect.pivot = new Vector2(0.5f, 0.5f);
            dot1Rect.anchoredPosition = new Vector2(0, -107);
            dot1Rect.sizeDelta = new Vector2(26, 26);
            Image dot1Img = dot1GO.AddComponent<Image>();
            if (circleSprite != null) dot1Img.sprite = circleSprite;
            dot1Img.color = new Color(0.2f, 0.25f, 0.2f);

            GameObject dot2GO = new GameObject("Dot2");
            dot2GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform dot2Rect = dot2GO.AddComponent<RectTransform>();
            dot2Rect.anchorMin = new Vector2(1f, 1f);
            dot2Rect.anchorMax = new Vector2(1f, 1f);
            dot2Rect.pivot = new Vector2(1f, 0.5f);
            dot2Rect.anchoredPosition = new Vector2(0, -107);
            dot2Rect.sizeDelta = new Vector2(26, 26);
            Image dot2Img = dot2GO.AddComponent<Image>();
            if (circleSprite != null) dot2Img.sprite = circleSprite;
            dot2Img.color = new Color(0.2f, 0.25f, 0.2f);

            // Milestone labels under bar
            GameObject lbl1GO = new GameObject("Label2Misiones");
            lbl1GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform lbl1Rect = lbl1GO.AddComponent<RectTransform>();
            lbl1Rect.anchorMin = new Vector2(0.5f, 1f);
            lbl1Rect.anchorMax = new Vector2(0.5f, 1f);
            lbl1Rect.pivot = new Vector2(0.5f, 1f);
            lbl1Rect.anchoredPosition = new Vector2(0, -125);
            lbl1Rect.sizeDelta = new Vector2(200, 24);
            TextMeshProUGUI lbl1TMP = lbl1GO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) lbl1TMP.font = dmSansTMPFont;
            lbl1TMP.text = "2 misiones";
            lbl1TMP.fontSize = 20;
            lbl1TMP.alignment = TextAlignmentOptions.Center;
            lbl1TMP.color = TextDim;

            GameObject lbl2GO = new GameObject("Label4Misiones");
            lbl2GO.transform.SetParent(milestoneTrackGO.transform, false);
            RectTransform lbl2Rect = lbl2GO.AddComponent<RectTransform>();
            lbl2Rect.anchorMin = new Vector2(1f, 1f);
            lbl2Rect.anchorMax = new Vector2(1f, 1f);
            lbl2Rect.pivot = new Vector2(1f, 1f);
            lbl2Rect.anchoredPosition = new Vector2(0, -125);
            lbl2Rect.sizeDelta = new Vector2(200, 24);
            TextMeshProUGUI lbl2TMP = lbl2GO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) lbl2TMP.font = dmSansTMPFont;
            lbl2TMP.text = "4 misiones";
            lbl2TMP.fontSize = 20;
            lbl2TMP.alignment = TextAlignmentOptions.Right;
            lbl2TMP.color = TextDim;

            // Stats Row: Completed count (Left) + Reset Timer (Right)
            GameObject statsRowGO = new GameObject("StatsRow");
            statsRowGO.transform.SetParent(modalBoxGO.transform, false);
            RectTransform statsRowRect = statsRowGO.AddComponent<RectTransform>();
            statsRowRect.anchorMin = new Vector2(0.5f, 1f);
            statsRowRect.anchorMax = new Vector2(0.5f, 1f);
            statsRowRect.pivot = new Vector2(0.5f, 1f);
            statsRowRect.anchoredPosition = new Vector2(0, -280);
            statsRowRect.sizeDelta = new Vector2(860, 40);

            GameObject compCountGO = new GameObject("CompletedCount");
            compCountGO.transform.SetParent(statsRowGO.transform, false);
            RectTransform compCountRect = compCountGO.AddComponent<RectTransform>();
            compCountRect.anchorMin = new Vector2(0f, 0f);
            compCountRect.anchorMax = new Vector2(0.45f, 1f);
            compCountRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI compCountTMP = compCountGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) compCountTMP.font = dmSansTMPFont;
            compCountTMP.text = "Completadas: <color=white><b>0</b></color>";
            compCountTMP.fontSize = 24;
            compCountTMP.color = TextGray;
            compCountTMP.alignment = TextAlignmentOptions.Left;

            GameObject timerGO = new GameObject("ResetTimer");
            timerGO.transform.SetParent(statsRowGO.transform, false);
            RectTransform timerRect = timerGO.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.45f, 0f);
            timerRect.anchorMax = new Vector2(1f, 1f);
            timerRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI timerTMP = timerGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) timerTMP.font = dmSansTMPFont;
            timerTMP.text = "Se reinicia en 05h 41min";
            timerTMP.fontSize = 22;
            timerTMP.color = TextGray;
            timerTMP.alignment = TextAlignmentOptions.Right;

            // Mission Cards Vertical Container
            GameObject missionListGO = new GameObject("MissionsList");
            missionListGO.transform.SetParent(modalBoxGO.transform, false);
            RectTransform missionListRect = missionListGO.AddComponent<RectTransform>();
            missionListRect.anchorMin = new Vector2(0.5f, 1f);
            missionListRect.anchorMax = new Vector2(0.5f, 1f);
            missionListRect.pivot = new Vector2(0.5f, 1f);
            missionListRect.anchoredPosition = new Vector2(0, -335);
            missionListRect.sizeDelta = new Vector2(860, 800);

            VerticalLayoutGroup vlg = missionListGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 18f;

            List<MissionRowView> rowViews = new List<MissionRowView>();
            for (int i = 0; i < 4; i++)
            {
                GameObject rowGO = new GameObject($"MissionRow_{i}");
                rowGO.transform.SetParent(missionListGO.transform, false);
                RectTransform rowRect = rowGO.AddComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(860, 165);

                Image rowImg = rowGO.AddComponent<Image>();
                if (missionCardSprite != null)
                {
                    rowImg.sprite = missionCardSprite;
                    rowImg.type = Image.Type.Sliced;
                }
                else
                {
                    rowImg.color = new Color(1f, 1f, 1f, 0.05f);
                }

                // Row Title (Left)
                GameObject rowTitleGO = new GameObject("MissionTitle");
                rowTitleGO.transform.SetParent(rowGO.transform, false);
                RectTransform rowTitleRect = rowTitleGO.AddComponent<RectTransform>();
                rowTitleRect.anchorMin = new Vector2(0f, 1f);
                rowTitleRect.anchorMax = new Vector2(0.75f, 1f);
                rowTitleRect.pivot = new Vector2(0f, 1f);
                rowTitleRect.anchoredPosition = new Vector2(28, -26);
                rowTitleRect.sizeDelta = new Vector2(0, 36);
                TextMeshProUGUI rowTitleTMP = rowTitleGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) rowTitleTMP.font = dmSansTMPFont;
                rowTitleTMP.fontSize = 26;
                rowTitleTMP.color = TextWhite;

                // Status / Count (Right)
                GameObject rowStatusGO = new GameObject("StatusText");
                rowStatusGO.transform.SetParent(rowGO.transform, false);
                RectTransform rowStatusRect = rowStatusGO.AddComponent<RectTransform>();
                rowStatusRect.anchorMin = new Vector2(0.75f, 1f);
                rowStatusRect.anchorMax = new Vector2(1f, 1f);
                rowStatusRect.pivot = new Vector2(1f, 1f);
                rowStatusRect.anchoredPosition = new Vector2(-28, -26);
                rowStatusRect.sizeDelta = new Vector2(0, 36);
                TextMeshProUGUI rowStatusTMP = rowStatusGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) rowStatusTMP.font = dmSansTMPFont;
                rowStatusTMP.fontSize = 24;
                rowStatusTMP.color = TextGray;
                rowStatusTMP.alignment = TextAlignmentOptions.Right;

                // Micro Progress Bar
                GameObject rowBarGO = new GameObject("ProgressBar");
                rowBarGO.transform.SetParent(rowGO.transform, false);
                RectTransform rowBarRect = rowBarGO.AddComponent<RectTransform>();
                rowBarRect.anchorMin = new Vector2(0f, 0f);
                rowBarRect.anchorMax = new Vector2(1f, 0f);
                rowBarRect.pivot = new Vector2(0.5f, 0f);
                rowBarRect.anchoredPosition = new Vector2(0, 26);
                rowBarRect.sizeDelta = new Vector2(-56, 10);

                Slider rowSlider = rowBarGO.AddComponent<Slider>();
                rowSlider.interactable = false;

                GameObject rowBgGO = new GameObject("Background");
                rowBgGO.transform.SetParent(rowBarGO.transform, false);
                RectTransform rowBgRect = rowBgGO.AddComponent<RectTransform>();
                rowBgRect.anchorMin = Vector2.zero;
                rowBgRect.anchorMax = Vector2.one;
                rowBgRect.sizeDelta = Vector2.zero;
                RoundedRectGraphic rowBgG = rowBgGO.AddComponent<RoundedRectGraphic>();
                rowBgG.IsCapsule = true;
                rowBgG.color = new Color(1f, 1f, 1f, 0.10f);

                GameObject rowFillAreaGO = new GameObject("Fill Area");
                rowFillAreaGO.transform.SetParent(rowBarGO.transform, false);
                RectTransform rowFillAreaRect = rowFillAreaGO.AddComponent<RectTransform>();
                rowFillAreaRect.anchorMin = Vector2.zero;
                rowFillAreaRect.anchorMax = Vector2.one;
                rowFillAreaRect.sizeDelta = Vector2.zero;

                GameObject rowFillGO = new GameObject("Fill");
                rowFillGO.transform.SetParent(rowFillAreaGO.transform, false);
                RectTransform rowFillRect = rowFillGO.AddComponent<RectTransform>();
                rowFillRect.anchorMin = Vector2.zero;
                rowFillRect.anchorMax = Vector2.one;
                rowFillRect.sizeDelta = Vector2.zero;
                RoundedRectGraphic rowFillG = rowFillGO.AddComponent<RoundedRectGraphic>();
                rowFillG.IsCapsule = true;
                rowFillG.color = Gold;
                rowSlider.fillRect = rowFillRect;

                MissionRowView rowView = rowGO.AddComponent<MissionRowView>();
                SerializedObject rowSO = new SerializedObject(rowView);
                rowSO.FindProperty("backgroundImage").objectReferenceValue = rowImg;
                rowSO.FindProperty("titleText").objectReferenceValue = rowTitleTMP;
                rowSO.FindProperty("progressStatusText").objectReferenceValue = rowStatusTMP;
                rowSO.FindProperty("progressBar").objectReferenceValue = rowSlider;
                rowSO.FindProperty("progressFillImage").objectReferenceValue = rowFillG;
                rowSO.ApplyModifiedProperties();

                rowViews.Add(rowView);
            }

            // Configure MissionsModalController Serialized Properties
            SerializedObject modalSO = new SerializedObject(modalCtrl);
            modalSO.FindProperty("modalRoot").objectReferenceValue = modalRootGO;
            modalSO.FindProperty("modalCanvasGroup").objectReferenceValue = modalCG;
            modalSO.FindProperty("modalBoxRect").objectReferenceValue = modalBoxRect;
            modalSO.FindProperty("backdropCloseButton").objectReferenceValue = backdropBtn;
            modalSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
            modalSO.FindProperty("blurBackdropImage").objectReferenceValue = blurImg;
            modalSO.FindProperty("titleText").objectReferenceValue = modalTitleTMP;
            modalSO.FindProperty("completedCountText").objectReferenceValue = compCountTMP;
            modalSO.FindProperty("resetTimerText").objectReferenceValue = timerTMP;
            modalSO.FindProperty("milestoneSlider").objectReferenceValue = mSlider;
            modalSO.FindProperty("milestone1Dot").objectReferenceValue = dot1Img;
            modalSO.FindProperty("milestone2Dot").objectReferenceValue = dot2Img;
            modalSO.FindProperty("milestone1GiftBox").objectReferenceValue = gift1Img;
            modalSO.FindProperty("milestone2GiftBox").objectReferenceValue = gift2Img;
            modalSO.FindProperty("cardNormalSprite").objectReferenceValue = missionCardSprite;
            modalSO.FindProperty("cardDoneSprite").objectReferenceValue = missionCardDoneSprite;

            SerializedProperty rowsProp = modalSO.FindProperty("missionRows");
            rowsProp.arraySize = rowViews.Count;
            for (int i = 0; i < rowViews.Count; i++)
            {
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowViews[i];
            }
            modalSO.ApplyModifiedProperties();

            // Set initial state: Hidden by default
            modalRootGO.SetActive(false);

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
            so.FindProperty("missionsModal").objectReferenceValue = modalCtrl;

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];

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
