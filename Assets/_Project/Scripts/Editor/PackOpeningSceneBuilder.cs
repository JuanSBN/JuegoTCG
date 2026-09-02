#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;
using JuegoTCG.Packs;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class PackOpeningSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/PackOpeningScene.unity";
        private const string UIPath = "Assets/_Project/Art/UI";

        public static void BuildPackOpeningScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("JuegoTCG", "Por favor sal del modo Play (detén la ejecución) antes de generar la escena.", "Entendido");
                return;
            }

            // 1. Ensure all procedural UI sprites are generated and up-to-date
            ProceduralAssetGenerator.GenerateUISprites();
            AssetDatabase.Refresh();

            // 2. Rebuild CardPrefab
            CardPrefabBuilder.BuildCardPrefab();

            // Load UI Sprites
            Sprite pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_pill.png");
            Sprite pillBorderedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_pill_bordered.png");
            Sprite roundedPackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_rounded_pack.png");
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_star.png");
            Sprite raysSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/ui_rays.png");
            Sprite stadiumBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_stadium.png");
            Sprite stadiumLinesSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIPath}/bg_stadium_lines.png");

            // 3. Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.070f, 0.125f); // Deep pitch blue #0B1220
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

            // Global UI Particle Burst System (Topmost layer for sparkly reveals)
            GameObject particleSystemGO = new GameObject("UIParticleSystem");
            particleSystemGO.transform.SetParent(canvasGO.transform, false);
            RectTransform particleRect = particleSystemGO.AddComponent<RectTransform>();
            particleRect.anchorMin = Vector2.zero;
            particleRect.anchorMax = Vector2.one;
            particleRect.sizeDelta = Vector2.zero;
            particleSystemGO.AddComponent<UIParticleBurst>();

            // Event System
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Screen Container (Root for shaking effects)
            GameObject screenGO = new GameObject("ScreenContainer");
            screenGO.transform.SetParent(canvasGO.transform, false);
            RectTransform screenRect = screenGO.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.sizeDelta = Vector2.zero;

            // 1. Stadium Background (Atmospheric Dual-Orb Gradient like HTML prototype)
            GameObject bgGO = new GameObject("BackgroundImage");
            bgGO.transform.SetParent(screenGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGO.AddComponent<Image>();
            if (stadiumBgSprite != null) bgImg.sprite = stadiumBgSprite;
            bgImg.color = Color.white;

            // 2. Goalpost Stadium Lines (Subtle pitch motif from HTML)
            GameObject stadiumLinesGO = new GameObject("StadiumLines");
            stadiumLinesGO.transform.SetParent(screenGO.transform, false);
            RectTransform linesRect = stadiumLinesGO.AddComponent<RectTransform>();
            linesRect.anchorMin = Vector2.zero;
            linesRect.anchorMax = Vector2.one;
            linesRect.sizeDelta = Vector2.zero;
            Image linesImg = stadiumLinesGO.AddComponent<Image>();
            if (stadiumLinesSprite != null) linesImg.sprite = stadiumLinesSprite;
            linesImg.color = Color.white;
            linesImg.raycastTarget = false;

            // PackOpener Controller GO
            GameObject openerGO = new GameObject("PackOpenerController");
            openerGO.transform.SetParent(canvasGO.transform, false);
            PackOpener opener = openerGO.AddComponent<PackOpener>();

            // Top Bar
            GameObject topBarGO = new GameObject("TopBar");
            topBarGO.transform.SetParent(screenGO.transform, false);
            RectTransform topBarRect = topBarGO.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0.5f, 1f);
            topBarRect.anchorMax = new Vector2(0.5f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0, -35);
            topBarRect.sizeDelta = new Vector2(980, 80);

            // Force Holo Toggle (Pruebas)
            GameObject toggleGO = new GameObject("ForceHoloToggle");
            toggleGO.transform.SetParent(topBarGO.transform, false);
            RectTransform toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0f, 0.5f);
            toggleRect.anchorMax = new Vector2(0f, 0.5f);
            toggleRect.pivot = new Vector2(0f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(0, 0);
            toggleRect.sizeDelta = new Vector2(360, 60);
            Toggle toggle = toggleGO.AddComponent<Toggle>();
            toggle.isOn = true;

            GameObject toggleLabelGO = new GameObject("Label");
            toggleLabelGO.transform.SetParent(toggleGO.transform, false);
            RectTransform toggleLabelRect = toggleLabelGO.AddComponent<RectTransform>();
            toggleLabelRect.anchorMin = Vector2.zero;
            toggleLabelRect.anchorMax = Vector2.one;
            toggleLabelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI toggleTMP = toggleLabelGO.AddComponent<TextMeshProUGUI>();
            toggleTMP.text = "Forzar Holo (Prueba)";
            toggleTMP.fontSize = 20;
            toggleTMP.color = new Color(0.96f, 0.65f, 0.14f);

            // Pack Counter Pill Badge (Mathematical Vector Capsule)
            GameObject counterBadgeGO = new GameObject("PackCounterBadge");
            counterBadgeGO.transform.SetParent(topBarGO.transform, false);
            RectTransform counterBadgeRect = counterBadgeGO.AddComponent<RectTransform>();
            counterBadgeRect.anchorMin = new Vector2(1f, 0.5f);
            counterBadgeRect.anchorMax = new Vector2(1f, 0.5f);
            counterBadgeRect.pivot = new Vector2(1f, 0.5f);
            counterBadgeRect.anchoredPosition = new Vector2(0, 0);
            counterBadgeRect.sizeDelta = new Vector2(170, 44);

            RoundedRectGraphic badgeG = counterBadgeGO.AddComponent<RoundedRectGraphic>();
            badgeG.IsCapsule = true;
            badgeG.color = new Color(0.961f, 0.651f, 0.137f, 0.12f);
            badgeG.BorderWidth = 1.2f;
            badgeG.BorderColor = new Color(0.961f, 0.651f, 0.137f, 0.35f);

            // Auto-layout inside badge
            HorizontalLayoutGroup badgeLayout = counterBadgeGO.AddComponent<HorizontalLayoutGroup>();
            badgeLayout.childAlignment = TextAnchor.MiddleCenter;
            badgeLayout.spacing = 8f;
            badgeLayout.padding = new RectOffset(16, 16, 4, 4);
            badgeLayout.childControlWidth = false;
            badgeLayout.childControlHeight = false;

            // Star Icon inside badge
            GameObject badgeStarGO = new GameObject("BadgeStar");
            badgeStarGO.transform.SetParent(counterBadgeGO.transform, false);
            RectTransform badgeStarRect = badgeStarGO.AddComponent<RectTransform>();
            badgeStarRect.sizeDelta = new Vector2(18, 18);
            Image badgeStarImg = badgeStarGO.AddComponent<Image>();
            badgeStarImg.sprite = starSprite;
            badgeStarImg.color = new Color(1f, 0.85f, 0.45f);

            // Text inside badge
            GameObject counterGO = new GameObject("PackCounterText");
            counterGO.transform.SetParent(counterBadgeGO.transform, false);
            RectTransform counterRect = counterGO.AddComponent<RectTransform>();
            counterRect.sizeDelta = new Vector2(95, 28);
            TextMeshProUGUI counterTMP = counterGO.AddComponent<TextMeshProUGUI>();
            counterTMP.text = "5 sobres";
            counterTMP.fontSize = 19;
            counterTMP.fontStyle = FontStyles.Bold;
            counterTMP.alignment = TextAlignmentOptions.MidlineLeft;
            counterTMP.color = new Color(1f, 0.88f, 0.55f);

            // ----------------------------------------------------
            // 1. Closed Pack View
            // ----------------------------------------------------
            GameObject closedViewGO = new GameObject("ClosedPackView");
            closedViewGO.transform.SetParent(screenGO.transform, false);
            RectTransform closedRect = closedViewGO.AddComponent<RectTransform>();
            closedRect.anchorMin = Vector2.zero;
            closedRect.anchorMax = Vector2.one;
            closedRect.sizeDelta = Vector2.zero;

            // Pack Outer Border (Gold Glow Rounded Rect)
            GameObject packBorderGO = new GameObject("PackGraphic");
            packBorderGO.transform.SetParent(closedViewGO.transform, false);
            RectTransform packRect = packBorderGO.AddComponent<RectTransform>();
            packRect.anchorMin = new Vector2(0.5f, 0.5f);
            packRect.anchorMax = new Vector2(0.5f, 0.5f);
            packRect.pivot = new Vector2(0.5f, 0.5f);
            packRect.anchoredPosition = new Vector2(0, 60);
            packRect.sizeDelta = new Vector2(420, 600);
            Image packBorderImg = packBorderGO.AddComponent<Image>();
            packBorderImg.sprite = roundedPackSprite;
            packBorderImg.type = Image.Type.Sliced;
            packBorderImg.color = new Color(0.96f, 0.65f, 0.14f, 0.55f);

            // Pack Inner Body (Dark Blue Gradient with Mask so rays never overflow!)
            GameObject packInnerGO = new GameObject("PackInner");
            packInnerGO.transform.SetParent(packBorderGO.transform, false);
            RectTransform packInnerRect = packInnerGO.AddComponent<RectTransform>();
            packInnerRect.anchorMin = Vector2.zero;
            packInnerRect.anchorMax = Vector2.one;
            packInnerRect.offsetMin = new Vector2(6, 6);
            packInnerRect.offsetMax = new Vector2(-6, -6);
            Image packInnerImg = packInnerGO.AddComponent<Image>();
            packInnerImg.sprite = roundedPackSprite;
            packInnerImg.type = Image.Type.Sliced;
            packInnerImg.color = new Color(0.086f, 0.137f, 0.231f); // #16233B

            // Mask on inner body to clip rays inside the rounded card!
            Mask packMask = packInnerGO.AddComponent<Mask>();
            packMask.showMaskGraphic = true;

            // Pack Rotating Conic Rays (Masked inside the pack!)
            GameObject raysGO = new GameObject("PackRays");
            raysGO.transform.SetParent(packInnerGO.transform, false);
            RectTransform raysRect = raysGO.AddComponent<RectTransform>();
            raysRect.anchorMin = Vector2.zero;
            raysRect.anchorMax = Vector2.one;
            raysRect.sizeDelta = Vector2.zero;
            Image raysImg = raysGO.AddComponent<Image>();
            raysImg.sprite = raysSprite;
            raysImg.color = Color.white;
            raysImg.raycastTarget = false;

            // Pack Gold Star in Center
            GameObject starGO = new GameObject("PackCenterStar");
            starGO.transform.SetParent(packInnerGO.transform, false);
            RectTransform starRect = starGO.AddComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.pivot = new Vector2(0.5f, 0.5f);
            starRect.sizeDelta = new Vector2(160, 160);
            Image starImg = starGO.AddComponent<Image>();
            starImg.sprite = starSprite;
            starImg.color = new Color(1f, 0.85f, 0.45f);

            // Button trigger for opening pack
            Button packBtn = packBorderGO.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(packBtn.onClick, opener.OnClickOpenPack);

            // Pack Idle Animation Visuals Component
            PackIdleVisuals visuals = closedViewGO.AddComponent<PackIdleVisuals>();

            // Closed Pack CTA Label
            GameObject labelGO = new GameObject("OpenLabel");
            labelGO.transform.SetParent(closedViewGO.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, -290);
            labelRect.sizeDelta = new Vector2(600, 50);
            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "TOCA PARA ABRIR";
            labelTMP.fontSize = 32;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = Color.white;

            // Subtitle
            GameObject subLabelGO = new GameObject("SubLabel");
            subLabelGO.transform.SetParent(closedViewGO.transform, false);
            RectTransform subLabelRect = subLabelGO.AddComponent<RectTransform>();
            subLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
            subLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            subLabelRect.pivot = new Vector2(0.5f, 0.5f);
            subLabelRect.anchoredPosition = new Vector2(0, -335);
            subLabelRect.sizeDelta = new Vector2(600, 40);
            TextMeshProUGUI subLabelTMP = subLabelGO.AddComponent<TextMeshProUGUI>();
            subLabelTMP.text = "Sobre estándar · 5 cartas";
            subLabelTMP.fontSize = 20;
            subLabelTMP.alignment = TextAlignmentOptions.Center;
            subLabelTMP.color = new Color(0.60f, 0.65f, 0.72f);

            // Assign PackIdleVisuals fields
            SerializedObject soVisuals = new SerializedObject(visuals);
            soVisuals.FindProperty("packTransform").objectReferenceValue = packRect;
            soVisuals.FindProperty("raysTransform").objectReferenceValue = raysRect;
            soVisuals.FindProperty("ctaText").objectReferenceValue = labelTMP;
            soVisuals.ApplyModifiedProperties();

            // ----------------------------------------------------
            // 2. Reveal View (Single Card Centered + Progress Dots)
            // ----------------------------------------------------
            GameObject revealViewGO = new GameObject("RevealView");
            revealViewGO.transform.SetParent(screenGO.transform, false);
            RectTransform revealRect = revealViewGO.AddComponent<RectTransform>();
            revealRect.anchorMin = Vector2.zero;
            revealRect.anchorMax = Vector2.one;
            revealRect.sizeDelta = Vector2.zero;
            revealViewGO.SetActive(false);

            // Progress Dots Container
            GameObject progressGO = new GameObject("ProgressDotsContainer");
            progressGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform progressRect = progressGO.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 1f);
            progressRect.anchorMax = new Vector2(0.5f, 1f);
            progressRect.pivot = new Vector2(0.5f, 1f);
            progressRect.anchoredPosition = new Vector2(0, -135);
            progressRect.sizeDelta = new Vector2(300, 30);
            HorizontalLayoutGroup hlg = progressGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 18f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Single Card Container (Centered)
            GameObject singleContainerGO = new GameObject("SingleCardContainer");
            singleContainerGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform singleContainerRect = singleContainerGO.AddComponent<RectTransform>();
            singleContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            singleContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            singleContainerRect.pivot = new Vector2(0.5f, 0.5f);
            singleContainerRect.anchoredPosition = new Vector2(0, 30);
            singleContainerRect.sizeDelta = new Vector2(500, 680);

            // Fullscreen Button for Reveal Click Gesture
            Button revealBtn = revealViewGO.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(revealBtn.onClick, opener.OnClickCardInReveal);

            // Continue Hint Text
            GameObject continueHintGO = new GameObject("ContinueHintText");
            continueHintGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform continueHintRect = continueHintGO.AddComponent<RectTransform>();
            continueHintRect.anchorMin = new Vector2(0.5f, 0.5f);
            continueHintRect.anchorMax = new Vector2(0.5f, 0.5f);
            continueHintRect.pivot = new Vector2(0.5f, 0.5f);
            continueHintRect.anchoredPosition = new Vector2(0, -360);
            continueHintRect.sizeDelta = new Vector2(800, 40);
            TextMeshProUGUI continueHintTMP = continueHintGO.AddComponent<TextMeshProUGUI>();
            continueHintTMP.text = "Toca la carta para revelar";
            continueHintTMP.fontSize = 22;
            continueHintTMP.alignment = TextAlignmentOptions.Center;
            continueHintTMP.color = new Color(0.60f, 0.65f, 0.72f);

            // Tilt Hint Text (Holographic notice)
            GameObject tiltHintGO = new GameObject("TiltHintText");
            tiltHintGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform tiltHintRect = tiltHintGO.AddComponent<RectTransform>();
            tiltHintRect.anchorMin = new Vector2(0.5f, 0.5f);
            tiltHintRect.anchorMax = new Vector2(0.5f, 0.5f);
            tiltHintRect.pivot = new Vector2(0.5f, 0.5f);
            tiltHintRect.anchoredPosition = new Vector2(0, -400);
            tiltHintRect.sizeDelta = new Vector2(800, 40);
            TextMeshProUGUI tiltHintTMP = tiltHintGO.AddComponent<TextMeshProUGUI>();
            tiltHintTMP.text = "Mueve el ratón / dedo sobre la carta para ver el efecto";
            tiltHintTMP.fontSize = 18;
            tiltHintTMP.alignment = TextAlignmentOptions.Center;
            tiltHintTMP.color = new Color(0.96f, 0.65f, 0.14f);

            // ----------------------------------------------------
            // 3. Summary View (Straight Cards Row)
            // ----------------------------------------------------
            GameObject summaryViewGO = new GameObject("SummaryView");
            summaryViewGO.transform.SetParent(screenGO.transform, false);
            RectTransform summaryRect = summaryViewGO.AddComponent<RectTransform>();
            summaryRect.anchorMin = Vector2.zero;
            summaryRect.anchorMax = Vector2.one;
            summaryRect.sizeDelta = Vector2.zero;
            summaryViewGO.SetActive(false);

            // Title
            GameObject titleGO = new GameObject("SummaryTitle");
            titleGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -115);
            titleRect.sizeDelta = new Vector2(900, 55);
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SOBRE COMPLETO";
            titleTMP.fontSize = 38;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;

            // Subtitle
            GameObject subGO = new GameObject("SummarySubtitle");
            subGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -170);
            subRect.sizeDelta = new Vector2(900, 40);
            TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Revisa lo que conseguiste";
            subTMP.fontSize = 22;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = new Color(0.7f, 0.75f, 0.85f);

            // Summary Cards Container (Centered)
            GameObject summaryContainerGO = new GameObject("SummaryCardContainer");
            summaryContainerGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform summaryContainerRect = summaryContainerGO.AddComponent<RectTransform>();
            summaryContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            summaryContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            summaryContainerRect.pivot = new Vector2(0.5f, 0.5f);
            summaryContainerRect.anchoredPosition = new Vector2(0, 0);
            summaryContainerRect.sizeDelta = new Vector2(1000, 1000);

            // Restart Button "ABRIR OTRO SOBRE" in Perfect Semicircular Gold Pill
            GameObject openAnotherBtnGO = new GameObject("OpenAnotherButton");
            openAnotherBtnGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform btnRect = openAnotherBtnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 80);
            btnRect.sizeDelta = new Vector2(520, 88);
            Image btnImg = openAnotherBtnGO.AddComponent<Image>();
            btnImg.sprite = pillSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.96f, 0.65f, 0.14f); // Rich Gold

            Button openAnotherBtn = openAnotherBtnGO.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(openAnotherBtn.onClick, opener.ResetToClosedView);

            GameObject btnTextGO = new GameObject("BtnText");
            btnTextGO.transform.SetParent(openAnotherBtnGO.transform, false);
            RectTransform btnTextRect = btnTextGO.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnTMP.text = "ABRIR OTRO SOBRE";
            btnTMP.fontSize = 26;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.color = new Color(0.04f, 0.07f, 0.12f);

            // ----------------------------------------------------
            // 4. Flash Overlay (Global Top)
            // ----------------------------------------------------
            GameObject flashGO = new GameObject("FlashOverlay");
            flashGO.transform.SetParent(canvasGO.transform, false);
            RectTransform flashRect = flashGO.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.sizeDelta = Vector2.zero;
            Image flashImg = flashGO.AddComponent<Image>();
            flashImg.color = Color.white;
            flashGO.SetActive(false);

            // Load CardPrefab
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Cards/CardPrefab.prefab");

            // Load 10 Pilot Cards into Catalog List
            List<CardData> catalogList = new List<CardData>();
            string pilotFolderPath = "Assets/_Project/ScriptableObjects/PilotAlbum";
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { pilotFolderPath });
            foreach (var guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null) catalogList.Add(card);
            }

            // Assign Serialized Properties on PackOpener
            SerializedObject so = new SerializedObject(opener);
            so.FindProperty("screenContainer").objectReferenceValue = screenRect;
            so.FindProperty("closedPackView").objectReferenceValue = closedViewGO;
            so.FindProperty("revealView").objectReferenceValue = revealViewGO;
            so.FindProperty("summaryView").objectReferenceValue = summaryViewGO;
            so.FindProperty("packGraphicTransform").objectReferenceValue = packRect;
            so.FindProperty("packCounterText").objectReferenceValue = counterTMP;
            so.FindProperty("forceHoloToggle").objectReferenceValue = toggle;
            so.FindProperty("progressDotsContainer").objectReferenceValue = progressRect;
            so.FindProperty("singleCardContainer").objectReferenceValue = singleContainerRect;
            so.FindProperty("continueHintText").objectReferenceValue = continueHintTMP;
            so.FindProperty("tiltHintText").objectReferenceValue = tiltHintTMP;
            so.FindProperty("summarySubtitleText").objectReferenceValue = subTMP;
            so.FindProperty("summaryCardContainer").objectReferenceValue = summaryContainerRect;
            so.FindProperty("openAnotherButton").objectReferenceValue = openAnotherBtn;
            so.FindProperty("flashOverlay").objectReferenceValue = flashImg;
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;

            SerializedProperty catalogProp = so.FindProperty("cardCatalog");
            catalogProp.arraySize = catalogList.Count;
            for (int i = 0; i < catalogList.Count; i++)
            {
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = catalogList[i];
            }
            so.ApplyModifiedProperties();

            // Ensure folder exists and save scene
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Escena PackOpeningScene.unity regenerada con máscara de rayos y cápsula ultra redondeada!</color>");
        }
    }
}
#endif
