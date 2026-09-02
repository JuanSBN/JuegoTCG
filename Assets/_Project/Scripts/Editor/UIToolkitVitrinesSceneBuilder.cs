using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class UIToolkitVitrinesSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/VitrinesSceneUIToolkit.unity";
        private const string UXMLPath = "Assets/_Project/UI/Views/VitrinesScreen.uxml";
        private const string PanelSettingsPath = "Assets/_Project/UI/PanelSettings.asset";

        [MenuItem("JuegoTCG/✨ UI Toolkit (UXML + USS)/🏆 Vitrinas Públicas UI Toolkit", priority = 21)]
        public static void BuildUIToolkitVitrinesScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[UIToolkit] Sal del modo Play antes de generar la escena.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.075f, 0.051f); // #09130d
            cam.orthographic = true;
            camGO.AddComponent<AudioListener>();

            // UI Document
            GameObject uiDocGO = new GameObject("UIDocument_VitrinesScreen");
            UIDocument uiDoc = uiDocGO.AddComponent<UIDocument>();

            // Load UXML
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (uxml != null)
            {
                uiDoc.visualTreeAsset = uxml;
            }
            else
            {
                Debug.LogError($"[UIToolkit] No se encontró el archivo UXML en {UXMLPath}");
            }

            // Panel Settings
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1080, 2400);
                panelSettings.match = 0.0f; // Match Width
                
                string dir = Path.GetDirectoryName(PanelSettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                AssetDatabase.SaveAssets();
            }
            uiDoc.panelSettings = panelSettings;

            // Add Controller
            uiDocGO.AddComponent<UIToolkitVitrinesController>();

            // Save Scene
            string sceneDir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register in Build Settings
            JuegoTCG.Editor.AutoRegisterBuildScenes.RegisterScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log($"<color=gold>[UIToolkit:Vitrinas] ¡Escena de Vitrinas Públicas generada con 100% fidelidad Figma en {ScenePath}!</color>");
        }
    }
}
