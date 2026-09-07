using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using JuegoTCG.Cards;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class UIToolkitPackOpeningSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/PackOpeningSceneUIToolkit.unity";
        private const string UXMLPath = "Assets/_Project/UI/Views/PackOpeningScreen.uxml";
        private const string USSPath = "Assets/_Project/UI/Styles/PackOpeningScreen.uss";
        private const string PanelSettingsPath = "Assets/_Project/UI/PanelSettings.asset";
        private const string CardPrefabPath = "Assets/_Project/Prefabs/Cards/CardPrefab.prefab";

        [MenuItem("JuegoTCG/✨ UI Toolkit (UXML + USS)/🎴 Apertura de Sobres UI Toolkit", priority = 30)]
        public static void BuildPackOpeningScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("JuegoTCG", "Por favor sal del modo Play antes de generar la escena.", "Entendido");
                return;
            }

            AssetDatabase.ImportAsset(USSPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UXMLPath, ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camara Principal
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.070f, 0.125f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGO.AddComponent<AudioListener>();

            // 2. UI Document (Entorno UI Toolkit 1080x2400)
            GameObject uiDocGO = new GameObject("UIDocument_PackOpening");
            UIDocument uiDoc = uiDocGO.AddComponent<UIDocument>();

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (uxml != null)
            {
                uiDoc.visualTreeAsset = uxml;
            }

            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1080, 2400);
                panelSettings.match = 0.0f;
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                AssetDatabase.SaveAssets();
            }
            uiDoc.panelSettings = panelSettings;

            // 3. Canvas uGUI para el CardPrefab (Renderizado nitido oficial con TextMeshPro y Foil)
            GameObject canvasGO = new GameObject("CardRevealCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 5; // Por encima del fondo UI Toolkit

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.0f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.SetActive(false); // Inicialmente oculto hasta que el sobre se desgarre

            // Contenedor central para spawnear el CardPrefab
            GameObject spawnContainerGO = new GameObject("CardSpawnContainer");
            spawnContainerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform spawnRect = spawnContainerGO.AddComponent<RectTransform>();
            spawnRect.anchorMin = new Vector2(0.5f, 0.5f);
            spawnRect.anchorMax = new Vector2(0.5f, 0.5f);
            spawnRect.pivot = new Vector2(0.5f, 0.5f);
            spawnRect.anchoredPosition = new Vector2(0, 30);
            spawnRect.sizeDelta = new Vector2(680, 960);

            // 4. EventSystem para permitir clics e inclinacion interactiva (HolographicTilt)
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 5. Cargar CardPrefab y Catalogo de Cartas
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            List<CardData> catalog = new List<CardData>();
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/_Project/ScriptableObjects/PilotAlbum" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null) catalog.Add(card);
            }

            // 6. Controlador Principal UI Toolkit
            UIToolkitPackOpeningController controller = uiDocGO.AddComponent<UIToolkitPackOpeningController>();

            // Serializar referencias en el controlador
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("cardRevealCanvas").objectReferenceValue = canvasGO;
            so.FindProperty("cardSpawnContainer").objectReferenceValue = spawnRect;

            SerializedProperty catalogProp = so.FindProperty("cardCatalog");
            catalogProp.ClearArray();
            for (int i = 0; i < catalog.Count; i++)
            {
                catalogProp.InsertArrayElementAtIndex(i);
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = catalog[i];
            }

            string[] frameGuids = new string[]
            {
                "4edcac4ad7f822e4aa7b10b2dd755926", // Comun
                "586794b59d6595341aa4a2f2b59209ce", // Especial
                "ab60ad89df16072448c901abb76cbe3a", // Epica
                "2a89c6d7166430641b49f80a84ac2cd8", // Legendaria
                "8ab77af7592605c48b2e119ccdb7dcb3", // Mitica
                "ae059fc1520988141a79cb933243639f"  // Full Art
            };

            SerializedProperty framesProp = so.FindProperty("rarityFrames");
            framesProp.ClearArray();
            for (int i = 0; i < 6; i++)
            {
                framesProp.InsertArrayElementAtIndex(i);
                string framePath = AssetDatabase.GUIDToAssetPath(frameGuids[i]);
                Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frameSprite;
            }

            so.ApplyModifiedProperties();

            // Guardar Escena
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[UI Toolkit] Escena de Apertura de Sobres con CardPrefab creada con exito en: {ScenePath}</color>");
        }
    }
}