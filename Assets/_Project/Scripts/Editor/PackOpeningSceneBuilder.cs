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
            // Create new scene
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

            // 1. Closed Pack UI
            GameObject closedPackGO = new GameObject("ClosedPackUI");
            closedPackGO.transform.SetParent(canvasGO.transform, false);
            RectTransform packRect = closedPackGO.AddComponent<RectTransform>();
            packRect.sizeDelta = new Vector2(400, 560);
            Image packImg = closedPackGO.AddComponent<Image>();
            packImg.color = new Color(0.09f, 0.14f, 0.23f);

            // Button trigger for opening pack
            Button packBtn = closedPackGO.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(packBtn.onClick, opener.OnClickOpenPack);

            // Closed Pack Text Label
            GameObject labelGO = new GameObject("OpenLabel");
            labelGO.transform.SetParent(closedPackGO.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.1f);
            labelRect.anchorMax = new Vector2(1, 0.3f);
            labelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "TOCAR PARA ABRIR SOBRE";
            labelTMP.fontSize = 28;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = new Color(0.96f, 0.65f, 0.14f); // Gold

            // 2. Card Container
            GameObject containerGO = new GameObject("CardContainer");
            containerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            // 3. Flash Overlay
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
            so.FindProperty("closedPackUI").objectReferenceValue = closedPackGO;
            so.FindProperty("flashOverlay").objectReferenceValue = flashImg;
            so.FindProperty("cardContainer").objectReferenceValue = containerRect;
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

            Debug.Log("<color=green>[JuegoTCG] ¡Escena PackOpeningScene.unity creada exitosamente en Assets/_Project/Scenes/PackOpeningScene.unity!</color>");
        }
    }
}
#endif
