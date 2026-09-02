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
    public static class ProfileSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/ProfileScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";
        private const string FontPath = "Assets/_Project/Art/Fonts";

        // Exact Design Tokens from docs/Pantallas/src/App.tsx
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);          // #e8a820
        private static readonly Color GoldBorder = new Color(0.831f, 0.588f, 0.055f);    // #d4960e
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);        // #0d1a13
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color NavBg = new Color(0.055f, 0.125f, 0.086f, 0.85f);

        // Rarity Palette
        private static readonly Color ColorComun = new Color(0.153f, 0.788f, 0.416f);       // #27c96a
        private static readonly Color ColorPocoComun = new Color(0.706f, 0.784f, 0.765f);   // #b4c8c3
        private static readonly Color ColorRara = new Color(0.608f, 0.361f, 0.965f);        // #9b5cf6
        private static readonly Color ColorMitica = new Color(0.910f, 0.659f, 0.125f);      // #e8a820

        private struct FormationSlotDef
        {
            public string id;
            public string pos;
            public string rarity;
            public float xPct;
            public float yPct;
            public Color rarityColor;

            public FormationSlotDef(string id, string pos, string r, float x, float y, Color col)
            {
                this.id = id;
                this.pos = pos;
                this.rarity = r;
                this.xPct = x;
                this.yPct = y;
                this.rarityColor = col;
            }
        }

        private struct FeaturedSlotDef
        {
            public string name;
            public string initials;
            public string rarity;
            public Color rarityColor;

            public FeaturedSlotDef(string n, string ini, string r, Color col)
            {
                name = n;
                initials = ini;
                rarity = r;
                rarityColor = col;
            }
        }

        [MenuItem("JuegoTCG/Generar Pantalla de Perfil")]
        public static void BuildProfileScene()
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
            Sprite stadiumLinesSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_tactical_pitch_lines.png");
            Sprite iconGear = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_gear.png");
            Sprite iconEdit = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_edit.png");
            Sprite iconCopy = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_copy.png");
            Sprite iconProfile = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_profile.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_icon_user.png");

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
            GameObject controllerGO = new GameObject("ProfileScreenController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            ProfileScreenController controller = controllerGO.AddComponent<ProfileScreenController>();

            // Content Container
            GameObject contentGO = new GameObject("ContentContainer");
            contentGO.transform.SetParent(canvasGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            // Scrollable Profile View
            GameObject scrollGO = new GameObject("ProfileScrollView");
            scrollGO.transform.SetParent(contentGO.transform, false);
            RectTransform sRect = scrollGO.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0f);
            sRect.anchorMax = new Vector2(0.5f, 1f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.offsetMin = new Vector2(-490, 160);
            sRect.offsetMax = new Vector2(490, -40);

            ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 35f;

            // Viewport
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRect;

            // Content Container (Vertical Layout)
            GameObject scrollContentGO = new GameObject("Content");
            scrollContentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform scRect = scrollContentGO.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0f, 1f);
            scRect.anchorMax = new Vector2(1f, 1f);
            scRect.pivot = new Vector2(0.5f, 1f);
            scRect.anchoredPosition = Vector2.zero;
            scRect.sizeDelta = new Vector2(0, 1850);

            VerticalLayoutGroup vlg = scrollContentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 35f;
            vlg.padding = new RectOffset(0, 0, 10, 80);

            scrollRect.content = scRect;

            // ====================================================
            // 1. USER PROFILE HEADER (Settings + Avatar + Name + Friend Code)
            // ====================================================
            GameObject headerSectionGO = new GameObject("HeaderSection");
            headerSectionGO.transform.SetParent(scrollContentGO.transform, false);
            RectTransform hsRect = headerSectionGO.AddComponent<RectTransform>();
            hsRect.sizeDelta = new Vector2(980, 360);

            // Settings Button (Top Right)
            GameObject settingsBtnGO = new GameObject("SettingsButton");
            settingsBtnGO.transform.SetParent(headerSectionGO.transform, false);
            RectTransform sbRect = settingsBtnGO.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 1f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 1f);
            sbRect.anchoredPosition = new Vector2(-10, -10);
            sbRect.sizeDelta = new Vector2(48, 48);
            if (iconGear != null)
            {
                Image sbImg = settingsBtnGO.AddComponent<Image>();
                sbImg.sprite = iconGear;
                sbImg.color = new Color(1f, 1f, 1f, 0.50f);
            }
            Button settingsBtn = settingsBtnGO.AddComponent<Button>();

            // Avatar with Gold Border + Edit Badge
            GameObject avatarGO = new GameObject("Avatar");
            avatarGO.transform.SetParent(headerSectionGO.transform, false);
            RectTransform avRect = avatarGO.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0.5f, 1f);
            avRect.anchorMax = new Vector2(0.5f, 1f);
            avRect.pivot = new Vector2(0.5f, 1f);
            avRect.anchoredPosition = new Vector2(0, -20);
            avRect.sizeDelta = new Vector2(170, 170);

            RoundedRectGraphic avG = avatarGO.AddComponent<RoundedRectGraphic>();
            avG.IsCapsule = true;
            avG.color = new Color(1f, 1f, 1f, 0.07f);
            avG.BorderWidth = 2.5f;
            avG.BorderColor = GoldBorder;

            GameObject avIconGO = new GameObject("Icon");
            avIconGO.transform.SetParent(avatarGO.transform, false);
            RectTransform aviRect = avIconGO.AddComponent<RectTransform>();
            aviRect.anchorMin = new Vector2(0.5f, 0.5f);
            aviRect.anchorMax = new Vector2(0.5f, 0.5f);
            aviRect.sizeDelta = new Vector2(90, 90);
            if (iconUser != null)
            {
                Image aviImg = avIconGO.AddComponent<Image>();
                aviImg.sprite = iconUser;
                aviImg.color = new Color(1f, 1f, 1f, 0.45f);
            }

            // Edit Badge on Avatar
            GameObject editBadgeGO = new GameObject("EditBadge");
            editBadgeGO.transform.SetParent(avatarGO.transform, false);
            RectTransform ebRect = editBadgeGO.AddComponent<RectTransform>();
            ebRect.anchorMin = new Vector2(1f, 0f);
            ebRect.anchorMax = new Vector2(1f, 0f);
            ebRect.pivot = new Vector2(1f, 0f);
            ebRect.anchoredPosition = new Vector2(4, -4);
            ebRect.sizeDelta = new Vector2(50, 50);

            RoundedRectGraphic ebG = editBadgeGO.AddComponent<RoundedRectGraphic>();
            ebG.IsCapsule = true;
            ebG.color = CardBg;
            ebG.BorderWidth = 2f;
            ebG.BorderColor = GoldBorder;

            GameObject ebIconGO = new GameObject("PencilIcon");
            ebIconGO.transform.SetParent(editBadgeGO.transform, false);
            RectTransform ebiRect = ebIconGO.AddComponent<RectTransform>();
            ebiRect.anchorMin = new Vector2(0.5f, 0.5f);
            ebiRect.anchorMax = new Vector2(0.5f, 0.5f);
            ebiRect.sizeDelta = new Vector2(26, 26);
            if (iconEdit != null)
            {
                Image ebiImg = ebIconGO.AddComponent<Image>();
                ebiImg.sprite = iconEdit;
                ebiImg.color = Gold;
            }

            // Username + Edit
            GameObject userRowGO = new GameObject("UsernameRow");
            userRowGO.transform.SetParent(headerSectionGO.transform, false);
            RectTransform urRect = userRowGO.AddComponent<RectTransform>();
            urRect.anchorMin = new Vector2(0.5f, 1f);
            urRect.anchorMax = new Vector2(0.5f, 1f);
            urRect.pivot = new Vector2(0.5f, 1f);
            urRect.anchoredPosition = new Vector2(0, -210);
            urRect.sizeDelta = new Vector2(500, 50);

            GameObject userNameGO = new GameObject("Username");
            userNameGO.transform.SetParent(userRowGO.transform, false);
            RectTransform unRect = userNameGO.AddComponent<RectTransform>();
            unRect.anchorMin = Vector2.zero;
            unRect.anchorMax = Vector2.one;
            unRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI unTMP = userNameGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) unTMP.font = dmSansTMPFont;
            unTMP.text = "JUGADOR_01";
            unTMP.fontSize = 32;
            unTMP.fontStyle = FontStyles.Bold;
            unTMP.alignment = TextAlignmentOptions.Center;
            unTMP.color = TextWhite;

            // Friend Code + Copy
            GameObject codeBtnGO = new GameObject("FriendCodeButton");
            codeBtnGO.transform.SetParent(headerSectionGO.transform, false);
            RectTransform cbRect = codeBtnGO.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 1f);
            cbRect.anchorMax = new Vector2(0.5f, 1f);
            cbRect.pivot = new Vector2(0.5f, 1f);
            cbRect.anchoredPosition = new Vector2(0, -270);
            cbRect.sizeDelta = new Vector2(550, 48);

            GameObject codeTextGO = new GameObject("Text");
            codeTextGO.transform.SetParent(codeBtnGO.transform, false);
            RectTransform ctRect = codeTextGO.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI ctTMP = codeTextGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) ctTMP.font = dmSansTMPFont;
            ctTMP.text = "Código de amigo: <color=#b0c0b8>4872-1093</color>";
            ctTMP.fontSize = 22;
            ctTMP.alignment = TextAlignmentOptions.Center;
            ctTMP.color = TextGray;

            Button copyCodeBtn = codeBtnGO.AddComponent<Button>();

            // ====================================================
            // 2. MI 11 IDEAL (Tactical Pitch with 4-3-3 Formation)
            // ====================================================
            GameObject formationSectionGO = new GameObject("FormationSection");
            formationSectionGO.transform.SetParent(scrollContentGO.transform, false);
            RectTransform formRect = formationSectionGO.AddComponent<RectTransform>();
            formRect.sizeDelta = new Vector2(980, 780);

            // Header: Title Left (Barlow) + Counter Right (DM Sans)
            GameObject formTitleRowGO = new GameObject("TitleRow");
            formTitleRowGO.transform.SetParent(formationSectionGO.transform, false);
            RectTransform ftrRect = formTitleRowGO.AddComponent<RectTransform>();
            ftrRect.anchorMin = new Vector2(0f, 1f);
            ftrRect.anchorMax = new Vector2(1f, 1f);
            ftrRect.pivot = new Vector2(0.5f, 1f);
            ftrRect.anchoredPosition = new Vector2(0, 0);
            ftrRect.sizeDelta = new Vector2(0, 45);

            GameObject fTitleGO = new GameObject("Title");
            fTitleGO.transform.SetParent(formTitleRowGO.transform, false);
            RectTransform ftRect = fTitleGO.AddComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0f, 0f);
            ftRect.anchorMax = new Vector2(0.6f, 1f);
            ftRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI ftTMP = fTitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) ftTMP.font = barlowTMPFont;
            ftTMP.text = "MI 11 IDEAL";
            ftTMP.fontSize = 32;
            ftTMP.fontStyle = FontStyles.Bold;
            ftTMP.characterSpacing = 8f;
            ftTMP.color = TextWhite;

            GameObject fCounterGO = new GameObject("Counter");
            fCounterGO.transform.SetParent(formTitleRowGO.transform, false);
            RectTransform fcRect = fCounterGO.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0.6f, 0f);
            fcRect.anchorMax = new Vector2(1f, 1f);
            fcRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI fcTMP = fCounterGO.AddComponent<TextMeshProUGUI>();
            if (dmSansTMPFont != null) fcTMP.font = dmSansTMPFont;
            fcTMP.text = "5 / 11 espacios";
            fcTMP.fontSize = 22;
            fcTMP.alignment = TextAlignmentOptions.Right;
            fcTMP.color = TextGray;

            // Pitch Container
            GameObject pitchBoxGO = new GameObject("PitchContainer");
            pitchBoxGO.transform.SetParent(formationSectionGO.transform, false);
            RectTransform pbRect = pitchBoxGO.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0f);
            pbRect.anchorMax = new Vector2(0.5f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.anchoredPosition = new Vector2(0, 0);
            pbRect.sizeDelta = new Vector2(980, 720);

            RoundedRectGraphic pbG = pitchBoxGO.AddComponent<RoundedRectGraphic>();
            pbG.CornerRadius = 24f;
            pbG.color = new Color(0.031f, 0.071f, 0.047f, 0.85f);
            pbG.BorderWidth = 1.5f;
            pbG.BorderColor = BorderSubtle;

            // Pitch Line Markings
            GameObject pitchLinesGO = new GameObject("PitchLines");
            pitchLinesGO.transform.SetParent(pitchBoxGO.transform, false);
            RectTransform plRect = pitchLinesGO.AddComponent<RectTransform>();
            plRect.anchorMin = Vector2.zero;
            plRect.anchorMax = Vector2.one;
            plRect.sizeDelta = Vector2.zero;
            if (stadiumLinesSprite != null)
            {
                Image plImg = pitchLinesGO.AddComponent<Image>();
                plImg.sprite = stadiumLinesSprite;
                plImg.color = new Color(1f, 1f, 1f, 0.08f);
            }

            // 11 Player Slots in 4-3-3 Formation
            FormationSlotDef[] formationSlots = new FormationSlotDef[]
            {
                // FWD (3)
                new FormationSlotDef("f1", "DEL", "Rara", 0.22f, 0.11f, ColorRara),
                new FormationSlotDef("f2", "DEL", null, 0.50f, 0.11f, BorderSubtle),
                new FormationSlotDef("f3", "DEL", null, 0.78f, 0.11f, BorderSubtle),
                // MID (3)
                new FormationSlotDef("m1", "MED", "Mítica", 0.18f, 0.35f, ColorMitica),
                new FormationSlotDef("m2", "MED", null, 0.50f, 0.35f, BorderSubtle),
                new FormationSlotDef("m3", "MED", "Común", 0.82f, 0.35f, ColorComun),
                // DEF (4)
                new FormationSlotDef("d1", "DEF", "Poco común", 0.11f, 0.62f, ColorPocoComun),
                new FormationSlotDef("d2", "DEF", null, 0.36f, 0.62f, BorderSubtle),
                new FormationSlotDef("d3", "DEF", null, 0.64f, 0.62f, BorderSubtle),
                new FormationSlotDef("d4", "DEF", "Rara", 0.89f, 0.62f, ColorRara),
                // GK (1)
                new FormationSlotDef("g1", "POR", null, 0.50f, 0.86f, BorderSubtle),
            };

            for (int i = 0; i < formationSlots.Length; i++)
            {
                FormationSlotDef slot = formationSlots[i];
                bool isEmpty = (slot.rarity == null);

                GameObject slotGO = new GameObject($"Slot_{slot.id}_{slot.pos}");
                slotGO.transform.SetParent(pitchBoxGO.transform, false);
                RectTransform slotRect = slotGO.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(slot.xPct, 1f - slot.yPct);
                slotRect.anchorMax = new Vector2(slot.xPct, 1f - slot.yPct);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = Vector2.zero;
                slotRect.sizeDelta = new Vector2(115, 155);

                RoundedRectGraphic slotG = slotGO.AddComponent<RoundedRectGraphic>();
                slotG.CornerRadius = 14f;
                slotG.color = CardBg;
                slotG.BorderWidth = 1.8f;
                slotG.BorderColor = isEmpty ? BorderSubtle : slot.rarityColor;

                CanvasGroup cg = slotGO.AddComponent<CanvasGroup>();
                cg.alpha = isEmpty ? 0.45f : 1f;

                // Position Chip (Top Left)
                GameObject posChipGO = new GameObject("PosChip");
                posChipGO.transform.SetParent(slotGO.transform, false);
                RectTransform posRect = posChipGO.AddComponent<RectTransform>();
                posRect.anchorMin = new Vector2(0f, 1f);
                posRect.anchorMax = new Vector2(0f, 1f);
                posRect.pivot = new Vector2(0f, 1f);
                posRect.anchoredPosition = new Vector2(8, -8);
                posRect.sizeDelta = new Vector2(50, 26);

                RoundedRectGraphic posG = posChipGO.AddComponent<RoundedRectGraphic>();
                posG.IsCapsule = true;
                posG.color = new Color(0.024f, 0.055f, 0.039f, 0.90f);
                posG.BorderWidth = 1.2f;
                posG.BorderColor = isEmpty ? BorderSubtle : slot.rarityColor;

                GameObject posTextGO = new GameObject("Text");
                posTextGO.transform.SetParent(posChipGO.transform, false);
                RectTransform ptRect = posTextGO.AddComponent<RectTransform>();
                ptRect.anchorMin = Vector2.zero;
                ptRect.anchorMax = Vector2.one;
                ptRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI ptTMP = posTextGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) ptTMP.font = dmSansTMPFont;
                ptTMP.text = slot.pos;
                ptTMP.fontSize = 15;
                ptTMP.fontStyle = FontStyles.Bold;
                ptTMP.alignment = TextAlignmentOptions.Center;
                ptTMP.color = isEmpty ? TextDim : slot.rarityColor;

                // Inner Dot Indicator when occupied
                if (!isEmpty)
                {
                    GameObject dotGO = new GameObject("IndicatorDot");
                    dotGO.transform.SetParent(slotGO.transform, false);
                    RectTransform dotRect = dotGO.AddComponent<RectTransform>();
                    dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                    dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                    dotRect.anchoredPosition = new Vector2(0, -10);
                    dotRect.sizeDelta = new Vector2(36, 36);

                    RoundedRectGraphic dotG = dotGO.AddComponent<RoundedRectGraphic>();
                    dotG.IsCapsule = true;
                    dotG.color = new Color(1f, 1f, 1f, 0.07f);
                    dotG.BorderWidth = 1.5f;
                    dotG.BorderColor = slot.rarityColor;
                }
            }

            // ====================================================
            // 3. CARTAS DESTACADAS (Featured Showcase Cards)
            // ====================================================
            GameObject featuredSectionGO = new GameObject("FeaturedSection");
            featuredSectionGO.transform.SetParent(scrollContentGO.transform, false);
            RectTransform featRect = featuredSectionGO.AddComponent<RectTransform>();
            featRect.sizeDelta = new Vector2(980, 480);

            GameObject featTitleGO = new GameObject("Title");
            featTitleGO.transform.SetParent(featuredSectionGO.transform, false);
            RectTransform fttRect = featTitleGO.AddComponent<RectTransform>();
            fttRect.anchorMin = new Vector2(0f, 1f);
            fttRect.anchorMax = new Vector2(1f, 1f);
            fttRect.pivot = new Vector2(0.5f, 1f);
            fttRect.anchoredPosition = new Vector2(0, 0);
            fttRect.sizeDelta = new Vector2(0, 45);
            TextMeshProUGUI fttTMP = featTitleGO.AddComponent<TextMeshProUGUI>();
            if (barlowTMPFont != null) fttTMP.font = barlowTMPFont;
            fttTMP.text = "CARTAS DESTACADAS";
            fttTMP.fontSize = 32;
            fttTMP.fontStyle = FontStyles.Bold;
            fttTMP.characterSpacing = 8f;
            fttTMP.color = TextWhite;

            GameObject featRowGO = new GameObject("CardsRow");
            featRowGO.transform.SetParent(featuredSectionGO.transform, false);
            RectTransform frRect = featRowGO.AddComponent<RectTransform>();
            frRect.anchorMin = new Vector2(0.5f, 0f);
            frRect.anchorMax = new Vector2(0.5f, 0f);
            frRect.pivot = new Vector2(0.5f, 0f);
            frRect.anchoredPosition = new Vector2(0, 0);
            frRect.sizeDelta = new Vector2(980, 410);

            HorizontalLayoutGroup frHlg = featRowGO.AddComponent<HorizontalLayoutGroup>();
            frHlg.childAlignment = TextAnchor.MiddleCenter;
            frHlg.childControlWidth = false;
            frHlg.childControlHeight = false;
            frHlg.childForceExpandWidth = false;
            frHlg.childForceExpandHeight = false;
            frHlg.spacing = 30f;

            FeaturedSlotDef[] featuredCards = new FeaturedSlotDef[]
            {
                new FeaturedSlotDef("Luis Díaz", "LD", "Mítica", ColorMitica),
                new FeaturedSlotDef("Bellingham", "JB", "Rara", ColorRara),
                new FeaturedSlotDef("Vacío", null, null, BorderSubtle)
            };

            for (int i = 0; i < featuredCards.Length; i++)
            {
                FeaturedSlotDef featCard = featuredCards[i];
                bool isEmpty = (featCard.rarity == null);

                GameObject fCardGO = new GameObject($"FeaturedCard_{i + 1}_{featCard.name}");
                fCardGO.transform.SetParent(featRowGO.transform, false);
                RectTransform fCardRect = fCardGO.AddComponent<RectTransform>();
                fCardRect.sizeDelta = new Vector2(295, 390); // 3:4 aspect ratio

                RoundedRectGraphic fcG = fCardGO.AddComponent<RoundedRectGraphic>();
                fcG.CornerRadius = 18f;
                fcG.color = CardBg;
                fcG.BorderWidth = 2f;
                fcG.BorderColor = isEmpty ? BorderSubtle : featCard.rarityColor;

                // Avatar Circle with initials
                GameObject fAvatarGO = new GameObject("AvatarRing");
                fAvatarGO.transform.SetParent(fCardGO.transform, false);
                RectTransform favRect = fAvatarGO.AddComponent<RectTransform>();
                favRect.anchorMin = new Vector2(0.5f, 1f);
                favRect.anchorMax = new Vector2(0.5f, 1f);
                favRect.pivot = new Vector2(0.5f, 1f);
                favRect.anchoredPosition = new Vector2(0, -40);
                favRect.sizeDelta = new Vector2(100, 100);

                RoundedRectGraphic favG = fAvatarGO.AddComponent<RoundedRectGraphic>();
                favG.IsCapsule = true;
                favG.color = new Color(1f, 1f, 1f, 0.05f);
                favG.BorderWidth = 1.8f;
                favG.BorderColor = isEmpty ? BorderSubtle : featCard.rarityColor;

                if (!isEmpty)
                {
                    GameObject favTextGO = new GameObject("Initials");
                    favTextGO.transform.SetParent(fAvatarGO.transform, false);
                    RectTransform fatRect = favTextGO.AddComponent<RectTransform>();
                    fatRect.anchorMin = Vector2.zero;
                    fatRect.anchorMax = Vector2.one;
                    fatRect.sizeDelta = Vector2.zero;
                    TextMeshProUGUI fatTMP = favTextGO.AddComponent<TextMeshProUGUI>();
                    if (dmSansTMPFont != null) fatTMP.font = dmSansTMPFont;
                    fatTMP.text = featCard.initials;
                    fatTMP.fontSize = 32;
                    fatTMP.fontStyle = FontStyles.Bold;
                    fatTMP.alignment = TextAlignmentOptions.Center;
                    fatTMP.color = featCard.rarityColor;
                }

                // Player Name
                GameObject fnGO = new GameObject("Name");
                fnGO.transform.SetParent(fCardGO.transform, false);
                RectTransform fnRect = fnGO.AddComponent<RectTransform>();
                fnRect.anchorMin = new Vector2(0.05f, 0.08f);
                fnRect.anchorMax = new Vector2(0.95f, 0.30f);
                fnRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI fnTMP = fnGO.AddComponent<TextMeshProUGUI>();
                if (dmSansTMPFont != null) fnTMP.font = dmSansTMPFont;
                fnTMP.text = featCard.name;
                fnTMP.fontSize = 24;
                fnTMP.fontStyle = isEmpty ? FontStyles.Normal : FontStyles.Bold;
                fnTMP.alignment = TextAlignmentOptions.Center;
                fnTMP.color = isEmpty ? TextDim : TextWhite;
            }

            // ====================================================
            // 4. LIQUID-GLASS BOTTOM NAVIGATION BAR (Tab Perfil Active)
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
                bool isTabActive = (i == 4); // "Perfil" is Active
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
            so.FindProperty("usernameText").objectReferenceValue = unTMP;
            so.FindProperty("friendCodeText").objectReferenceValue = ctTMP;
            so.FindProperty("copyCodeButton").objectReferenceValue = copyCodeBtn;
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            so.FindProperty("formationCounterText").objectReferenceValue = fcTMP;

            so.FindProperty("tabInicioButton").objectReferenceValue = tabBtns[0];
            so.FindProperty("tabCartasButton").objectReferenceValue = tabBtns[1];
            so.FindProperty("tabTiendaButton").objectReferenceValue = tabBtns[2];
            so.FindProperty("tabComunidadButton").objectReferenceValue = tabBtns[3];
            so.FindProperty("tabPerfilButton").objectReferenceValue = tabBtns[4];

            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabDir = "Assets/_Project/Prefabs/UI";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/ProfileUI.prefab";
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

            Debug.Log("<color=green>[JuegoTCG] ¡Pantalla de Perfil generada con 5 Pestañas (ProfileScene & ProfileUI.prefab)!</color>");
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
