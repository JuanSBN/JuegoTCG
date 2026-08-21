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

namespace JuegoTCG.EditorTools
{
    public static class PackOpeningSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/PackOpeningScene.unity";

        [MenuItem("JuegoTCG/Generar Escena de Apertura de Sobres")]
        public static void BuildPackOpeningScene()
        {
            // 1. Rebuild CardPrefab with CardBackground first
            CardPrefabBuilder.BuildCardPrefab();

            // 2. Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f); // Deep pitch blue #0B1220
            cam.orthographic = true;
            camGO.AddComponent<AudioListener>();

            // Canvas
            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Event System
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // PackOpener Controller GO
            GameObject openerGO = new GameObject("PackOpenerController");
            openerGO.transform.SetParent(canvasGO.transform, false);
            PackOpener opener = openerGO.AddComponent<PackOpener>();

            // Top Bar
            GameObject topBarGO = new GameObject("TopBar");
            topBarGO.transform.SetParent(canvasGO.transform, false);
            RectTransform topBarRect = topBarGO.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0.05f, 0.92f);
            topBarRect.anchorMax = new Vector2(0.95f, 0.98f);
            topBarRect.sizeDelta = Vector2.zero;

            // Force Holo Toggle (Pruebas)
            GameObject toggleGO = new GameObject("ForceHoloToggle");
            toggleGO.transform.SetParent(topBarGO.transform, false);
            RectTransform toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 0);
            toggleRect.anchorMax = new Vector2(0.45f, 1);
            toggleRect.sizeDelta = Vector2.zero;
            Toggle toggle = toggleGO.AddComponent<Toggle>();
            toggle.isOn = true; // Por defecto activado para pruebas visuales inmediatas

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

            // Pack Counter Text
            GameObject counterGO = new GameObject("PackCounter");
            counterGO.transform.SetParent(topBarGO.transform, false);
            RectTransform counterRect = counterGO.AddComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0.55f, 0);
            counterRect.anchorMax = new Vector2(1, 1);
            counterRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI counterTMP = counterGO.AddComponent<TextMeshProUGUI>();
            counterTMP.text = "5 sobres";
            counterTMP.fontSize = 22;
            counterTMP.fontStyle = FontStyles.Bold;
            counterTMP.alignment = TextAlignmentOptions.Right;
            counterTMP.color = new Color(0.96f, 0.65f, 0.14f);

            // ----------------------------------------------------
            // 1. Closed Pack View
            // ----------------------------------------------------
            GameObject closedViewGO = new GameObject("ClosedPackView");
            closedViewGO.transform.SetParent(canvasGO.transform, false);
            RectTransform closedRect = closedViewGO.AddComponent<RectTransform>();
            closedRect.anchorMin = Vector2.zero;
            closedRect.anchorMax = Vector2.one;
            closedRect.sizeDelta = Vector2.zero;

            // Pack Card Graphic
            GameObject packCardGO = new GameObject("PackGraphic");
            packCardGO.transform.SetParent(closedViewGO.transform, false);
            RectTransform packRect = packCardGO.AddComponent<RectTransform>();
            packRect.sizeDelta = new Vector2(460, 640);
            Image packImg = packCardGO.AddComponent<Image>();
            packImg.color = new Color(0.09f, 0.14f, 0.23f);

            // Button trigger for opening pack
            Button packBtn = packCardGO.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(packBtn.onClick, opener.OnClickOpenPack);

            // Closed Pack Text Label
            GameObject labelGO = new GameObject("OpenLabel");
            labelGO.transform.SetParent(packCardGO.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0.12f);
            labelRect.anchorMax = new Vector2(0.95f, 0.28f);
            labelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "TOCA PARA ABRIR";
            labelTMP.fontSize = 30;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = new Color(0.96f, 0.65f, 0.14f); // Gold

            // Subtitle
            GameObject subLabelGO = new GameObject("SubLabel");
            subLabelGO.transform.SetParent(packCardGO.transform, false);
            RectTransform subLabelRect = subLabelGO.AddComponent<RectTransform>();
            subLabelRect.anchorMin = new Vector2(0.05f, 0.04f);
            subLabelRect.anchorMax = new Vector2(0.95f, 0.12f);
            subLabelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI subLabelTMP = subLabelGO.AddComponent<TextMeshProUGUI>();
            subLabelTMP.text = "Sobre estándar · 5 cartas";
            subLabelTMP.fontSize = 18;
            subLabelTMP.alignment = TextAlignmentOptions.Center;
            subLabelTMP.color = new Color(0.65f, 0.7f, 0.8f);

            // ----------------------------------------------------
            // 2. Reveal View (Single Card Centered + Progress Dots)
            // ----------------------------------------------------
            GameObject revealViewGO = new GameObject("RevealView");
            revealViewGO.transform.SetParent(canvasGO.transform, false);
            RectTransform revealRect = revealViewGO.AddComponent<RectTransform>();
            revealRect.anchorMin = Vector2.zero;
            revealRect.anchorMax = Vector2.one;
            revealRect.sizeDelta = Vector2.zero;
            revealViewGO.SetActive(false);

            // Progress Dots Container
            GameObject progressGO = new GameObject("ProgressDotsContainer");
            progressGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform progressRect = progressGO.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.3f, 0.86f);
            progressRect.anchorMax = new Vector2(0.7f, 0.90f);
            progressRect.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup hlg = progressGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 16f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Single Card Container (Centered)
            GameObject singleContainerGO = new GameObject("SingleCardContainer");
            singleContainerGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform singleContainerRect = singleContainerGO.AddComponent<RectTransform>();
            singleContainerRect.anchorMin = Vector2.zero;
            singleContainerRect.anchorMax = Vector2.one;
            singleContainerRect.sizeDelta = Vector2.zero;

            // Fullscreen Button for Reveal Click Gesture
            Button revealBtn = revealViewGO.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(revealBtn.onClick, opener.OnClickCardInReveal);

            // Continue Hint Text
            GameObject continueHintGO = new GameObject("ContinueHintText");
            continueHintGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform continueHintRect = continueHintGO.AddComponent<RectTransform>();
            continueHintRect.anchorMin = new Vector2(0.1f, 0.08f);
            continueHintRect.anchorMax = new Vector2(0.9f, 0.13f);
            continueHintRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI continueHintTMP = continueHintGO.AddComponent<TextMeshProUGUI>();
            continueHintTMP.text = "Toca la pantalla para revelar la siguiente carta";
            continueHintTMP.fontSize = 22;
            continueHintTMP.alignment = TextAlignmentOptions.Center;
            continueHintTMP.color = new Color(0.85f, 0.88f, 0.95f);

            // Tilt Hint Text (Holographic notice)
            GameObject tiltHintGO = new GameObject("TiltHintText");
            tiltHintGO.transform.SetParent(revealViewGO.transform, false);
            RectTransform tiltHintRect = tiltHintGO.AddComponent<RectTransform>();
            tiltHintRect.anchorMin = new Vector2(0.05f, 0.03f);
            tiltHintRect.anchorMax = new Vector2(0.95f, 0.08f);
            tiltHintRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI tiltHintTMP = tiltHintGO.AddComponent<TextMeshProUGUI>();
            tiltHintTMP.text = "Mueve el ratón / dedo sobre la carta para ver el efecto holográfico";
            tiltHintTMP.fontSize = 18;
            tiltHintTMP.alignment = TextAlignmentOptions.Center;
            tiltHintTMP.color = new Color(0.96f, 0.65f, 0.14f);

            // ----------------------------------------------------
            // 3. Summary View (Straight Cards Row)
            // ----------------------------------------------------
            GameObject summaryViewGO = new GameObject("SummaryView");
            summaryViewGO.transform.SetParent(canvasGO.transform, false);
            RectTransform summaryRect = summaryViewGO.AddComponent<RectTransform>();
            summaryRect.anchorMin = Vector2.zero;
            summaryRect.anchorMax = Vector2.one;
            summaryRect.sizeDelta = Vector2.zero;
            summaryViewGO.SetActive(false);

            // Title
            GameObject titleGO = new GameObject("SummaryTitle");
            titleGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.82f);
            titleRect.anchorMax = new Vector2(0.9f, 0.92f);
            titleRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SOBRE COMPLETO";
            titleTMP.fontSize = 42;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;

            // Subtitle
            GameObject subGO = new GameObject("SummarySubtitle");
            subGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.05f, 0.77f);
            subRect.anchorMax = new Vector2(0.95f, 0.83f);
            subRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Revisa lo que conseguiste";
            subTMP.fontSize = 24;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = new Color(0.7f, 0.75f, 0.85f);

            // Summary Cards Container (Centered Row)
            GameObject summaryContainerGO = new GameObject("SummaryCardContainer");
            summaryContainerGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform summaryContainerRect = summaryContainerGO.AddComponent<RectTransform>();
            summaryContainerRect.anchorMin = new Vector2(0.05f, 0.25f);
            summaryContainerRect.anchorMax = new Vector2(0.95f, 0.72f);
            summaryContainerRect.sizeDelta = Vector2.zero;

            // Restart Button "ABRIR OTRO SOBRE"
            GameObject openAnotherBtnGO = new GameObject("OpenAnotherButton");
            openAnotherBtnGO.transform.SetParent(summaryViewGO.transform, false);
            RectTransform btnRect = openAnotherBtnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.2f, 0.08f);
            btnRect.anchorMax = new Vector2(0.8f, 0.16f);
            btnRect.sizeDelta = Vector2.zero;
            Image btnImg = openAnotherBtnGO.AddComponent<Image>();
            btnImg.color = new Color(0.96f, 0.65f, 0.14f); // Gold

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
            so.FindProperty("closedPackView").objectReferenceValue = closedViewGO;
            so.FindProperty("revealView").objectReferenceValue = revealViewGO;
            so.FindProperty("summaryView").objectReferenceValue = summaryViewGO;
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

            Debug.Log("<color=green>[JuegoTCG] ¡Escena PackOpeningScene.unity regenerada sin caracteres unicode faltantes!</color>");
        }
    }
}
#endif
